using System;

namespace AgInterop.Classes
{
    public class UUIDEventArgs : EventArgs
    {
        public readonly string UUID;
        public UUIDEventArgs(string uuid)
        {
            UUID = uuid;
        }
    }
}
