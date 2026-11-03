using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

internal enum EditorProbeStatus { Ready, Waiting, Unresponsive, TimedOut, Cancelled, Unavailable }
internal static class EditorProbe
{
    // UI Automation providers can block synchronously. Keep that work in an owned,
    // hidden worker so a timeout can actually stop it, rather than leak a thread.
    internal static Task<EditorProbeStatus> CheckAsync(string executable, long window, int timeoutMs, CancellationToken cancellation)
    {
        return Task.Run(delegate {
            if (cancellation.IsCancellationRequested) return EditorProbeStatus.Cancelled;
            using (var process = new Process()) {
                process.StartInfo = new ProcessStartInfo(executable, "--probe-editor " + window.ToString(CultureInfo.InvariantCulture)) {
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
                };
                try {
                    process.Start();
                    using (cancellation.Register(delegate { Stop(process); })) {
                        var clock = Stopwatch.StartNew();
                        while (!process.WaitForExit(50)) {
                            if (cancellation.IsCancellationRequested) return EditorProbeStatus.Cancelled;
                            if (clock.ElapsedMilliseconds >= timeoutMs) return EditorProbeStatus.TimedOut;
                        }
                        if (cancellation.IsCancellationRequested) return EditorProbeStatus.Cancelled;
                        return process.ExitCode == 0 ? EditorProbeStatus.Ready :
                            process.ExitCode == 1 ? EditorProbeStatus.Waiting :
                            process.ExitCode == 2 ? EditorProbeStatus.Unresponsive : EditorProbeStatus.Unavailable;
                    }
                } catch (Exception) { return cancellation.IsCancellationRequested ? EditorProbeStatus.Cancelled : EditorProbeStatus.Unavailable; }
                finally { Stop(process); }
            }
        });
    }
    private static void Stop(Process process)
    {
        try { if (!process.HasExited) { process.Kill(); process.WaitForExit(500); } }
        catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { }
    }
}
