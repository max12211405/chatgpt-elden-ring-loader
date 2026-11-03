using System;
using System.IO;
internal static class LaunchSettingsTests
{
    private static int Main()
    {
        var custom = LaunchSettings.Parse("{\"appId\":\"Example.Codex_abc123!App\",\"processName\":\"Codex\",\"windowTitle\":\"\"}");
        if (custom.AppId != "Example.Codex_abc123!App" || custom.ProcessName != "Codex" || custom.WindowTitle != "") throw new Exception("Custom installation settings were lost.");
        foreach (string json in new[] {"null", "{\"appId\":\"https://example.com\"}", "{\"appId\":\"\"}", "{\"processName\":\"x' OR 1=1\"}", "{\"processName\":\"Codex.exe\"}", "{\"processName\":\"../Codex\"}"}) {
            bool rejected = false;
            try { LaunchSettings.Parse(json); } catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new Exception("Invalid settings were accepted: " + json);
        }
        if (LaunchSettings.Parse("{}").AppId != "OpenAI.Codex_2p2nqsd0c76g0!App") throw new Exception("Default target changed.");
        Console.WriteLine("PASS: installation-specific settings and six invalid-input cases"); return 0;
    }
}
