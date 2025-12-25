using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using AutoClacker.Services;
using AutoClacker.Models;

namespace AutoClacker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IInputSimulator _sim;
        private readonly Settings _s;
        private CancellationTokenSource? _cts;
        private string _status = "Stopped";
        private bool _capturing;
        private string _target = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            _s = Settings.Load();
            _sim = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new WindowsInputSimulator()
                 : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? new LinuxInputSimulator()
                 : new MacInputSimulator();
            Logger.Log($"MainViewModel: Simulator={_sim.PlatformName}, Available={_sim.IsAvailable}");
            Logger.Log($"MainViewModel: Settings loaded - Mode={_s.Mode}, Interval={_s.IntervalMs}, KbKey={_s.KeyboardKey}, TriggerKey={_s.TriggerKey}");
        }

        public string Status { get => _status; set { _status = value; Notify(nameof(Status)); } }
        public string Platform => _sim.PlatformName;
        public bool Running { get; private set; }
        public bool Capturing { get => _capturing; set { _capturing = value; Notify(nameof(Capturing)); } }
        public string Target => _target;
        
        public bool IsMouseMode { get => _s.Mode == "Mouse"; set { _s.Mode = value ? "Mouse" : "Keyboard"; Notify(nameof(IsMouseMode)); Save(); } }
        public string MouseBtn { get => _s.MouseButton; set { _s.MouseButton = value; Notify(nameof(MouseBtn)); Save(); } }
        public string ClickType { get => _s.ClickType; set { _s.ClickType = value; Notify(nameof(ClickType)); Save(); } }
        public string KbKey { get => _s.KeyboardKey; set { _s.KeyboardKey = value; Notify(nameof(KbKey)); Save(); } }
        public string TriggerKey { get => _s.TriggerKey; set { _s.TriggerKey = value; Notify(nameof(TriggerKey)); Save(); } }
        public int Interval { get => _s.IntervalMs; set { _s.IntervalMs = Math.Max(10, value); Notify(nameof(Interval)); Save(); } }
        public bool ShowDebugConsole { get => _s.ShowDebugConsole; set { _s.ShowDebugConsole = value; Notify(nameof(ShowDebugConsole)); Save(); } }

        public void StartCapture(string t) { _target = t; Capturing = true; Logger.Log($"StartCapture: target={t}"); }
        public void CaptureKey(string k) { if (!Capturing) return; if (_target == "kb") KbKey = k; else if (_target == "trigger") TriggerKey = k; Capturing = false; Logger.Log($"CaptureKey: {k} for target {_target}"); }

        public void Toggle() { if (Running) Stop(); else Start(); }
        
        public void Start()
        {
            if (Running) { Logger.Log("Start: Already running"); return; }
            Logger.Log($"Start: Beginning loop - Mode={_s.Mode}, Interval={Interval}");
            Running = true;
            Status = "Running";
            Notify(nameof(Running));
            _cts = new();
            Task.Run(() => Loop(_cts.Token));
        }
        
        public void Stop()
        {
            Logger.Log("Stop: Stopping loop");
            Running = false;
            Status = "Stopped";
            Notify(nameof(Running));
            _cts?.Cancel();
        }

        async Task Loop(CancellationToken t)
        {
            Logger.Log($"Loop started - Mode={_s.Mode}, MouseBtn={MouseBtn}, KbKey={KbKey}");
            int count = 0;
            while (!t.IsCancellationRequested)
            {
                try
                {
                    count++;
                    
                    if (_s.Mode == "Mouse")
                        _sim.MouseClick(MouseBtn, ClickType == "Double");
                    else
                        _sim.KeyPress(KbKey);
                    
                    // Update console counter every 10 iterations to reduce overhead
                    if (count % 10 == 0 || count == 1)
                        Logger.UpdateCounter(_s.Mode, count, true);
                    
                    await Task.Delay(Interval, t);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex) { Logger.Log($"Loop error: {ex.Message}"); break; }
            }
            Logger.PrintSummary(_s.Mode, count);
            Logger.Log($"Loop ended after {count} iterations");
        }

        void Save() { _s.Save(); Logger.Log("Settings saved"); }
        void Notify(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
