using AgInterop.Structs.AgCoreStructs;
using System;
using System.Net.Sockets;

namespace AgInterop.Classes
{
    public class TcpMessageEventArgs : EventArgs
    {
        public TcpClient Client;
        public IPCData Data;
        public Object State;

        public TcpMessageEventArgs(TcpClient client, IPCData? data, Object state)
        {
            Client = client;
            if (data != null)
                Data = (IPCData)data;
            State = state;
        }
    }
}
