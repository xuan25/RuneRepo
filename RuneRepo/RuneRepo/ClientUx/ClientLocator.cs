using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RuneRepo.ClientUx
{
    class ClientLocator
    {
        public static string GetLolPath()
        {
            Process[] processes = Process.GetProcessesByName("LeagueClientUx");
            if (processes.Length > 0)
            {
                Process process = processes[0];
                try
                {
                    string filename = process.MainModule.FileName;
                    string lolPath = Path.GetDirectoryName(filename);
                    return lolPath;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Tencent client
                    string filename = GetProcessName(process.Id);                                   // The process is protected by tencent, using Win32Api "GetModuleFileNameEx" to get the filename
                    string lolPath = Path.GetDirectoryName(filename);
                    lolPath = Path.Combine(new DirectoryInfo(lolPath).Parent.FullName, "Game");     // Executable is in "LeagueClient\\" while log is in "Game\\"
                    lolPath = Encoding.Default.GetString(Encoding.UTF8.GetBytes(lolPath));          // Decode with UTF-8 and encode with Default (e.g. GB2312 for 简体中文)
                    DirectoryInfo directoryInfo = new DirectoryInfo(lolPath);
                    if (directoryInfo.Exists)
                        return directoryInfo.FullName;
                    else
                        return null;
                }
            }
            return null;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, int nSize);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private static string GetProcessName(int pid)
        {
            IntPtr processHandle = OpenProcess(0x1000, false, pid);
            if (processHandle == IntPtr.Zero)
            {
                return null;
            }
            StringBuilder stringBuilder = new StringBuilder(4096);
            string result = null;
            if (GetModuleFileNameEx(processHandle, IntPtr.Zero, stringBuilder, stringBuilder.Capacity) > 0)
            {
                result = stringBuilder.ToString();
            }
            CloseHandle(processHandle);
            return result;
        }
    }
}
