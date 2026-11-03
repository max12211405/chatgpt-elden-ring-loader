using System;
using System.Drawing;

internal static class WindowEdgeLayoutTests
{
    private static void Main()
    {
        int cases = 0;
        foreach (Rectangle monitor in new[] {new Rectangle(0, 0, 1920, 1080), new Rectangle(-2560, 0, 2560, 1440), new Rectangle(1920, -1440, 2560, 1440)})
        foreach (int border in new[] {0, 8, 9, 12, 16}) {
            Rectangle window = WindowEdgeLayout.Expand(monitor, border, border, border);
            Rectangle image = Rectangle.FromLTRB(window.Left + border, window.Top + 38, window.Right - border, window.Bottom - border);
            if (image.Left != monitor.Left || image.Right != monitor.Right || image.Bottom != monitor.Bottom || window.Top != monitor.Top)
                throw new Exception("Rendered content does not meet the physical monitor edges");
            cases++;
        }
        // The actual 125% DPI measurement reported by the existing launcher.
        Rectangle actual = WindowEdgeLayout.Expand(new Rectangle(0, 0, 1920, 1080), 9, 9, 9);
        if (actual != new Rectangle(-9, 0, 1938, 1089)) throw new Exception("Observed 9px resize border was not compensated");
        Console.WriteLine("PASS: " + (cases + 1) + " physical-pixel edge cases; native caption retained");
    }
}
