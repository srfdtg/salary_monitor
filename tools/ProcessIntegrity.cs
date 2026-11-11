using System;
using System.Runtime.InteropServices;

internal static class ProcessIntegrity
{
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const uint TOKEN_QUERY = 0x0008;
    private const int TokenIntegrityLevel = 25;
    private const int TokenIsAppContainer = 29;
    private const int TokenElevation = 20;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(IntPtr token, int infoClass,
        IntPtr info, int length, out int returnLength);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool ConvertSidToStringSid(IntPtr sid, out IntPtr stringSid);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    private static string IntegritySid(IntPtr token)
    {
        int needed;
        GetTokenInformation(token, TokenIntegrityLevel, IntPtr.Zero, 0, out needed);
        if (needed <= 0)
        {
            return "error " + Marshal.GetLastWin32Error();
        }

        IntPtr buffer = Marshal.AllocHGlobal(needed);
        try
        {
            if (!GetTokenInformation(token, TokenIntegrityLevel, buffer, needed, out needed))
            {
                return "error " + Marshal.GetLastWin32Error();
            }

            IntPtr sid = Marshal.ReadIntPtr(buffer);
            IntPtr text;
            if (!ConvertSidToStringSid(sid, out text))
            {
                return "error " + Marshal.GetLastWin32Error();
            }

            try
            {
                return Marshal.PtrToStringUni(text);
            }
            finally
            {
                LocalFree(text);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string IntValue(IntPtr token, int infoClass)
    {
        IntPtr buffer = Marshal.AllocHGlobal(4);
        try
        {
            int needed;
            if (!GetTokenInformation(token, infoClass, buffer, 4, out needed))
            {
                return "error " + Marshal.GetLastWin32Error();
            }
            return Marshal.ReadInt32(buffer).ToString();
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("usage: ProcessIntegrity.exe <pid>");
            return 2;
        }

        int pid;
        if (!int.TryParse(args[0], out pid))
        {
            Console.WriteLine("invalid pid");
            return 2;
        }

        IntPtr process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process == IntPtr.Zero)
        {
            Console.WriteLine("OpenProcess error " + Marshal.GetLastWin32Error());
            return 1;
        }

        IntPtr token;
        try
        {
            if (!OpenProcessToken(process, TOKEN_QUERY, out token))
            {
                Console.WriteLine("OpenProcessToken error " + Marshal.GetLastWin32Error());
                return 1;
            }

            try
            {
                Console.WriteLine("pid=" + pid);
                Console.WriteLine("integrity=" + IntegritySid(token));
                Console.WriteLine("appContainer=" + IntValue(token, TokenIsAppContainer));
                Console.WriteLine("elevated=" + IntValue(token, TokenElevation));
                return 0;
            }
            finally
            {
                CloseHandle(token);
            }
        }
        finally
        {
            CloseHandle(process);
        }
    }
}

