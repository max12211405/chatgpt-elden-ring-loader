using System;
internal static class RecoveryPolicyTests
{
    private static int Main()
    {
        try {
            Check(RecoveryPolicy.Decide("open-directly", false) == RecoveryAction.Retry, "unready direct must retry, never enter");
            Check(RecoveryPolicy.Decide("enter", false) == RecoveryAction.Retry, "stale normal enter must retry");
            Check(RecoveryPolicy.Decide("enter", true) == RecoveryAction.Enter, "confirmed ready can enter");
            Check(RecoveryPolicy.Decide("open-directly", true) == RecoveryAction.Enter, "legacy direct still requires readiness");
            Check(RecoveryPolicy.Decide("retry-readiness", true) == RecoveryAction.Retry, "check again is not an entry action");
            Check(RecoveryPolicy.Decide("other", true) == RecoveryAction.Ignore, "unknown messages ignored");
            Console.WriteLine("PASS: all entry messages require editor readiness; recovery only retries"); return 0;
        } catch (Exception e) { Console.WriteLine("FAIL: " + e.Message); return 1; }
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
