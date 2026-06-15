using System;
using System.Runtime.InteropServices;
using System.Threading;
using Timer = System.Threading.Timer;

namespace ImeOverlay
{
    internal class ImeMonitor : IDisposable
    {
        // isHangul, isCapsLock
        public event Action<bool, bool>? ImeStateChanged;
        // 한/영 또는 CapsLock 키 눌렸을 때 오버레이 표시 트리거
        public event Action? KeyTriggered;

        private Timer? _pollTimer;
        private bool   _lastHangul = false;
        private bool   _lastCaps   = false;
        private bool   _disposed;

        // 키보드 훅
        private IntPtr _hookHandle = IntPtr.Zero;
        private Win32.LowLevelKeyboardProc? _hookProc;

        public void Start()
        {
            // 폴링: IME 상태 변경 감지
            _pollTimer = new Timer(_ => Poll(), null, 500, 200);

            // 키보드 훅: 한/영, CapsLock 키 눌렸을 때 트리거
            _hookProc   = KeyboardProc;
            _hookHandle = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _hookProc, IntPtr.Zero, 0);
        }

        public void Stop()
        {
            _pollTimer?.Dispose();
            _pollTimer = null;

            if (_hookHandle != IntPtr.Zero)
            {
                Win32.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }

        private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == Win32.WM_KEYDOWN || msg == Win32.WM_SYSKEYDOWN)
                {
                    int vk = Marshal.ReadInt32(lParam);
                    // VK_HANGUL(0x15), VK_KANJI(0x19), VK_CAPITAL(0x14=CapsLock)
                    if (vk == Win32.VK_HANGUL || vk == Win32.VK_KANJI || vk == 0x14)
                    {
                        KeyTriggered?.Invoke();
                        // 상태 변경 후 읽기 위해 약간 지연
                        new Timer(_ => Poll(), null, 50, Timeout.Infinite);
                    }
                }
            }
            return Win32.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        private void Poll()
        {
            try
            {
                bool isHangul = GetImeState();
                bool isCaps   = (GetKeyState(0x14) & 0x0001) != 0;

                if (isHangul != _lastHangul || isCaps != _lastCaps)
                {
                    _lastHangul = isHangul;
                    _lastCaps   = isCaps;
                    ImeStateChanged?.Invoke(isHangul, isCaps);
                }
            }
            catch { }
        }

        private static bool GetImeState()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            IntPtr imeWnd = ImmGetDefaultIMEWnd(hwnd);
            if (imeWnd == IntPtr.Zero) return false;
            IntPtr result = SendMessage(imeWnd, 0x0283, new IntPtr(0x0001), IntPtr.Zero);
            return (result.ToInt32() & 0x1) != 0;
        }

        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern short GetKeyState(int nVirtKey);
        [DllImport("imm32.dll")]  static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}
