using System;
using System.Threading.Tasks;

internal static class ForegroundHandoff
{
    internal static async Task<bool> ActivateAsync(long startup, long target, Func<bool> valid,
        Func<long> foreground, Func<bool> request, Func<double> clock, Func<int, Task> wait)
    {
        if (startup == 0 || target == 0) return false;
        double started = clock(), nextRequest = started;
        while (valid()) {
            long current = foreground();
            if (current == target) return true;
            if (current != startup && current != 0) return false;
            double now = clock();
            if (now - started >= 1200) return false;
            if (now >= nextRequest) {
                // Cross-process activation is asynchronous. The observed HWND,
                // not an immediate return value, confirms that it has finished.
                request(); nextRequest = clock() + 125;
            }
            await wait(25);
        }
        return false;
    }
}

internal static class WindowReveal
{
    internal static bool CanReveal(long expectedWindow, long currentWindow, long foregroundWindow, bool isAlive, bool isReady)
    {
        return expectedWindow != 0 && isAlive && isReady && currentWindow == expectedWindow && foregroundWindow == expectedWindow;
    }
    internal static async Task<bool> FadeAsync(int durationMs, Action<double> opacity, Func<bool> canReveal, Func<double> clock, Func<int, Task> wait)
    {
        double started = clock(); opacity(1);
        while (true) {
            if (!canReveal()) { opacity(1); return false; }
            double t = Math.Max(0, Math.Min(1, (clock() - started) / Math.Max(1, durationMs)));
            opacity(1 - t * t * (3 - 2 * t));
            if (t >= 1) return true;
            await wait(16);
        }
    }
}
