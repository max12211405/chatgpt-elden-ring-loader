using System;
using System.Drawing;

internal static class WindowEdgeLayout
{
    // A standard framed window's Bounds include invisible resize borders.
    // Move those borders off-screen while keeping the native caption at the top.
    internal static Rectangle Expand(Rectangle monitor, int left, int right, int bottom)
    {
        left = Math.Max(0, left); right = Math.Max(0, right); bottom = Math.Max(0, bottom);
        return Rectangle.FromLTRB(monitor.Left - left, monitor.Top, monitor.Right + right, monitor.Bottom + bottom);
    }
}
