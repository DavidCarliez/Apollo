using System.Runtime.Serialization;

namespace TelegramTransport
{
    [DataContract]
    internal sealed class TelegramEnvelope
    {
        [DataMember(Name = "v")]
        public int Version { get; set; } = 1;

        [DataMember(Name = "sender_id")]
        public string SenderId { get; set; } = string.Empty;

        [DataMember(Name = "client_id")]
        public string ClientId { get; set; } = string.Empty;

        [DataMember(Name = "to_server")]
        public bool ToServer { get; set; }

        [DataMember(Name = "packet_id")]
        public string PacketId { get; set; } = string.Empty;

        [DataMember(Name = "reply_to")]
        public string ReplyToPacketId { get; set; } = string.Empty;

        [DataMember(Name = "sleep")]
        public int SleepSeconds { get; set; }

        [DataMember(Name = "jitter")]
        public int JitterPercent { get; set; }

        [DataMember(Name = "chunk")]
        public int Chunk { get; set; }

        [DataMember(Name = "chunks")]
        public int Chunks { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; } = string.Empty;
    }
}
