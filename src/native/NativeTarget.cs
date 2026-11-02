using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;

internal static class NativeTarget
{
    internal static string AppId { get { return LaunchSettings.Current.AppId; } }
    internal delegate bool EnumCallback(IntPtr hwnd, IntPtr param);
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumCallback callback, IntPtr param);
    [DllImport("user32.dll")] internal static extern bool EnumChildWindows(IntPtr hwnd, EnumCallback callback, IntPtr param);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern bool IsZoomed(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern bool IsHungAppWindow(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);
    [DllImport("user32.dll")] internal static extern bool GetClientRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] internal static extern bool ShowWindowAsync(IntPtr hwnd, int command);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint first, uint second, bool attach);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] internal static extern bool PrintWindow(IntPtr hwnd, IntPtr dc, uint flags);
    [DllImport("user32.dll")] internal static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);
    [DllImport("kernel32.dll")] internal static extern IntPtr OpenProcess(uint access, bool inherit, uint id);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern int GetApplicationUserModelId(IntPtr process, ref uint length, StringBuilder appId);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] internal static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
    [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    [DllImport("user32.dll")] internal static extern IntPtr GetThreadDpiAwarenessContext();
    [DllImport("user32.dll")] internal static extern bool AreDpiAwarenessContextsEqual(IntPtr first, IntPtr second);
    [StructLayout(LayoutKind.Sequential)] internal struct MinMaxInfo {
        public int ReservedX, ReservedY, MaxSizeX, MaxSizeY, MaxPositionX, MaxPositionY, MinTrackX, MinTrackY, MaxTrackX, MaxTrackY;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct DisplayMode {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        public ushort SpecVersion, DriverVersion, Size, DriverExtra;
        public uint Fields;
        public int PositionX, PositionY;
        public uint DisplayOrientation, DisplayFixedOutput;
        public short Color, Duplex, YResolution, TTOption, Collate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string FormName;
        public ushort LogPixels;
        public uint BitsPerPel, PelsWidth, PelsHeight, DisplayFlags, DisplayFrequency, ICMMethod, ICMIntent, MediaType, DitherType, Reserved1, Reserved2, PanningWidth, PanningHeight;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool EnumDisplaySettings(string device, int mode, ref DisplayMode settings);
    internal static bool TryGetPhysicalDisplay(string device, out Rect bounds)
    {
        var settings = new DisplayMode(); settings.Size = (ushort)Marshal.SizeOf(typeof(DisplayMode));
        bounds = new Rect();
        if (!EnumDisplaySettings(device, -1, ref settings) || settings.PelsWidth == 0 || settings.PelsHeight == 0) return false;
        bounds.Left = settings.PositionX; bounds.Top = settings.PositionY;
        bounds.Right = bounds.Left + (int)settings.PelsWidth; bounds.Bottom = bounds.Top + (int)settings.PelsHeight;
        return true;
    }

    [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivationManager
    {
        int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string id, [MarshalAs(UnmanagedType.LPWStr)] string args, uint options, out uint pid);
        int ActivateForFile(IntPtr items, string verb, out uint pid);
        int ActivateForProtocol(IntPtr items, out uint pid);
    }
    internal static uint Launch()
    {
        object manager = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")));
        try { uint pid; int hr = ((IActivationManager)manager).ActivateApplication(AppId, null, 0, out pid); Marshal.ThrowExceptionForHR(hr); return pid; }
        finally { Marshal.ReleaseComObject(manager); }
    }
    internal static string ExportTargetIcon(string folder)
    {
        IntPtr window = FindWindow(); if (window == IntPtr.Zero) return null;
        uint pid; GetWindowThreadProcessId(window, out pid);
        using (var process = Process.GetProcessById((int)pid))
        using (var icon = Icon.ExtractAssociatedIcon(process.MainModule.FileName)) {
            if (icon == null) return null;
            string path = System.IO.Path.Combine(folder, "ChatGPT.ico");
            using (var file = System.IO.File.Create(path)) icon.Save(file);
            return path;
        }
    }
    internal static bool IsRunning(out string evidence)
    {
        // Diagnostic only: record which process caused the decision. Do not log
        // full command lines, which can contain unrelated user data.
        var roles = new Dictionary<int, string>();
        string queryStatus = "ok";
        try {
            using (var query = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='" + LaunchSettings.Current.ProcessName + ".exe'")) {
                query.Options.Timeout = TimeSpan.FromSeconds(2);
                using (var results = query.Get()) foreach (ManagementObject row in results) using (row) {
                    string command = Convert.ToString(row["CommandLine"]);
                    var role = Regex.Match(command, @"(?:^|\s)--type(?:=|\s+)([a-zA-Z0-9_-]+)");
                    roles[Convert.ToInt32(row["ProcessId"])] = WarmLaunchPolicy.IsMainCommandLine(command) ? "main" : String.IsNullOrEmpty(command) ? "unknown" : role.Success ? role.Groups[1].Value : "helper";
                }
            }
        } catch (Exception error) { queryStatus = error.GetType().Name; }
        var detail = new StringBuilder("role-query=" + queryStatus); bool running = false;
        foreach (var candidate in Process.GetProcessesByName(LaunchSettings.Current.ProcessName)) {
            try {
                IntPtr handle = OpenProcess(0x1000, false, (uint)candidate.Id); if (handle == IntPtr.Zero) continue;
                try {
                    uint length = 512; var id = new StringBuilder((int)length);
                    if (GetApplicationUserModelId(handle, ref length, id) == 0 && id.ToString() == AppId) {
                        string role; if (!roles.TryGetValue(candidate.Id, out role)) role = "unknown";
                        // Same executable and package identity are shared by
                        // Electron's renderer, GPU, utility and crash handlers.
                        // Only the application main process suppresses the scene.
                        if (role == "main") running = true;
                        detail.AppendFormat("; pid={0} role={1} main-window={2} started={3:s}", candidate.Id, role, candidate.MainWindowHandle, candidate.StartTime);
                    }
                }
                finally { CloseHandle(handle); }
            } catch { }
            finally { candidate.Dispose(); }
        }
        evidence = detail.ToString(); return running;
    }
    internal static IntPtr FindWindow()
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr hwnd, IntPtr ignored) {
            // ChatGPT also owns a hidden window with the same caption and identity.
            // Only its visible main window can become the readiness target.
            if (!IsWindowVisible(hwnd)) return true;
            uint pid; GetWindowThreadProcessId(hwnd, out pid);
            if (pid == (uint)Process.GetCurrentProcess().Id) return true;
            var title = new StringBuilder(512); GetWindowText(hwnd, title, title.Capacity);
            if (title.Length == 0 || (!String.IsNullOrEmpty(LaunchSettings.Current.WindowTitle) && title.ToString().IndexOf(LaunchSettings.Current.WindowTitle, StringComparison.OrdinalIgnoreCase) < 0)) return true;
            Rect client; if (!GetClientRect(hwnd, out client) || client.Right < 350 || client.Bottom < 200) return true;
            IntPtr process = OpenProcess(0x1000, false, pid); if (process == IntPtr.Zero) return true;
            try {
                uint length = 512; var id = new StringBuilder((int)length);
                if (GetApplicationUserModelId(process, ref length, id) == 0 && id.ToString() == AppId) { found = hwnd; return false; }
            } finally { CloseHandle(process); }
            return true;
        }, IntPtr.Zero);
        return found;
    }
    internal static bool Responsive(IntPtr hwnd)
    {
        if (!IsWindow(hwnd) || IsHungAppWindow(hwnd)) return false;
        Rect rect; GetClientRect(hwnd, out rect);
        if (rect.Right - rect.Left < 350 || rect.Bottom - rect.Top < 200) return false;
        IntPtr result;
        return SendMessageTimeout(hwnd, 0, IntPtr.Zero, IntPtr.Zero, 2, 200, out result) != IntPtr.Zero;
    }
    internal static bool HasChatEditor(IntPtr hwnd)
    {
        try {
            var root = AutomationElement.FromHandle(hwnd); var bounds = root.Current.BoundingRectangle;
            var editors = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
            foreach (AutomationElement editor in editors) {
                var current = editor.Current; var r = current.BoundingRectangle;
                if (current.IsEnabled && current.IsKeyboardFocusable && !current.IsOffscreen && r.Width > 200 && r.Height >= 18 && r.Top > bounds.Top + bounds.Height * .42) return true;
            }
        } catch { }
        return false;
    }
    internal static bool HasPaintedContent(IntPtr hwnd)
    {
        if (!Responsive(hwnd) || !IsWindowVisible(hwnd) || IsIconic(hwnd)) return false;
        Rect r; GetWindowRect(hwnd, out r); int width = r.Right - r.Left, height = r.Bottom - r.Top;
        if (width <= 0 || height <= 0 || width > 12000 || height > 8000) return false;
        using (var bitmap = new Bitmap(width, height)) using (var graphics = Graphics.FromImage(bitmap)) {
            IntPtr dc = graphics.GetHdc(); bool painted;
            try { painted = PrintWindow(hwnd, dc, 2); } finally { graphics.ReleaseHdc(dc); }
            if (!painted) return false;
            var colors = new HashSet<int>(); int nonblack = 0;
            for (int y = height / 5; y < height * 9 / 10; y += Math.Max(3, height / 55))
                for (int x = width / 12; x < width * 11 / 12; x += Math.Max(3, width / 90)) {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.R + pixel.G + pixel.B > 36) nonblack++;
                    colors.Add((pixel.R / 20 << 8) | (pixel.G / 20 << 4) | (pixel.B / 20));
                }
            return nonblack > 100 && colors.Count >= 8;
        }
    }
    internal static bool BringForward(IntPtr hwnd)
    {
        if (!IsWindow(hwnd)) return false;
        ShowWindow(hwnd, IsIconic(hwnd) ? 9 : 5);
        return SetForegroundWindow(hwnd) && GetForegroundWindow() == hwnd;
    }
    internal static async Task<bool> ActivateForHandoffAsync(IntPtr hwnd, IntPtr startup, Func<bool> valid, Action<string> log)
    {
        var clock = Stopwatch.StartNew(); int attempts = 0;
        bool activated = await ForegroundHandoff.ActivateAsync(startup.ToInt64(), hwnd.ToInt64(),
            delegate { return IsWindow(hwnd) && valid(); },
            delegate { return GetForegroundWindow().ToInt64(); },
            delegate {
                IntPtr before = GetForegroundWindow();
                bool accepted = SetForegroundWindow(hwnd); attempts++;
                log("handoff activation request: attempt=" + attempts + " accepted=" + accepted + " foreground=" + before.ToInt64() + " -> " + GetForegroundWindow().ToInt64());
                return accepted;
            },
            delegate { return clock.Elapsed.TotalMilliseconds; },
            delegate(int ms) { return Task.Delay(ms); });
        log("handoff activation verified: " + activated + " attempts=" + attempts + " elapsed=" + clock.ElapsedMilliseconds + " ms foreground=" + GetForegroundWindow().ToInt64());
        return activated;
    }
    internal static bool FocusStartupPrompt(IntPtr startup, IntPtr chat)
    {
        if (GetForegroundWindow() == startup || BringForward(startup)) return true;
        // Repair only the expected chat-to-loader focus transfer; never attach
        // to another application the user deliberately selected.
        if (chat == IntPtr.Zero || GetForegroundWindow() != chat) return false;
        uint ignored; uint foregroundThread = GetWindowThreadProcessId(chat, out ignored), currentThread = GetCurrentThreadId();
        if (foregroundThread == 0 || foregroundThread == currentThread) return false;
        bool attached = AttachThreadInput(currentThread, foregroundThread, true);
        try { return attached && BringForward(startup); }
        finally { if (attached) AttachThreadInput(currentThread, foregroundThread, false); }
    }
    internal static async Task<bool> MaximizeForHandoffAsync(IntPtr hwnd)
    {
        bool prepared = await HandoffPreparation.PrepareAsync(
            delegate { return IsWindow(hwnd); },
            delegate { return IsZoomed(hwnd) && !IsIconic(hwnd); },
            delegate { return ShowWindowAsync(hwnd, 3); }); // SW_MAXIMIZE
        if (!prepared) return false;
        // Drain a bounded message round trip before revealing the resized app.
        bool responsive = await Task.Run(delegate { return Responsive(hwnd); });
        return responsive && IsWindow(hwnd) && IsZoomed(hwnd) && !IsIconic(hwnd);
    }
}
