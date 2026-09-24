using AgInterop.Structs.AgCoreStructs;
using System;

namespace AgInterop.Interfaces
{
    public interface IProcess
    {
        bool Inject(byte[] code, string arguments = "");
        void WaitForExit();
        void WaitForExit(int milliseconds);

        bool Start();
        bool StartWithCredentials(AgCoreLogonInformation logonInfo);

        bool StartWithCredentials(IntPtr hToken);

    }
}
