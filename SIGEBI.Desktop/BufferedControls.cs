using System.Runtime.InteropServices;

namespace SIGEBI.Desktop;

internal sealed class BufferedTabControl : TabControl
{
    public BufferedTabControl()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }
}

internal sealed class BufferedDataGridView : DataGridView
{
    private const int WmSetRedraw = 0x000B;

    public BufferedDataGridView()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    public void BeginUpdate()
    {
        if (IsHandleCreated)
            NativeMethods.SendMessage(Handle, WmSetRedraw, 0, 0);
        SuspendLayout();
    }

    public void EndUpdate()
    {
        ResumeLayout(true);
        if (IsHandleCreated)
            NativeMethods.SendMessage(Handle, WmSetRedraw, 1, 0);
        Invalidate(true);
        Update();
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        internal static extern nint SendMessage(
            nint windowHandle,
            uint message,
            nuint wordParameter,
            nint longParameter);
    }
}

internal sealed class BufferedPanel : Panel
{
    public BufferedPanel()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
    }
}

internal sealed class BufferedTableLayoutPanel : TableLayoutPanel
{
    public BufferedTableLayoutPanel()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
    }
}

internal sealed class BufferedFlowLayoutPanel : FlowLayoutPanel
{
    public BufferedFlowLayoutPanel()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);
    }
}
