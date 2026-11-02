using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

internal sealed class LaunchSettings
{
    internal static LaunchSettings Current = Parse("{}");
    internal string AppId = "OpenAI.Codex_2p2nqsd0c76g0!App";
    internal string ProcessName = "ChatGPT";
    internal string WindowTitle = "ChatGPT";

    internal static LaunchSettings Parse(string json)
    {
        var values = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
        if (values == null) throw new InvalidDataException("Expected a JSON settings object.");
        var result = new LaunchSettings(); object value;
        if (values.TryGetValue("appId", out value)) result.AppId = Convert.ToString(value);
        if (values.TryGetValue("processName", out value)) result.ProcessName = Convert.ToString(value);
        if (values.TryGetValue("windowTitle", out value)) result.WindowTitle = Convert.ToString(value);
        if (!Regex.IsMatch(result.AppId, @"\A[A-Za-z0-9_.-]+![A-Za-z0-9_.-]+\z"))
            throw new InvalidDataException("appId must be a packaged Windows application ID. Run Setup.ps1.");
        if (!Regex.IsMatch(result.ProcessName, @"\A[A-Za-z0-9_.-]{1,80}\z") || result.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("processName must be an executable name without .exe.");
        return result;
    }
    internal static void Load(string folder)
    {
        string path = Path.Combine(folder, "launcher.json");
        Current = File.Exists(path) ? Parse(File.ReadAllText(path)) : Parse("{}");
    }
}
