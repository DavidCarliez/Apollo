using AgInterop.Structs.AgCoreStructs;
using AgInterop.Structs.MythicStructs;
using System;
using System.Security.Principal;

namespace AgInterop.Interfaces
{
    public interface IIdentityManager
    {
        WindowsIdentity GetCurrentPrimaryIdentity();
        WindowsIdentity GetCurrentImpersonationIdentity();
        WindowsIdentity GetOriginal();

        bool GetCurrentLogonInformation(out AgCoreLogonInformation logonInfo);

        void Revert();

        void SetPrimaryIdentity(WindowsIdentity identity);

        void SetPrimaryIdentity(IntPtr hToken);

        void SetImpersonationIdentity(WindowsIdentity identity);
        void SetImpersonationIdentity(IntPtr hToken);

        bool SetIdentity(AgCoreLogonInformation token);

        IntegrityLevel GetIntegrityLevel();

        bool IsOriginalIdentity();

        bool GetSystem();
        bool RunAsSystem(Action<WindowsIdentity> action);

    }
}
