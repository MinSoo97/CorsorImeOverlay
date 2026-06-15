using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ImeOverlay
{
    internal class OverlayForm : Form
    {
        private bool     _isHangul = false;
        private bool     _isCaps   = false;
        private Settings _settings;

        private readonly System.Windows.Forms.Timer _trackTimer;
        private readonly System.Windows.Forms.Timer _fadeTimer;

        private const int    OFFSET_X      = 18;
        private const int    OFFSET_Y      = 20;
        private const int    FADE_INTERVAL = 30;
        private const double FADE_STEP     = 0.05;

        private DateTime _showUntil = DateTime.MinValue;
        private double   _opacity   = 0;

        private static readonly Color COLOR_HANGUL_BG = Color.FromArgb(83, 74, 183);
        private static readonly Color COLOR_HANGUL_FG = Color.FromArgb(238, 237, 254);
        private static readonly Color COLOR_ENG_BG    = Color.FromArgb(15, 110, 86);
        private static readonly Color COLOR_ENG_FG    = Color.FromArgb(225, 245, 238);
        private static readonly Color TRANSPARENT_KEY = Color.FromArgb(1, 0, 1);

        private static IntPtr _mouseHook = IntPtr.Zero;
        private static Win32.LowLevelKeyboardProc? _mouseProc;
        private static OverlayForm? _instance;

        public OverlayForm(Settings settings)
        {
            _settings = settings;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar   = false;
            TopMost         = true;
            BackColor       = TRANSPARENT_KEY;
            TransparencyKey = TRANSPARENT_KEY;
            Cursor          = Cursors.Default;
            Opacity         = 0;
            Visible         = true;

            _instance = this;

            _trackTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _trackTimer.Tick += OnTrackTick;
            _trackTimer.Start();

            _fadeTimer = new System.Windows.Forms.Timer { Interval = FADE_INTERVAL };
            _fadeTimer.Tick += OnFadeTick;
            _fadeTimer.Start();

            ApplySettings(settings);
            SetBounds(-200, -200, Width, Height);

            _mouseProc = MouseProc;
            _mouseHook = SetWindowsHookEx(14, _mouseProc, IntPtr.Zero, 0);
        }

        public void ApplySettings(Settings settings)
        {
            _settings = settings;

            // 폰트 크기 + 레이블 길이에 따라 폼 크기 자동 조정
            float fs      = settings.FontSize;
            var   newFont = new Font("맑은 고딕", fs, FontStyle.Bold, GraphicsUnit.Point);
            string[] labels = { settings.HangulLabel, settings.EngLowerLabel, settings.EngUpperLabel };
            int fw = 32, fh = 22;
            using (var g = CreateGraphics())
            {
                foreach (var lbl in labels)
                {
                    if (string.IsNullOrEmpty(lbl)) continue;
                    var sz = g.MeasureString(lbl, newFont);
                    fw = Math.Max(fw, (int)Math.Ceiling(sz.Width) + 12);
                    fh = Math.Max(fh, (int)Math.Ceiling(sz.Height) + 8);
                }
            }
            Font = newFont;
            Size = new Size(fw, fh);

            if (settings.AlwaysShow)
            {
                _opacity   = settings.MaxOpacity;
                Opacity    = _opacity;
                _showUntil = DateTime.MaxValue;
            }

            Invalidate();
        }

        private static IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == 0x201 || msg == 0x204 || msg == 0x207)
                    _instance?.ShowOverlay();
            }
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        public void ShowOverlay()
        {
            if (InvokeRequired) { Invoke((Action)ShowOverlay); return; }

            if (_settings.AlwaysShow)
            {
                _opacity   = _settings.MaxOpacity;
                Opacity    = _opacity;
                _showUntil = DateTime.MaxValue;
            }
            else
            {
                _showUntil = DateTime.Now.AddMilliseconds(_settings.ShowDurationMs);
                _opacity   = _settings.MaxOpacity;
                Opacity    = _opacity;
            }
        }

        private void OnTrackTick(object? sender, EventArgs e)
        {
            if (_opacity > 0)
            {
                var p = Cursor.Position;
                Win32.SetWindowPos(Handle, Win32.HWND_TOPMOST,
                    p.X + OFFSET_X, p.Y + OFFSET_Y,
                    0, 0,
                    Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
            }
        }

        private void OnFadeTick(object? sender, EventArgs e)
        {
            if (_settings.AlwaysShow)
            {
                if (Math.Abs(Opacity - _settings.MaxOpacity) > 0.01)
                {
                    _opacity = _settings.MaxOpacity;
                    Opacity  = _opacity;
                }
                return;
            }

            if (_opacity <= 0) return;

            if (DateTime.Now < _showUntil)
            {
                if (Math.Abs(_opacity - _settings.MaxOpacity) > 0.01)
                {
                    _opacity = _settings.MaxOpacity;
                    Opacity  = _opacity;
                }
                return;
            }

            _opacity -= FADE_STEP;
            if (_opacity <= 0) { _opacity = 0; Opacity = 0; }
            else Opacity = _opacity;
        }

        public void UpdateState(bool isHangul, bool isCaps)
        {
            if (_isHangul == isHangul && _isCaps == isCaps) return;
            _isHangul = isHangul;
            _isCaps   = isCaps;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color  bg;
            Color  fg;
            string label;

            if (_isHangul)
            {
                bg    = _settings.HangulBgColor;
                fg    = _settings.HangulFgColor;
                label = _settings.HangulLabel;
            }
            else
            {
                bg    = _settings.EngBgColor;
                fg    = _settings.EngFgColor;
                label = _isCaps ? _settings.EngUpperLabel : _settings.EngLowerLabel;
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var bgBrush = new SolidBrush(bg);
            using var path    = RoundedRect(rect, 6);
            g.FillPath(bgBrush, path);

            var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var fgBrush = new SolidBrush(fg);
            g.DrawString(label, Font, fgBrush, new RectangleF(0, 0, Width, Height), sf);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000020;
                cp.ExStyle |= 0x00000080;
                cp.ExStyle |= 0x08000000;
                return cp;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trackTimer.Dispose();
                _fadeTimer.Dispose();
                if (_mouseHook != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_mouseHook);
                    _mouseHook = IntPtr.Zero;
                }
            }
            base.Dispose(disposing);
        }

        [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int idHook, Win32.LowLevelKeyboardProc fn, IntPtr hMod, uint threadId);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    }
}
