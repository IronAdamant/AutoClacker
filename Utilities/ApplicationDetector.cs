using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace AutoClicker.Utilities
{
    public class ApplicationDetector
    {
        public List<string> GetRunningApplications()
        {
            return Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                .Select(p => p.ProcessName)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }

        public Process GetProcessByName(string processName)
        {
            return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName))
                .FirstOrDefault(p => !string.IsNullOrEmpty(p.MainWindowTitle));
        }
    }
}