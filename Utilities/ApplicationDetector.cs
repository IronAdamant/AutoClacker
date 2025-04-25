using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AutoClacker.Utilities
{
    public class ApplicationDetector
    {
        public List<string> GetRunningApplications()
        {
            List<string> applications = new List<string>();
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (!string.IsNullOrEmpty(process.MainWindowTitle))
                    {
                        applications.Add(process.ProcessName);
                    }
                }
                catch { }
            }
            var result = applications.OrderBy(app => app).ToList();
            Console.WriteLine($"ApplicationDetector found {result.Count} applications: {string.Join(", ", result)}");
            return result;
        }

        public Process GetProcessByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0 ? processes[0] : null;
        }
    }
}