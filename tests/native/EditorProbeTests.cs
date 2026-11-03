using System;
using System.Diagnostics;
using System.Threading;
internal static class EditorProbeTests
{
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--probe-editor") {
            if (args[1] == "2") Thread.Sleep(10000);
            return args[1] == "1" ? 0 : args[1] == "3" ? 2 : 1;
        }
        try {
            string executable = Process.GetCurrentProcess().MainModule.FileName;
            Check(EditorProbe.CheckAsync(executable, 1, 2000, CancellationToken.None).Result == EditorProbeStatus.Ready, "ready result");
            Check(EditorProbe.CheckAsync(executable, 0, 2000, CancellationToken.None).Result == EditorProbeStatus.Waiting, "waiting result");
            Check(EditorProbe.CheckAsync(executable, 3, 2000, CancellationToken.None).Result == EditorProbeStatus.Unresponsive, "unresponsive result");
            var clock = Stopwatch.StartNew();
            Check(EditorProbe.CheckAsync(executable, 2, 250, CancellationToken.None).Result == EditorProbeStatus.TimedOut, "hung provider times out");
            Check(clock.ElapsedMilliseconds < 2000, "hung worker is stopped within a bound");
            using (var cancelled = new CancellationTokenSource()) {
                var pending = EditorProbe.CheckAsync(executable, 2, 5000, cancelled.Token); Thread.Sleep(100); cancelled.Cancel();
                Check(pending.Result == EditorProbeStatus.Cancelled, "closing cancels owned worker");
            }
            Check(Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length == 1, "no probe worker left behind");
            Console.WriteLine("PASS: ready/waiting/unresponsive, hung-provider timeout, cancellation, no owned worker remains"); return 0;
        } catch (Exception e) { Console.WriteLine("FAIL: " + e.Message); return 1; }
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
