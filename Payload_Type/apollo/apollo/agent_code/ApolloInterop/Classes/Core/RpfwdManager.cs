using System;
using System.Net.Sockets;
using AgInterop.Interfaces;
using AgInterop.Structs.MythicStructs;

namespace AgInterop.Classes
{
    public abstract class RpfwdManager : IRpfwdManager
    {
        protected IAgent _agent;

        public RpfwdManager(IAgent agent)
        {
            _agent = agent;
        }

        public virtual bool AddConnection(TcpClient client, int ServerID, int port, int debugLevel, Tasking task)
        {
            throw new NotImplementedException();
        }

        public virtual bool Route(SocksDatagram dg)
        {
            throw new NotImplementedException();
        }

        public virtual bool Remove(int id)
        {
            throw new NotImplementedException();
        }
    }
}
