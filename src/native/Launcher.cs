using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

internal sealed class GraceLauncher : Form
{
    private readonly WebView2 web = new WebView2();
    private readonly LaunchGate gate = new LaunchGate();
    private readonly ReadinessProbe probe = new ReadinessProbe();
    private readonly CancellationTokenSource probeLifetime = new CancellationTokenSource();
    private CancellationTokenSource activeScan;
    private readonly System.Windows.Forms.Timer poll = new System.Windows.Forms.Timer();
    private readonly bool demo;
    private readonly string folder = AppDomain.CurrentDomain.BaseDirectory;
    private IntPtr target;
    private bool frontend, handedOff, closing, recoveryPublished, preparingHandoff, keyboardQueued, enterHeld, engineFailed, appActivated, targetResponsive;
    private DateTime nextScan, startupTime = DateTime.UtcNow;
    private Process browserProcess;
    private int frameLeft, frameRight, frameBottom;
    internal GraceLauncher(bool demonstration)
    {
        demo = demonstration; Text = "ChatGPT"; BackColor = Color.FromArgb(8, 11, 11);
        AutoScaleMode = AutoScaleMode.None;
        MinimumSize = new Size(720, 460); StartPosition = FormStartPosition.Manual;
        IntPtr existingChat = NativeTarget.FindWindow();
        Bounds = (existingChat == IntPtr.Zero ? Screen.FromPoint(Cursor.Position) : Screen.FromHandle(existingChat)).Bounds;
        WindowState = FormWindowState.Normal; TopMost = true;
        FormBorderStyle = FormBorderStyle.Sizable; MinimizeBox = true; MaximizeBox = true; ControlBox = true;
        KeyPreview = true;
        try { Icon = Icon.ExtractAssociatedIcon(Path.Combine(folder, "ChatGPTGraceLauncher.exe")); } catch { }
        web.Dock = DockStyle.Fill; web.DefaultBackgroundColor = BackColor; Controls.Add(web);
        web.KeyDown += delegate(object sender, KeyEventArgs key) {
            if (key.KeyCode != Keys.Enter || key.Modifiers != Keys.None) return;
            key.Handled = true; key.SuppressKeyPress = true;
            QueueNativeEnter(enterHeld); enterHeld = true;
        };
        web.KeyUp += delegate(object sender, KeyEventArgs key) { if (key.KeyCode == Keys.Enter) enterHeld = false; };
        KeyUp += delegate(object sender, KeyEventArgs key) { if (key.KeyCode == Keys.Enter) enterHeld = false; };
        poll.Interval = 500; poll.Tick += CheckTarget;
        Shown += async delegate {
            WindowState = FormWindowState.Normal;
            FillMonitor(Screen.FromHandle(Handle));
            await Initialize();
        };
        FormClosing += delegate { closing = true; probeLifetime.Cancel(); CancelEditorScan(); MuteSound(); };
        FormClosed += delegate {
            closing = true;
            poll.Stop();
            var owned = new List<Process>();
            try {
                if (web.CoreWebView2 != null) foreach (var info in web.CoreWebView2.Environment.GetProcessInfos()) {
                    try { var child = Process.GetProcessById(info.ProcessId); var handle = child.Handle; owned.Add(child); } catch (ArgumentException) { } catch (System.ComponentModel.Win32Exception) { }
                }
            } catch (Exception error) { Log("renderer inventory: " + error.GetType().Name); }
            web.Dispose();
            if (browserProcess != null) {
                try {
                    if (!browserProcess.WaitForExit(700)) { browserProcess.Kill(); browserProcess.WaitForExit(1200); }
                    Log("owned renderer exited: " + browserProcess.HasExited);
                } catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { }
                browserProcess.Dispose();
            }
            int remaining = 0;
            foreach (var child in owned) {
                try { if (!child.HasExited && !child.WaitForExit(300)) { child.Kill(); child.WaitForExit(1000); } if (!child.HasExited) remaining++; }
                catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { remaining++; }
                finally { child.Dispose(); }
            }
            Log("owned WebView processes remaining: " + remaining + " of " + owned.Count);
        };
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e); int enabled = 1;
        try { NativeTarget.DwmSetWindowAttribute(Handle, 20, ref enabled, 4); } catch { }
        try {
            int square = 1, noBorder = unchecked((int)0xFFFFFFFE);
            NativeTarget.DwmSetWindowAttribute(Handle, 33, ref square, 4);
            NativeTarget.DwmSetWindowAttribute(Handle, 34, ref noBorder, 4);
        } catch { }
    }
    private void FillMonitor(Screen screen)
    {
        if (closing || IsDisposed || !IsHandleCreated || WindowState == FormWindowState.Minimized) return;
        WindowState = FormWindowState.Normal;
        NativeTarget.Rect window, physical;
        if (!NativeTarget.GetWindowRect(Handle, out window)) return;
        var client = RectangleToScreen(ClientRectangle);
        frameLeft = Math.Max(0, client.Left - window.Left);
        frameRight = Math.Max(0, window.Right - client.Right);
        frameBottom = Math.Max(0, window.Bottom - client.Bottom);
        Rectangle monitor = NativeTarget.TryGetPhysicalDisplay(screen.DeviceName, out physical)
            ? Rectangle.FromLTRB(physical.Left, physical.Top, physical.Right, physical.Bottom) : screen.Bounds;
        Bounds = WindowEdgeLayout.Expand(monitor, frameLeft, frameRight, frameBottom);
        PerformLayout();
    }
    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.Enter) { QueueNativeEnter((message.LParam.ToInt64() & 0x40000000L) != 0); return true; }
        return base.ProcessCmdKey(ref message, keyData);
    }
    private void QueueNativeEnter(bool repeated)
    {
        if (!frontend || IsDisposed || !KeyboardEntryPolicy.CanTrigger(13, repeated, gate.Ready, preparingHandoff || handedOff || closing || keyboardQueued)) return;
        keyboardQueued = true;
        try {
            BeginInvoke((Action)(async delegate {
                try {
                    if (closing || IsDisposed || web.CoreWebView2 == null) return;
                    string accepted = await web.CoreWebView2.ExecuteScriptAsync("(()=>{if(!window.GraceStartup)return false;if(window.preview?.state!=='ready')GraceStartup.setReady(true);return GraceStartup.enterFromKeyboard();})()");
                    Log("native Enter request: " + accepted);
                } catch (Exception error) { Log("native Enter: " + error.GetType().Name); }
                finally { keyboardQueued = false; }
            }));
        } catch (InvalidOperationException) { keyboardQueued = false; }
    }
    private async void FocusReadyPrompt()
    {
        if (!frontend || closing || handedOff || preparingHandoff || IsDisposed || WindowState == FormWindowState.Minimized) return;
        try {
            bool focused = NativeTarget.FocusStartupPrompt(Handle, target);
            web.Select(); web.Focus();
            string documentFocus = await web.CoreWebView2.ExecuteScriptAsync("document.hasFocus()");
            Log("startup keyboard focus: native=" + focused + " document=" + documentFocus);
        } catch (Exception error) { Log("startup focus: " + error.GetType().Name); }
    }
    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
        if (message.Msg == 0x02E0 && IsHandleCreated && !closing) { // WM_DPICHANGED
            BeginInvoke((Action)delegate { if (!closing && !IsDisposed) FillMonitor(Screen.FromHandle(Handle)); });
        }
        if (message.Msg != 0x24) return; // WM_GETMINMAXINFO
        NativeTarget.Rect display;
        if (!NativeTarget.TryGetPhysicalDisplay(Screen.FromHandle(message.HWnd).DeviceName, out display)) return;
        var info = (NativeTarget.MinMaxInfo)System.Runtime.InteropServices.Marshal.PtrToStructure(message.LParam, typeof(NativeTarget.MinMaxInfo));
        info.MaxPositionX = -frameLeft; info.MaxPositionY = 0;
        info.MaxSizeX = display.Right - display.Left + frameLeft + frameRight;
        info.MaxSizeY = display.Bottom - display.Top + frameBottom;
        info.MaxTrackX = Math.Max(info.MaxTrackX, info.MaxSizeX); info.MaxTrackY = Math.Max(info.MaxTrackY, info.MaxSizeY);
        System.Runtime.InteropServices.Marshal.StructureToPtr(info, message.LParam, false);
    }
    private void Log(string message)
    {
        try { File.AppendAllText(Path.Combine(folder, "launcher.log"), DateTime.Now.ToString("s") + " " + message + Environment.NewLine); } catch { }
    }
    private void MuteSound()
    {
        try { if (web.CoreWebView2 != null) web.CoreWebView2.IsMuted = true; }
        catch (InvalidOperationException) { }
        catch (System.Runtime.InteropServices.COMException) { }
    }
    private void LogCoverage(IntPtr chat)
    {
        NativeTarget.Rect sceneRect, chatRect;
        if (chat == IntPtr.Zero || !NativeTarget.GetWindowRect(Handle, out sceneRect) || !NativeTarget.GetWindowRect(chat, out chatRect)) return;
        bool covers = sceneRect.Left <= chatRect.Left + 8 && sceneRect.Top <= chatRect.Top + 8 && sceneRect.Right >= chatRect.Right - 8 && sceneRect.Bottom >= chatRect.Bottom - 8;
        var display = Screen.FromHandle(Handle).Bounds;
        NativeTarget.Rect physical;
        bool measuredPhysical = NativeTarget.TryGetPhysicalDisplay(Screen.FromHandle(Handle).DeviceName, out physical);
        bool dpiAware = NativeTarget.AreDpiAwarenessContextsEqual(NativeTarget.GetThreadDpiAwarenessContext(), new IntPtr(-4));
        var image = web.RectangleToScreen(web.ClientRectangle);
        Log("window coverage: " + new JavaScriptSerializer().Serialize(new { coversChat = covers, scene = sceneRect, image = new { Left = image.Left, Top = image.Top, Right = image.Right, Bottom = image.Bottom }, chat = chatRect, monitor = new { Left = display.Left, Top = display.Top, Right = display.Right, Bottom = display.Bottom }, physicalMonitor = physical, physicalMeasured = measuredPhysical, perMonitorV2 = dpiAware, state = WindowState.ToString() }));
    }
    private void CoverChatWindow(IntPtr chat)
    {
        if (chat == IntPtr.Zero || WindowState == FormWindowState.Minimized) return;
        var destination = Screen.FromHandle(chat);
        if (Screen.FromHandle(Handle).DeviceName != destination.DeviceName) {
            FillMonitor(destination);
        }
        // Keep the real app painted and accessible behind the scene, so its
        // editor can be detected without briefly revealing a second window.
        if (!NativeTarget.IsZoomed(chat)) NativeTarget.ShowWindowAsync(chat, 3); // SW_MAXIMIZE, under the scene
        TopMost = true; Activate(); web.Focus();
        LogCoverage(chat);
    }
    private async Task Initialize()
    {
        try {
            string runtime = null;
            string runtimeRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "EdgeWebView", "Application");
            if (Directory.Exists(runtimeRoot)) runtime = Directory.GetDirectories(runtimeRoot).Where(p => File.Exists(Path.Combine(p, "msedgewebview2.exe"))).OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            var options = new CoreWebView2EnvironmentOptions();
            options.AdditionalBrowserArguments = "--autoplay-policy=no-user-gesture-required";
            var environment = await CoreWebView2Environment.CreateAsync(runtime, Path.Combine(folder, "WebViewData"), options);
            if (closing || IsDisposed) return;
            await web.EnsureCoreWebView2Async(environment);
            if (closing || IsDisposed) return;
            browserProcess = Process.GetProcessById((int)web.CoreWebView2.BrowserProcessId);
            Log("owned browser pid: " + browserProcess.Id);
            web.CoreWebView2.Settings.AreDevToolsEnabled = false; web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            web.CoreWebView2.Settings.IsStatusBarEnabled = false; web.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            web.CoreWebView2.NewWindowRequested += delegate(object sender, CoreWebView2NewWindowRequestedEventArgs e) { e.Handled = true; };
            web.CoreWebView2.NavigationStarting += delegate(object sender, CoreWebView2NavigationStartingEventArgs e) {
                Uri uri; if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out uri) || !uri.IsFile || !String.Equals(Path.GetFullPath(uri.LocalPath), Path.Combine(folder, "startup.html"), StringComparison.OrdinalIgnoreCase)) e.Cancel = true;
            };
            web.CoreWebView2.NavigationCompleted += delegate { frontend = true; PublishReady(); };
            web.CoreWebView2.ProcessFailed += HandleEngineFailure;
            web.CoreWebView2.WebMessageReceived += HandleMessage;
            web.Source = new Uri(Path.Combine(folder, "startup.html") + (demo ? "?demo=1" : ""));
            if (!demo) {
                target = NativeTarget.FindWindow();
                await Task.Run(delegate { NativeTarget.Launch(); });
                if (closing || IsDisposed) return;
                appActivated = true; PublishReady();
                Log("activated ChatGPT");
                try { string iconPath = NativeTarget.ExportTargetIcon(folder); if (iconPath != null) Icon = new Icon(iconPath); } catch (Exception error) { Log("icon: " + error.GetType().Name); }
                poll.Start();
            }
            Log(demo ? "demo started" : "startup started");
        } catch (Exception error) {
            if (closing || IsDisposed) return;
            Log("initialization failed: " + error.GetType().Name + " " + error.Message);
            MessageBox.Show(this, "The startup scene could not open. ChatGPT will open directly.", "ChatGPT", MessageBoxButtons.OK, MessageBoxIcon.Information);
            handedOff = true; try { NativeTarget.Launch(); } catch { } Close();
        }
    }
    private async void HandleEngineFailure(object sender, CoreWebView2ProcessFailedEventArgs e)
    {
        Log("animation engine failed: kind=" + e.ProcessFailedKind + " reason=" + e.Reason + " exit=" + e.ExitCode);
        // GPU/utility subprocesses can recover themselves. A lost browser or
        // main renderer cannot deliver the page's entry message any more.
        if (closing || handedOff || engineFailed ||
            (e.ProcessFailedKind != CoreWebView2ProcessFailedKind.BrowserProcessExited &&
             e.ProcessFailedKind != CoreWebView2ProcessFailedKind.RenderProcessExited)) return;
        engineFailed = true; poll.Stop(); MuteSound();
        try {
            await Task.Run(delegate { NativeTarget.Launch(); });
            Log("animation engine fallback: activated the actual chat directly");
        } catch (Exception error) { Log("animation engine fallback: " + error.GetType().Name); }
        if (!closing && !IsDisposed) { handedOff = true; Close(); }
    }
    private async void HandleMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (preparingHandoff) return;
        try {
            var message = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
            object kind;
            if (!message.TryGetValue("type", out kind) || gate.Entered || handedOff || closing) return;
            RecoveryAction action = RecoveryPolicy.Decide(Convert.ToString(kind), gate.Ready);
            if (action == RecoveryAction.Ignore) return;
            if (action == RecoveryAction.Retry) {
                if (Convert.ToString(kind) != "retry-readiness") web.CoreWebView2.PostWebMessageAsJson("{\"type\":\"reset-entry\"}");
                RetryReadiness(); return;
            }
            preparingHandoff = true;
            if (closing || IsDisposed) return;
            IntPtr found = NativeTarget.FindWindow();
            if (!NativeTarget.IsWindow(found) || !gate.Ready || found.ToInt64() != probe.Target) {
                target = found; probe.SetTarget(found.ToInt64()); gate.SetReady(false); targetResponsive = false;
                Log("handoff deferred: chat target changed or is not ready");
                web.CoreWebView2.PostWebMessageAsJson("{\"type\":\"reset-entry\"}"); PublishReady(); return;
            }
            target = found;
            bool maximized = await NativeTarget.MaximizeForHandoffAsync(found);
            if (closing || IsDisposed) return;
            if (!maximized || NativeTarget.FindWindow() != found ||
                !gate.Ready || probe.Target != found.ToInt64())
                throw new InvalidOperationException("Chat window was not ready and maximized for handoff");
            Log("handoff target verified maximized: " + found.ToInt64());
            MuteSound();
            // Hide the GPU child once it reaches the same flat color as this
            // native cover. Only the lightweight cover fades over the real app.
            BackColor = Color.FromArgb(24, 24, 24); web.Visible = false; Refresh();
            bool activated = await NativeTarget.ActivateForHandoffAsync(found, Handle,
                delegate { return !closing && !IsDisposed && NativeTarget.FindWindow() == found && gate.Ready && probe.Target == found.ToInt64(); }, Log);
            if (closing || IsDisposed) return;
            if (!activated) throw new InvalidOperationException("Chat foreground activation did not complete");
            var revealClock = Stopwatch.StartNew();
            bool revealed = await WindowReveal.FadeAsync(780,
                delegate(double alpha) { if (!IsDisposed) Opacity = alpha; },
                delegate { return !closing && !IsDisposed && WindowReveal.CanReveal(found.ToInt64(), NativeTarget.FindWindow().ToInt64(), NativeTarget.GetForegroundWindow().ToInt64(), NativeTarget.IsWindow(found), gate.Ready && probe.Target == found.ToInt64()); },
                delegate { return revealClock.Elapsed.TotalMilliseconds; },
                delegate(int ms) { return Task.Delay(ms); });
            if (closing || IsDisposed) return;
            if (!revealed || NativeTarget.FindWindow() != found || NativeTarget.GetForegroundWindow() != found || !gate.TryEnter())
                throw new InvalidOperationException("Chat target changed during the gradual reveal");
            handedOff = true; poll.Stop(); Hide(); TopMost = false;
            Log("chat reveal completed: " + revealClock.ElapsedMilliseconds + " ms");
            Log("handoff completed"); Close();
        } catch (Exception error) {
            Log("message failed: " + error.GetType().Name + " " + error.Message);
            if (!closing && !IsDisposed && web.CoreWebView2 != null) {
                CancelEditorScan(); probe.Restart(); gate.SetReady(false); nextScan = DateTime.MinValue;
                enterHeld = false; Opacity = 1; web.Visible = true; TopMost = true; Activate(); web.CoreWebView2.IsMuted = false;
                web.CoreWebView2.PostWebMessageAsJson("{\"type\":\"reset-entry\",\"ready\":" + (gate.Ready ? "true" : "false") + "}");
                PublishReady();
                web.CoreWebView2.PostWebMessageAsJson("{\"type\":\"recovery\",\"visible\":true}");
            }
        } finally { preparingHandoff = false; }
    }
    private void CancelEditorScan()
    {
        if (activeScan != null) activeScan.Cancel();
    }
    private void RetryReadiness()
    {
        if (gate.Ready) { PublishReady(); return; }
        Log("recovery: rechecking editor; unready chat will not be revealed");
        CancelEditorScan(); probe.Restart(); gate.SetReady(false); nextScan = DateTime.MinValue;
        if (target == IntPtr.Zero) {
            PublishReady(); web.CoreWebView2.PostWebMessageAsJson("{\"type\":\"recovery\",\"visible\":true}");
        }
    }
    private async void CheckTarget(object sender, EventArgs e)
    {
        if (handedOff || demo || closing || IsDisposed) return;
        try {
            target = NativeTarget.FindWindow();
            if (probe.Target != target.ToInt64()) {
                CancelEditorScan(); recoveryPublished = false;
                CoverChatWindow(target);
                probe.SetTarget(target.ToInt64()); gate.SetReady(false); targetResponsive = false; nextScan = DateTime.MinValue; PublishReady();
            }
            if (!gate.Ready && !recoveryPublished && frontend && (DateTime.UtcNow - startupTime).TotalSeconds >= 30) {
                recoveryPublished = true;
                web.CoreWebView2.PostWebMessageAsJson("{\"type\":\"recovery\",\"visible\":true}");
            }
            // No frame capture or heavy UIA work continues after readiness.
            if (target == IntPtr.Zero || gate.Ready || probe.InFlight || DateTime.UtcNow < nextScan) return;
            IntPtr editorTarget = target; int revision = probe.Begin();
            var scanCancellation = CancellationTokenSource.CreateLinkedTokenSource(probeLifetime.Token);
            activeScan = scanCancellation;
            try {
                var clock = Stopwatch.StartNew();
                EditorProbeStatus status = await EditorProbe.CheckAsync(Application.ExecutablePath, editorTarget.ToInt64(), 5000, scanCancellation.Token);
                if (closing || IsDisposed || !probe.Complete(editorTarget.ToInt64(), revision, status == EditorProbeStatus.Ready)) return;
                nextScan = DateTime.UtcNow.AddSeconds(2); gate.SetReady(probe.Ready);
                targetResponsive = targetResponsive || status == EditorProbeStatus.Ready || status == EditorProbeStatus.Waiting;
                Log("editor scan: " + status + " elapsed=" + clock.ElapsedMilliseconds + " ms");
                if (probe.Ready) Log("ready: chat editor"); PublishReady();
            } finally {
                if (activeScan == scanCancellation) activeScan = null;
                scanCancellation.Dispose();
            }
        } catch (Exception error) { Log("readiness check: " + error.GetType().Name); }
    }
    private void PublishReady()
    {
        if (frontend && !demo && !IsDisposed && web.CoreWebView2 != null) {
            int percent = StartupProgress.Percent(appActivated, target != IntPtr.Zero, targetResponsive, gate.Ready);
            web.CoreWebView2.PostWebMessageAsJson(new JavaScriptSerializer().Serialize(new {
                type = "ready", ready = gate.Ready, progress = percent, revision = probe.Revision
            }));
            if (gate.Ready) FocusReadyPrompt();
        }
    }
    [STAThread] private static void Main(string[] args)
    {
        // The worker must use the same selected package as the parent host.
        try { LaunchSettings.Load(AppDomain.CurrentDomain.BaseDirectory); }
        catch (Exception error) {
            if (args.Contains("--probe-editor") || args.Contains("--diagnose-readiness")) { Environment.Exit(3); return; }
            MessageBox.Show("Invalid launcher settings. Run Setup.ps1 again.\n" + error.Message, "ChatGPT Elden Ring Loader"); return;
        }
        if (args.Length == 2 && args[0] == "--probe-editor") {
            long handle;
            if (!Int64.TryParse(args[1], out handle) || handle == 0) { Environment.Exit(3); return; }
            try {
                IntPtr window = new IntPtr(handle);
                Environment.Exit(!NativeTarget.Responsive(window) ? 2 : NativeTarget.HasChatEditor(window) ? 0 : 1);
            } catch { Environment.Exit(3); }
            return;
        }
        if (args.Length == 2 && args[0] == "--diagnose-readiness") {
            IntPtr window = NativeTarget.FindWindow();
            EditorProbeStatus status = EditorProbe.CheckAsync(Application.ExecutablePath, window.ToInt64(), 5000, CancellationToken.None).GetAwaiter().GetResult();
            File.WriteAllText(args[1], new JavaScriptSerializer().Serialize(new { window = window.ToInt64(), status = status.ToString() })); return;
        }
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher.log"), DateTime.Now.ToString("s") + " main pid: " + Process.GetCurrentProcess().Id + Environment.NewLine); } catch { }
        if (args.Length > 1 && args[0] == "--self-test") {
            var policy = new LaunchGate(); bool blocked = !policy.TryEnter(); policy.SetReady(true); bool once = policy.TryEnter() && !policy.TryEnter();
            File.WriteAllText(args[1], "{\"blockedBeforeReady\":" + blocked.ToString().ToLowerInvariant() + ",\"entersOnce\":" + once.ToString().ToLowerInvariant() + "}"); Environment.Exit(blocked && once ? 0 : 1); return;
        }
        NativeTarget.SetCurrentProcessExplicitAppUserModelID("ChatGPT.GraceLauncher");
        bool demonstration = args.Contains("--demo");
        bool forceStartup = args.Contains("--test-startup") || Path.GetFileNameWithoutExtension(Application.ExecutablePath).EndsWith("Test", StringComparison.OrdinalIgnoreCase);
        bool first; using (var mutex = new Mutex(true, "Local\\CodexAtGrace", out first)) {
            if (!first) {
                foreach (string name in new[] { "ChatGPTGraceLauncher", "ChatGPTGraceTest" })
                    foreach (var process in Process.GetProcessesByName(name))
                        using (process) if (process.Id != Process.GetCurrentProcess().Id && process.MainWindowHandle != IntPtr.Zero) NativeTarget.BringForward(process.MainWindowHandle);
                return;
            }
            if (!demonstration && !forceStartup) {
                string processEvidence; bool running = NativeTarget.IsRunning(out processEvidence);
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher.log"), DateTime.Now.ToString("s") + " background check: " + processEvidence + Environment.NewLine); } catch { }
                if (running) {
                    NativeTarget.Launch();
                    try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher.log"), DateTime.Now.ToString("s") + " warm launch: verified main process, activated ChatGPT without WebView" + Environment.NewLine); } catch { }
                    return;
                }
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher.log"), DateTime.Now.ToString("s") + " cold launch: no verified main process" + Environment.NewLine); } catch { }
            }
            Application.Run(new GraceLauncher(demonstration));
        }
    }
}
