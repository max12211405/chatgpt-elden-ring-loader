using System;
internal static class StartupProgressTests
{
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); }
    private static int Main()
    {
        try {
            Check(StartupProgress.Percent(false, false, false, false) == 10, "scene preparation");
            Check(StartupProgress.Percent(true, false, false, false) == 30, "activation completed");
            Check(StartupProgress.Percent(true, true, false, false) == 60, "window detected");
            Check(StartupProgress.Percent(true, true, true, false) == 85, "responsive window is still waiting for editor");
            Check(StartupProgress.Percent(true, true, true, true) == 100, "confirmed editor completes progress");
            Check(StartupProgress.Percent(true, false, true, true) == 30, "stale signals cannot complete a missing window");
            Check(StartupProgress.Percent(true, true, false, true) == 100, "editor evidence supersedes responsiveness sample");
            Console.WriteLine("PASS: observed startup stages; 100 requires an existing window and confirmed editor"); return 0;
        } catch (Exception error) { Console.WriteLine("FAIL: " + error.Message); return 1; }
    }
}
