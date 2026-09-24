using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace TelegramTransport
{
    internal sealed class TelegramApiClient : IDisposable
    {
        private readonly string _endpoint;
        private readonly string _userAgent;
        private readonly IWebProxy _proxy;
        private long _nextUpdateId;

        public TelegramApiClient(
            string botToken,
            string apiBase,
            string userAgent,
            string proxyHost,
            string proxyPort,
            string proxyUser,
            string proxyPassword)
        {
            if (string.IsNullOrWhiteSpace(botToken))
            {
                throw new ArgumentException("A Telegram bot token is required.", nameof(botToken));
            }

            _endpoint = apiBase.TrimEnd('/') + "/bot" + botToken + "/";
            _userAgent = userAgent;
            _proxy = CreateProxy(proxyHost, proxyPort, proxyUser, proxyPassword);
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
        }

        public void SendText(string chatId, string text)
        {
            var request = new TelegramSendMessageRequest
            {
                ChatId = chatId,
                Text = text,
                DisableWebPagePreview = true
            };

            Post<TelegramMessage>("sendMessage", request, 30);
        }

        public TelegramUpdate[] GetUpdates(int timeoutSeconds)
        {
            var request = new TelegramGetUpdatesRequest
            {
                Offset = _nextUpdateId,
                Timeout = Math.Max(1, timeoutSeconds),
                AllowedUpdates = new[] { "message" }
            };

            TelegramUpdate[] updates = Post<TelegramUpdate[]>(
                "getUpdates",
                request,
                request.Timeout + 30) ?? new TelegramUpdate[0];

            if (updates.Length > 0)
            {
                _nextUpdateId = updates.Max(update => update.UpdateId) + 1;
            }

            return updates;
        }

        private T Post<T>(string method, object body, int timeoutSeconds)
        {
            for (int attempt = 0; attempt < 4; attempt++)
            {
                string responseBody = string.Empty;
                HttpStatusCode statusCode = 0;
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_endpoint + method);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Accept = "application/json";
                    request.Proxy = _proxy;
                    request.Timeout = Math.Max(1, timeoutSeconds) * 1000;
                    request.ReadWriteTimeout = request.Timeout;
                    if (!string.IsNullOrWhiteSpace(_userAgent))
                    {
                        request.UserAgent = _userAgent;
                    }

                    byte[] data = Encoding.UTF8.GetBytes(JsonCodec.Serialize(body));
                    request.ContentLength = data.Length;
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(data, 0, data.Length);
                    }

                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        statusCode = response.StatusCode;
                        responseBody = ReadResponse(response);
                    }
                }
                catch (WebException exception)
                {
                    var response = exception.Response as HttpWebResponse;
                    if (response == null)
                    {
                        throw;
                    }

                    using (response)
                    {
                        statusCode = response.StatusCode;
                        responseBody = ReadResponse(response);
                    }
                }

                TelegramApiResponse<T>? telegramResponse = null;
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    telegramResponse = JsonCodec.Deserialize<TelegramApiResponse<T>>(responseBody);
                }

                if ((int)statusCode == 429 &&
                    telegramResponse != null &&
                    telegramResponse.Parameters != null &&
                    telegramResponse.Parameters.RetryAfter.HasValue)
                {
                    Thread.Sleep(Math.Max(1, Math.Min(60, telegramResponse.Parameters.RetryAfter.Value)) * 1000);
                    continue;
                }

                if ((int)statusCode < 200 ||
                    (int)statusCode >= 300 ||
                    telegramResponse == null ||
                    !telegramResponse.Ok)
                {
                    string description = telegramResponse == null
                        ? "Telegram returned HTTP " + (int)statusCode + "."
                        : telegramResponse.Description;
                    throw new InvalidOperationException(description);
                }

                return telegramResponse.Result;
            }

            throw new InvalidOperationException("Telegram rate limit retries were exhausted.");
        }

        private static string ReadResponse(HttpWebResponse response)
        {
            using (Stream stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream ?? Stream.Null, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static IWebProxy CreateProxy(
            string proxyHost,
            string proxyPort,
            string proxyUser,
            string proxyPassword)
        {
            if (string.IsNullOrWhiteSpace(proxyHost))
            {
                IWebProxy proxy = WebRequest.GetSystemWebProxy();
                proxy.Credentials = CredentialCache.DefaultCredentials;
                return proxy;
            }

            string address = proxyHost;
            if (!string.IsNullOrWhiteSpace(proxyPort))
            {
                address = proxyHost.TrimEnd('/') + ":" + proxyPort;
            }

            var configuredProxy = new WebProxy(address);
            if (!string.IsNullOrWhiteSpace(proxyUser))
            {
                configuredProxy.Credentials = new NetworkCredential(proxyUser, proxyPassword);
            }
            return configuredProxy;
        }

        public byte[]? DownloadDocument(string fileId)
        {
            var request = new TelegramGetFileRequest { FileId = fileId };
            byte[] requestBody = Encoding.UTF8.GetBytes(JsonCodec.Serialize(request));
            string responseBody = string.Empty;
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_endpoint + "getFile");
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Accept = "application/json";
                req.Proxy = _proxy;
                using (var rs = req.GetRequestStream())
                    rs.Write(requestBody, 0, requestBody.Length);
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream() ?? Stream.Null, Encoding.UTF8))
                    responseBody = reader.ReadToEnd();
            }
            catch (WebException) { return null; }

            var file = JsonCodec.Deserialize<TelegramFileResponse>(responseBody);
            if (file == null || string.IsNullOrWhiteSpace(file.FilePath)) return null;

            string downloadUrl = _endpoint + "file/" + file.FilePath;
            try
            {
                HttpWebRequest dl = (HttpWebRequest)WebRequest.Create(downloadUrl);
                dl.Method = "GET";
                dl.Proxy = _proxy;
                using (var resp = (HttpWebResponse)dl.GetResponse())
                using (var stream = resp.GetResponseStream() ?? Stream.Null)
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
            catch (WebException) { return null; }
        }

        public void Dispose()
        {
        }
    }

    internal static class JsonCodec
    {
        public static string Serialize(object value)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(value.GetType());
                serializer.WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static T Deserialize<T>(string json)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(stream);
            }
        }
    }

    [DataContract]
    internal sealed class TelegramApiResponse<T>
    {
        [DataMember(Name = "ok")]
        public bool Ok { get; set; }

        [DataMember(Name = "result")]
        public T Result { get; set; } = default!;

        [DataMember(Name = "description")]
        public string Description { get; set; } = string.Empty;

        [DataMember(Name = "parameters")]
        public TelegramResponseParameters? Parameters { get; set; }
    }

    [DataContract]
    internal sealed class TelegramResponseParameters
    {
        [DataMember(Name = "retry_after")]
        public int? RetryAfter { get; set; }
    }

    [DataContract]
    internal sealed class TelegramGetUpdatesRequest
    {
        [DataMember(Name = "offset")]
        public long Offset { get; set; }

        [DataMember(Name = "timeout")]
        public int Timeout { get; set; }

        [DataMember(Name = "allowed_updates")]
        public string[] AllowedUpdates { get; set; } = new string[0];
    }

    [DataContract]
    internal sealed class TelegramSendMessageRequest
    {
        [DataMember(Name = "chat_id")]
        public string ChatId { get; set; } = string.Empty;

        [DataMember(Name = "text")]
        public string Text { get; set; } = string.Empty;

        [DataMember(Name = "disable_web_page_preview")]
        public bool DisableWebPagePreview { get; set; }
    }

    [DataContract]
    internal sealed class TelegramUpdate
    {
        [DataMember(Name = "update_id")]
        public long UpdateId { get; set; }

        [DataMember(Name = "message")]
        public TelegramMessage? Message { get; set; }
    }

    [DataContract]
    internal sealed class TelegramMessage
    {
        [DataMember(Name = "text")]
        public string? Text { get; set; }

        [DataMember(Name = "from")]
        public TelegramUser? From { get; set; }

        [DataMember(Name = "document")]
        public TelegramDocument? Document { get; set; }
    }

    [DataContract]
    internal sealed class TelegramDocument
    {
        [DataMember(Name = "file_id")]
        public string FileId { get; set; } = string.Empty;
        [DataMember(Name = "file_name")]
        public string? FileName { get; set; }
        [DataMember(Name = "file_size")]
        public long FileSize { get; set; }
    }

    [DataContract]
    internal sealed class TelegramGetFileRequest
    {
        [DataMember(Name = "file_id")]
        public string FileId { get; set; } = string.Empty;
    }

    [DataContract]
    internal sealed class TelegramFileResponse
    {
        [DataMember(Name = "file_id")]
        public string FileId { get; set; } = string.Empty;
        [DataMember(Name = "file_path")]
        public string? FilePath { get; set; }
    }

    [DataContract]
    internal sealed class TelegramUser
    {
        [DataMember(Name = "is_bot")]
        public bool IsBot { get; set; }

        [DataMember(Name = "username")]
        public string? Username { get; set; }
    }
}
