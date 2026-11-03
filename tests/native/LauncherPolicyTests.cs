using System;
internal static class LauncherPolicyTests
{
    private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); }
    private static int Main()
    {
        try { Run(); return 0; }
        catch (Exception error) { Console.WriteLine("FAIL: " + error.Message); return 1; }
    }
    private static void Run()
    {
        Check(KeyboardEntryPolicy.CanTrigger(13, false, true, false), "fresh Enter can enter when ready");
        Check(!KeyboardEntryPolicy.CanTrigger(13, true, true, false), "holding Enter must not repeat");
        Check(!KeyboardEntryPolicy.CanTrigger(13, false, false, false), "native Enter must not bypass readiness");
        Check(!KeyboardEntryPolicy.CanTrigger(13, false, true, true), "native Enter must not duplicate an active handoff");
        Check(!KeyboardEntryPolicy.CanTrigger(32, false, true, false), "other native keys must keep normal behavior");
        Check(WarmLaunchPolicy.IsMainCommandLine("\"C:\\Apps\\ChatGPT.exe\""), "actual app main process permits warm launch");
        Check(!WarmLaunchPolicy.IsMainCommandLine("\"C:\\Apps\\ChatGPT.exe\" --type=renderer"), "leftover renderer must not suppress animation");
        Check(!WarmLaunchPolicy.IsMainCommandLine("ChatGPT.exe --type=crashpad-handler"), "leftover crash handler must not suppress animation");
        Check(!WarmLaunchPolicy.IsMainCommandLine("ChatGPT.exe --type=gpu-process"), "GPU helper must not suppress animation");
        Check(!WarmLaunchPolicy.IsMainCommandLine("ChatGPT.exe --type=utility"), "utility helper must not suppress animation");
        Check(!WarmLaunchPolicy.IsMainCommandLine("ChatGPT.exe \"--type=renderer\""), "quoted helper switch must not count as main");
        Check(!WarmLaunchPolicy.IsMainCommandLine(null), "unidentified process must not suppress animation");
        var gate = new LaunchGate();
        Check(!gate.TryEnter(), "waiting must block entry");
        gate.SetReady(true); Check(gate.TryEnter() && !gate.TryEnter(), "entry must occur once");
        var probe = new ReadinessProbe();
        probe.SetTarget(100); int oldRevision = probe.Begin();
        Check(!probe.Ready, "a visible window alone is not readiness");
        probe.SetTarget(200); int newRevision = probe.Begin();
        Check(!probe.Complete(100, oldRevision, true), "old editor results must be rejected");
        Check(probe.InFlight && !probe.Ready, "old result must not alter a new scan");
        Check(probe.Complete(200, newRevision, false) && !probe.Ready && !probe.InFlight, "no editor must stay waiting and permit retry");
        int retry = probe.Begin(); Check(probe.Complete(200, retry, true) && probe.Ready, "new editor must become ready");
        probe.SetTarget(0); Check(!probe.Ready && !probe.InFlight, "target disappearance must clear readiness");
        Check(!probe.Complete(200, retry, true), "late editor result must not revive a closed target");
        Console.WriteLine("PASS: main vs renderer/crash/GPU/utility process; entry guard, actual editor, retry, replacement, late result, target closure");
    }
}
