using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

internal static class HandoffPreparation
{
    internal static async Task<bool> PrepareAsync(Func<bool> valid, Func<bool> maximized, Func<bool> requestMaximize)
    {
        if (!valid()) return false;
        if (!maximized() && !requestMaximize()) return false;
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (valid()) {
            if (maximized()) return true;
            if (deadline.ElapsedMilliseconds >= 2000) return false;
            await Task.Delay(50);
        }
        return false;
    }
}

internal static class WarmLaunchPolicy
{
    internal static bool IsMainCommandLine(string commandLine)
    {
        return !String.IsNullOrWhiteSpace(commandLine) &&
            !Regex.IsMatch(commandLine, "(?:^|\\s)\"?--type(?:=|\\s|\"|$)", RegexOptions.IgnoreCase);
    }
}

internal static class KeyboardEntryPolicy
{
    internal static bool CanTrigger(int virtualKey, bool repeated, bool ready, bool busy)
    {
        return virtualKey == 13 && !repeated && ready && !busy;
    }
}

internal sealed class LaunchGate
{
    public bool Ready { get; private set; }
    public bool Entered { get; private set; }
    public void SetReady(bool ready) { Ready = ready; }
    public bool TryEnter()
    {
        if (!Ready || Entered) return false;
        Entered = true;
        return true;
    }
}

// A scan result belongs to one particular window generation.
internal sealed class ReadinessProbe
{
    public long Target { get; private set; }
    public int Revision { get; private set; }
    public bool InFlight { get; private set; }
    public bool Ready { get; private set; }
    public void SetTarget(long target)
    {
        if (Target == target) return;
        Target = target; Revision++; InFlight = false; Ready = false;
    }
    public int Begin() { InFlight = true; return Revision; }
    public bool Complete(long target, int revision, bool editorPresent)
    {
        if (target == 0 || target != Target || revision != Revision) return false;
        InFlight = false; Ready = editorPresent; return true;
    }
}
