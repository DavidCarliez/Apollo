using System.Runtime.InteropServices;
using System.Security.Principal;
using AgInterop.Enums;
using static AgInterop.Features.WindowsTypesAndAPIs.WinNTTypes;
using static AgInterop.Features.WindowsTypesAndAPIs.LSATypes;
using static AgInterop.Features.WindowsTypesAndAPIs.APIInteropTypes;

namespace AgInterop.Features.WindowsTypesAndAPIs;

public class Advapi32APIs
{
    public delegate NTSTATUS LsaOpenPolicy(HANDLE<LSA_OUT_STRING> SystemName, HANDLE<OBJECT_ATTRIBUTES> ObjectAttributes, ACCESS_MASK DesiredAccess, out HANDLE PolicyHandle);
    public delegate bool GetTokenInformation(HANDLE tokenHandle, Win32.TokenInformationClass tokenInformationClass, HANDLE tokenInformation, int tokenInformationLength, out int returnLength);
    public delegate uint LsaNtStatusToWinError(NTSTATUS status);
    public delegate bool OpenProcessToken(HANDLE ProcessHandle, TokenAccessLevels DesiredAccess, out HANDLE TokenHandle);
    public delegate bool ImpersonateLoggedOnUser(HANDLE TokenHandle);
    public delegate bool AllocateLocallyUniqueId(out LUID luid);
    public delegate bool LogonUserA(HANDLE lpszUsername, HANDLE lpszDomain, HANDLE lpszPassword, Win32.LogonType dwLogonType, Win32.LogonProvider dwLogonProvider, out HANDLE phToken);
    public delegate bool LogonUserW(
        [MarshalAs(UnmanagedType.LPWStr)] string lpszUsername,
        [MarshalAs(UnmanagedType.LPWStr)] string lpszDomain,
        [MarshalAs(UnmanagedType.LPWStr)] string lpszPassword,
        Win32.LogonType dwLogonType,
        Win32.LogonProvider dwLogonProvider,
        out HANDLE phToken);
}
