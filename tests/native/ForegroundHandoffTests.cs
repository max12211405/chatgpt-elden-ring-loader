using System;
using System.Threading.Tasks;

internal static class ForegroundHandoffTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static int Main()
    {
        try { Run().GetAwaiter().GetResult(); Console.WriteLine("PASS: one entry waits for asynchronous activation, retries denial, bounds timeout, respects other apps and target changes"); return 0; }
        catch (Exception error) { Console.WriteLine("FAIL: " + error.Message); return 1; }
    }
    private static async Task Run()
    {
        double clock = 0; int requests = 0;
        bool result = await ForegroundHandoff.ActivateAsync(10, 20, () => true,
            () => clock >= 75 ? 20 : 10, () => { requests++; return true; },
            () => clock, ms => { clock += ms; return Task.FromResult(0); });
        Check(result && clock >= 75 && requests == 1,
            "a successful but asynchronous activation must finish from the first entry instead of resetting the scene");

        clock = 0; requests = 0; long foreground = 10;
        result = await ForegroundHandoff.ActivateAsync(10, 20, () => true,
            () => foreground, () => { if (++requests == 3) foreground = 20; return requests >= 3; },
            () => clock, ms => { clock += ms; return Task.FromResult(0); });
        Check(result && requests == 3 && clock > 0 && clock < 1200,
            "two refused native requests must recover inside one entry, without three user clicks");

        clock = 0; requests = 0;
        result = await ForegroundHandoff.ActivateAsync(10, 20, () => true,
            () => 10, () => { requests++; return false; },
            () => clock, ms => { clock += ms; return Task.FromResult(0); });
        Check(!result && clock >= 1200 && clock <= 1225 && requests <= 12,
            "permanent refusal must return within a bounded wait");

        clock = 0; requests = 0;
        result = await ForegroundHandoff.ActivateAsync(10, 20, () => true,
            () => clock < 50 ? 10 : 30, () => { requests++; return true; },
            () => clock, ms => { clock += ms; return Task.FromResult(0); });
        Check(!result && clock == 50 && requests == 1,
            "switching to an unrelated app must cancel rather than steal focus back");

        clock = 0; requests = 0;
        result = await ForegroundHandoff.ActivateAsync(10, 20, () => clock < 50,
            () => 10, () => { requests++; return true; },
            () => clock, ms => { clock += ms; return Task.FromResult(0); });
        Check(!result && clock == 50 && requests == 1,
            "a closed, replaced or no-longer-ready target must cancel pending activation");

        requests = 0;
        result = await ForegroundHandoff.ActivateAsync(10, 20, () => true,
            () => 20, () => { requests++; return false; }, () => 0, ms => Task.FromResult(0));
        Check(result && requests == 0, "an already active target needs no further activation call");

        requests = 0;
        result = await ForegroundHandoff.ActivateAsync(10, 20, () => true,
            () => 30, () => { requests++; return true; }, () => 0, ms => Task.FromResult(0));
        Check(!result && requests == 0, "an unrelated foreground app must not receive activation attempts");

        clock = 0; foreground = 10;
        result = await ForegroundHandoff.ActivateAsync(10, 20, () => true,
            () => foreground, () => { foreground = 20; return false; },
            () => clock, ms => { clock += ms; return Task.FromResult(0); });
        Check(result, "observed target activation wins over a stale native return value");

        clock = 0;
        result = await ForegroundHandoff.ActivateAsync(10, 20, () => true,
            () => clock < 25 ? 10 : clock < 75 ? 0 : 20, () => true,
            () => clock, ms => { clock += ms; return Task.FromResult(0); });
        Check(result, "a transient null foreground during activation must not require another click");
    }
}
