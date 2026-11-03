using System;
using System.Collections.Generic;
using System.Threading.Tasks;
internal static class WindowRevealTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static int Main()
    {
        try { Run().GetAwaiter().GetResult(); Console.WriteLine("PASS: gradual real-window reveal, bounded duration, disappearing target recovery"); return 0; }
        catch (Exception error) { Console.WriteLine("FAIL: " + error.Message); return 1; }
    }
    private static async Task Run()
    {
        Check(WindowReveal.CanReveal(10, 10, 10, true, true), "matching live foreground chat may be revealed");
        Check(!WindowReveal.CanReveal(10, 20, 10, true, true), "a replaced current target must stay covered even if the old HWND survives");
        Check(!WindowReveal.CanReveal(10, 10, 20, true, true), "losing foreground to another app must restore the cover");
        Check(!WindowReveal.CanReveal(10, 10, 0, true, true), "refused foreground activation must not reveal desktop");
        double clock = 0; var samples = new List<double>();
        bool completed = await WindowReveal.FadeAsync(780, samples.Add, delegate { return true; }, delegate { return clock; }, delegate(int ms) { clock += ms; return Task.FromResult(0); });
        Check(completed && clock >= 780 && clock < 820, "handoff must wait for the gradual reveal");
        Check(samples.Count > 30 && samples[0] == 1 && samples[samples.Count - 1] == 0, "actual opacity must progress through intermediate frames");
        bool intermediate = false;
        for (int i = 1; i < samples.Count; i++) { Check(samples[i] <= samples[i - 1], "opacity must decrease without jumps backwards"); if (samples[i] > .1 && samples[i] < .9) intermediate = true; }
        Check(intermediate, "must reveal the real app progressively");
        clock = 0; samples.Clear();
        completed = await WindowReveal.FadeAsync(780, samples.Add, delegate { return clock < 320; }, delegate { return clock; }, delegate(int ms) { clock += ms; return Task.FromResult(0); });
        Check(!completed && samples[samples.Count - 1] == 1, "closing or replacing the target must restore the opaque cover");
        clock = 0; samples.Clear();
        completed = await WindowReveal.FadeAsync(780, samples.Add, delegate { return WindowReveal.CanReveal(10, clock < 320 ? 10 : 20, 10, true, true); }, delegate { return clock; }, delegate(int ms) { clock += ms; return Task.FromResult(0); });
        Check(!completed && samples[samples.Count - 1] == 1, "current-window replacement must abort before revealing it");
        clock = 0; samples.Clear();
        completed = await WindowReveal.FadeAsync(780, samples.Add, delegate { return WindowReveal.CanReveal(10, 10, clock < 320 ? 10 : 20, true, true); }, delegate { return clock; }, delegate(int ms) { clock += ms; return Task.FromResult(0); });
        Check(!completed && samples[samples.Count - 1] == 1, "lost foreground must abort before full transparency");
    }
}
