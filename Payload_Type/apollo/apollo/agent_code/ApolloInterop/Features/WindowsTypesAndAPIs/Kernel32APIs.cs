using AgInterop.Enums;

namespace AgInterop.Features.WindowsTypesAndAPIs;

public class Kernel32APIs
{
    public delegate APIInteropTypes.HANDLE OpenProcess(Win32.ProcessAccessFlags dwDesiredAccess, bool bInheritHandle,  int dwProcessId);
    public delegate bool CloseHandle(APIInteropTypes.HANDLE hObject);
}