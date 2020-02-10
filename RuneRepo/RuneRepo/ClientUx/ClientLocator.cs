using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                string filename = process.MainModule.FileName;
                string lolPath = Path.GetDirectoryName(filename);
                return lolPath;
            }
            return null;
        }
    }
}
