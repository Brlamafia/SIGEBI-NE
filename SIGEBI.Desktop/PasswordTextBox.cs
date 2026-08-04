using System.Runtime.InteropServices;

namespace SIGEBI.Desktop;

internal sealed class PasswordTextBox : TextBox
{
    private const int EmHideBalloonTip = 0x1504;
    private const int EmSetPasswordChar = 0x00CC;
    private const int WmSetFocus = 0x0007;
    private const int WmKeyDown = 0x0100;
    private const int WmChar = 0x0102;
    private const int WmLeftButtonDown = 0x0201;

    public PasswordTextBox()
    {
        UseSystemPasswordChar = true;
    }

    protected override void OnHandleCreated(EventArgs eventArgs)
    {
        base.OnHandleCreated(eventArgs);
        BeginInvoke(HideSystemBalloon);
    }

    protected override void WndProc(ref Message message)
    {
        var shouldHideBalloon = message.Msg is WmSetFocus
            or WmKeyDown
            or WmChar
            or WmLeftButtonDown
            or EmSetPasswordChar;

        base.WndProc(ref message);

        if (shouldHideBalloon)
            HideSystemBalloon();
    }

    private void HideSystemBalloon()
    {
        if (IsHandleCreated && !IsDisposed)
            NativeEdit.SendMessage(Handle, EmHideBalloonTip, 0, 0);
    }

    private static class NativeEdit
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        internal static extern nint SendMessage(
            nint windowHandle,
            uint message,
            nuint wordParameter,
            nint longParameter);
    }
}
