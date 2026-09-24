using AgInterop.Structs.AgCoreStructs;
using System;
using System.IO.Pipes;

namespace AgInterop.Classes
{
    public class NamedPipeMessageArgs : EventArgs
    {
        public PipeStream Pipe;
        public IPCData Data;
        public Object State;

        public NamedPipeMessageArgs(PipeStream pipe, IPCData? data, Object state)
        {
            Pipe = pipe;
            if (data != null)
                Data = (IPCData)data;
            State = state;
        }
    }
}
