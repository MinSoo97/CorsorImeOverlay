using System;
using System.Runtime.InteropServices;

namespace ImeOverlay
{
    internal static class Win32
    {
        // ── 키보드 훅 ──────────────────────────────────────────
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN     = 0x0100;
        public const int WM_KEYUP       = 0x0101;
        public const int WM_SYSKEYDOWN  = 0x0104;

        public const int VK_HANGUL      = 0x15;
        public const int VK_KANJI       = 0x19;   // 일부 키보드 한/영 키

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc fn,
                                                      IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
                                                    IntPtr wParam, IntPtr lParam);

        // ── IME ───────────────────────────────────────────────
        public const int IME_CMODE_NATIVE   = 0x0001;   // 한글 모드
        public const int IME_CMODE_FULLSHAPE = 0x0008;  // 전각

        [DllImport("imm32.dll")]
        public static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll")]
        public static extern bool ImmGetConversionStatus(IntPtr hImc,
                                                          out int lpfdwConversion,
                                                          out int lpfdwSentence);

        [DllImport("imm32.dll")]
        public static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hImc);

        // ── 창 관리 ───────────────────────────────────────────
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                                                int X, int Y, int cx, int cy, uint uFlags);

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE     = 0x0001;
        public const uint SWP_NOACTIVATE = 0x0010;

        // ── 모듈 핸들 ─────────────────────────────────────────
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);
    }
}
