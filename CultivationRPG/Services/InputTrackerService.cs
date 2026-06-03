using System.Runtime.InteropServices;
using System.Threading.Channels;
using CultivationRPG.Helpers;

namespace CultivationRPG.Services;

public sealed class InputTrackerService : IDisposable
{
    private readonly ChannelWriter<InputEvent> _writer;
    private Thread? _kbThread, _msThread;
    private IntPtr _kbHook, _msHook;
    private NativeMethods.LowLevelHookProc? _kbProc, _msProc;
    private int _moveCount;
    private uint _kbThreadId, _msThreadId;
    private volatile bool _stopped;

    public InputTrackerService(ChannelWriter<InputEvent> writer) => _writer = writer;

    public void Start()
    {
        _kbThread = new Thread(KbPump) { Name = "kb-hook", IsBackground = true };
        _msThread = new Thread(MsPump) { Name = "ms-hook", IsBackground = true };
        _kbThread.Start();
        _msThread.Start();
    }

    private void KbPump()
    {
        _kbProc = KbCallback; _kbThreadId = NativeMethods.GetCurrentThreadId();
        var hmod = NativeMethods.GetModuleHandle(null);
        _kbHook = NativeMethods.SetWindowsHookExW(13, _kbProc, hmod, 0);
        while (!_stopped && NativeMethods.GetMessageW(out var m, IntPtr.Zero, 0, 0) > 0)
        { NativeMethods.TranslateMessage(ref m); NativeMethods.DispatchMessageW(ref m); }
        if (_kbHook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_kbHook);
    }

    private IntPtr KbCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (uint)wParam is 0x0100 or 0x0104)
        {
            var info = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            if ((info.flags & 0x10) == 0) _writer.TryWrite(new InputEvent { Type = EventType.Key });
        }
        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private void MsPump()
    {
        _msProc = MsCallback; _msThreadId = NativeMethods.GetCurrentThreadId();
        var hmod = NativeMethods.GetModuleHandle(null);
        _msHook = NativeMethods.SetWindowsHookExW(14, _msProc, hmod, 0);
        while (!_stopped && NativeMethods.GetMessageW(out var m, IntPtr.Zero, 0, 0) > 0)
        { NativeMethods.TranslateMessage(ref m); NativeMethods.DispatchMessageW(ref m); }
        if (_msHook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_msHook);
    }

    private IntPtr MsCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var info = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            uint msg = (uint)wParam;
            if (msg is 0x0201 or 0x0204 or 0x0207)
                _writer.TryWrite(new InputEvent { Type = EventType.Click, X = info.pt.x, Y = info.pt.y });
            else if (msg == 0x0200 && ++_moveCount % 10 == 0)
                _writer.TryWrite(new InputEvent { Type = EventType.Move, X = info.pt.x, Y = info.pt.y });
        }
        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    public void Stop()
    {
        _stopped = true;
        if (_kbThreadId != 0) NativeMethods.PostThreadMessageW(_kbThreadId, 0x0012, UIntPtr.Zero, IntPtr.Zero);
        if (_msThreadId != 0) NativeMethods.PostThreadMessageW(_msThreadId, 0x0012, UIntPtr.Zero, IntPtr.Zero);
    }

    public void Dispose() { Stop(); }
}

public enum EventType { Key, Click, Move }
public record InputEvent { public EventType Type; public int X; public int Y; }
