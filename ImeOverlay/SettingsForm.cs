using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImeOverlay
{
    internal class SettingsForm : Form
    {
        private readonly Settings _settings;
        private bool _syncing = false;

        // 1페이지
        private RadioButton   _rbAlways   = null!;
        private RadioButton   _rbOnClick  = null!;
        private Label         _lblSec     = null!;
        private NumericUpDown _nudSec     = null!;
        private Label         _lblSecUnit = null!;
        private CheckBox      _chkStartup = null!;

        // 2페이지
        private TrackBar      _tbOpacity  = null!;
        private NumericUpDown _nudOpacity = null!;
        private TrackBar      _tbFont     = null!;
        private NumericUpDown _nudFont    = null!;

        private Button  _btnHangulBg    = null!;
        private Button  _btnHangulFg    = null!;
        private TextBox _txtHangulLabel = null!;
        private Button  _btnEngBg       = null!;
        private Button  _btnEngFg       = null!;
        private TextBox _txtEngLower    = null!;
        private TextBox _txtEngUpper    = null!;

        private Panel _previewPanel = null!;

        private Color _hangulBg, _hangulFg, _engBg, _engFg;

        public SettingsForm(Settings settings)
        {
            _settings = settings;
            _hangulBg = settings.HangulBgColor;
            _hangulFg = settings.HangulFgColor;
            _engBg    = settings.EngBgColor;
            _engFg    = settings.EngFgColor;
            BuildUI();
            LoadValues();
        }

        private void BuildUI()
        {
            Text            = "ImeOverlay 환경설정";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;
            Font            = new Font("맑은 고딕", 10f);

            var btnOk = new Button
            {
                Text         = "확인",
                Size         = new Size(90, 34),
                Font         = new Font("맑은 고딕", 10f),
                DialogResult = DialogResult.OK,
                Anchor       = AnchorStyles.Bottom | AnchorStyles.Right
            };
            var btnCancel = new Button
            {
                Text         = "취소",
                Size         = new Size(90, 34),
                Font         = new Font("맑은 고딕", 10f),
                DialogResult = DialogResult.Cancel,
                Anchor       = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnOk.Click += OnOkClick;

            var tab = new TabControl
            {
                Location = new Point(10, 10),
                Font     = new Font("맑은 고딕", 10f),
                Anchor   = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            tab.TabPages.Add(BuildPage1());
            tab.TabPages.Add(BuildPage2());

            Controls.Add(tab);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // ClientSize 기준으로 위치 계산 (DPI 무관)
            ClientSize         = new Size(630, 590);
            tab.Size           = new Size(ClientSize.Width - 20, ClientSize.Height - 58);
            btnOk.Location     = new Point(ClientSize.Width - 200, ClientSize.Height - 44);
            btnCancel.Location = new Point(ClientSize.Width - 102, ClientSize.Height - 44);
        }

                // ── 1페이지 ───────────────────────────────────────────
        private TabPage BuildPage1()
        {
            var page = new TabPage("표시 설정") { Font = new Font("맑은 고딕", 10f) };
            int lx = 16, ly = 16, lw = 570;

            var grpMode = new GroupBox
            {
                Text = "표시 모드", Location = new Point(lx, ly),
                Size = new Size(lw, 140), Font = new Font("맑은 고딕", 10f)
            };
            _rbAlways = new RadioButton
            {
                Text = "항상 표시", Location = new Point(16, 30), Size = new Size(300, 26)
            };
            _rbOnClick = new RadioButton
            {
                Text = "클릭 / 한영 / CapsLock 시 표시 후 사라짐",
                Location = new Point(16, 62), Size = new Size(420, 26)
            };
            _rbOnClick.CheckedChanged += (s, e) => UpdateSecVisible();

            _lblSec = new Label
            {
                Text = "표시 시간:", Location = new Point(36, 98),
                Size = new Size(80, 28), TextAlign = ContentAlignment.MiddleLeft
            };
            _nudSec = new NumericUpDown
            {
                Minimum = 1, Maximum = 30, DecimalPlaces = 0,
                Location = new Point(120, 96), Size = new Size(72, 28)
            };
            _lblSecUnit = new Label
            {
                Text = "초", Location = new Point(198, 98),
                Size = new Size(30, 28), TextAlign = ContentAlignment.MiddleLeft
            };
            grpMode.Controls.AddRange(new Control[]
                { _rbAlways, _rbOnClick, _lblSec, _nudSec, _lblSecUnit });

            ly += 158;
            _chkStartup = new CheckBox
            {
                Text = "Windows 시작 시 자동 실행",
                Location = new Point(lx, ly), Size = new Size(lw, 30),
                Font = new Font("맑은 고딕", 10f)
            };

            page.Controls.Add(grpMode);
            page.Controls.Add(_chkStartup);
            return page;
        }

        // ── 2페이지 ───────────────────────────────────────────
        private TabPage BuildPage2()
        {
            var page = new TabPage("스타일") { Font = new Font("맑은 고딕", 10f) };

            int lx = 16, ly = 14, settingsW = 360;

            // 투명도
            var grpOpa = new GroupBox
            {
                Text = "최대 투명도", Location = new Point(lx, ly), Size = new Size(settingsW, 64)
            };
            _tbOpacity = new TrackBar
            {
                Minimum = 10, Maximum = 100, TickFrequency = 10, LargeChange = 10,
                Location = new Point(6, 18), Size = new Size(210, 36)
            };
            _tbOpacity.Scroll += (s, e) =>
            {
                if (_syncing) return; _syncing = true;
                _nudOpacity.Value = _tbOpacity.Value;
                _syncing = false; UpdatePreview();
            };
            _nudOpacity = new NumericUpDown
            {
                Minimum = 10, Maximum = 100, DecimalPlaces = 0,
                Location = new Point(220, 24), Size = new Size(72, 28)
            };
            _nudOpacity.ValueChanged += (s, e) =>
            {
                if (_syncing) return; _syncing = true;
                _tbOpacity.Value = (int)_nudOpacity.Value;
                _syncing = false; UpdatePreview();
            };
            grpOpa.Controls.Add(_tbOpacity);
            grpOpa.Controls.Add(_nudOpacity);
            grpOpa.Controls.Add(MakeLabel("%", 296, 26, 28));

            // 글자 크기
            ly += 76;
            var grpFont = new GroupBox
            {
                Text = "글자 크기", Location = new Point(lx, ly), Size = new Size(settingsW, 64)
            };
            _tbFont = new TrackBar
            {
                Minimum = 60, Maximum = 300, TickFrequency = 20, LargeChange = 10, SmallChange = 5,
                Location = new Point(6, 18), Size = new Size(210, 36)
            };
            _tbFont.Scroll += (s, e) =>
            {
                if (_syncing) return; _syncing = true;
                _nudFont.Value = _tbFont.Value / 10m;
                _syncing = false; UpdatePreview();
            };
            _nudFont = new NumericUpDown
            {
                Minimum = 6, Maximum = 30, DecimalPlaces = 1, Increment = 0.5m,
                Location = new Point(220, 24), Size = new Size(80, 28)
            };
            _nudFont.ValueChanged += (s, e) =>
            {
                if (_syncing) return; _syncing = true;
                _tbFont.Value = (int)Math.Round((double)_nudFont.Value * 10);
                _syncing = false; UpdatePreview();
            };
            grpFont.Controls.Add(_tbFont);
            grpFont.Controls.Add(_nudFont);
            grpFont.Controls.Add(MakeLabel("pt", 304, 26, 28));

            // 한글 모드
            ly += 76;
            var grpHangul = new GroupBox
            {
                Text = "한글 모드", Location = new Point(lx, ly), Size = new Size(settingsW, 100)
            };
            // 배경색
            grpHangul.Controls.Add(MakeLabel("배경색:", 12, 30, 60));
            _btnHangulBg = MakeColorButton(76, 26, _hangulBg,
                c => { _hangulBg = c; _btnHangulBg.BackColor = c; UpdatePreview(); });
            grpHangul.Controls.Add(_btnHangulBg);
            // 글자색
            grpHangul.Controls.Add(MakeLabel("글자색:", 190, 30, 60));
            _btnHangulFg = MakeColorButton(254, 26, _hangulFg,
                c => { _hangulFg = c; _btnHangulFg.BackColor = c; UpdatePreview(); });
            grpHangul.Controls.Add(_btnHangulFg);
            // 표시 글자
            grpHangul.Controls.Add(MakeLabel("표시 글자:", 12, 66, 76));
            _txtHangulLabel = new TextBox
            {
                MaxLength = 5, Location = new Point(92, 62), Size = new Size(150, 28),
                TextAlign = HorizontalAlignment.Center, Font = new Font("맑은 고딕", 10f)
            };
            _txtHangulLabel.TextChanged += (s, e) => UpdatePreview();
            grpHangul.Controls.Add(_txtHangulLabel);

            // 영문 모드
            ly += 114;
            var grpEng = new GroupBox
            {
                Text = "영문 모드", Location = new Point(lx, ly), Size = new Size(settingsW, 100)
            };
            grpEng.Controls.Add(MakeLabel("배경색:", 12, 30, 60));
            _btnEngBg = MakeColorButton(76, 26, _engBg,
                c => { _engBg = c; _btnEngBg.BackColor = c; UpdatePreview(); });
            grpEng.Controls.Add(_btnEngBg);
            grpEng.Controls.Add(MakeLabel("글자색:", 190, 30, 60));
            _btnEngFg = MakeColorButton(254, 26, _engFg,
                c => { _engFg = c; _btnEngFg.BackColor = c; UpdatePreview(); });
            grpEng.Controls.Add(_btnEngFg);
            grpEng.Controls.Add(MakeLabel("소문자:", 12, 66, 60));
            _txtEngLower = new TextBox
            {
                MaxLength = 5, Location = new Point(76, 62), Size = new Size(100, 28),
                TextAlign = HorizontalAlignment.Center, Font = new Font("맑은 고딕", 10f)
            };
            _txtEngLower.TextChanged += (s, e) => UpdatePreview();
            grpEng.Controls.Add(_txtEngLower);
            grpEng.Controls.Add(MakeLabel("대문자:", 196, 66, 60));
            _txtEngUpper = new TextBox
            {
                MaxLength = 5, Location = new Point(260, 62), Size = new Size(80, 28),
                TextAlign = HorizontalAlignment.Center, Font = new Font("맑은 고딕", 10f)
            };
            _txtEngUpper.TextChanged += (s, e) => UpdatePreview();
            grpEng.Controls.Add(_txtEngUpper);

            // 미리보기 (우측)
            var grpPreview = new GroupBox
            {
                Text = "미리보기", Location = new Point(392, 14), Size = new Size(200, 460),
                Font = new Font("맑은 고딕", 10f)
            };
            _previewPanel = new Panel
            {
                Location  = new Point(8, 24),
                Size      = new Size(182, 428),
                BackColor = Color.FromArgb(50, 50, 50)
            };
            _previewPanel.Paint += OnPreviewPaint;
            grpPreview.Controls.Add(_previewPanel);

            page.Controls.Add(grpOpa);
            page.Controls.Add(grpFont);
            page.Controls.Add(grpHangul);
            page.Controls.Add(grpEng);
            page.Controls.Add(grpPreview);
            return page;
        }

        // ── 미리보기 렌더링 ───────────────────────────────────
        private void OnPreviewPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.FromArgb(50, 50, 50));

            float fs = (float)(_nudFont?.Value ?? 11.5m);

            // 각 레이블별 배지 크기 계산
            string hangulLbl = string.IsNullOrEmpty(_txtHangulLabel?.Text) ? "한" : _txtHangulLabel.Text;
            string lowerLbl  = string.IsNullOrEmpty(_txtEngLower?.Text)    ? "a"  : _txtEngLower.Text;
            string upperLbl  = string.IsNullOrEmpty(_txtEngUpper?.Text)    ? "A"  : _txtEngUpper.Text;

            using var badgeFont = new Font("맑은 고딕", fs, FontStyle.Bold, GraphicsUnit.Point);

            int cx = 18, cy = 20;

            // 한글
            var (hw, hh) = MeasureBadge(g, badgeFont, hangulLbl);
            DrawCursor(g, cx, cy);
            DrawBadge(g, cx + 14, cy + 14, hw, hh, badgeFont, _hangulBg, _hangulFg, hangulLbl);
            DrawRowLabel(g, cx + 14 + hw + 6, cy + 14 + (hh - 14) / 2, "한글");

            int sep1 = cy + 14 + hh + 14;
            DrawSep(g, sep1);

            // 소문자
            var (lw2, lh) = MeasureBadge(g, badgeFont, lowerLbl);
            int cy2 = sep1 + 12;
            DrawCursor(g, cx, cy2);
            DrawBadge(g, cx + 14, cy2 + 14, lw2, lh, badgeFont, _engBg, _engFg, lowerLbl);
            DrawRowLabel(g, cx + 14 + lw2 + 6, cy2 + 14 + (lh - 14) / 2, "소문자");

            int sep2 = cy2 + 14 + lh + 14;
            DrawSep(g, sep2);

            // 대문자
            var (uw, uh) = MeasureBadge(g, badgeFont, upperLbl);
            int cy3 = sep2 + 12;
            DrawCursor(g, cx, cy3);
            DrawBadge(g, cx + 14, cy3 + 14, uw, uh, badgeFont, _engBg, _engFg, upperLbl);
            DrawRowLabel(g, cx + 14 + uw + 6, cy3 + 14 + (uh - 14) / 2, "대문자");
        }

        private static (int w, int h) MeasureBadge(Graphics g, Font font, string text)
        {
            var sz = g.MeasureString(text, font);
            int w  = Math.Max(32, (int)Math.Ceiling(sz.Width) + 14);
            int h  = Math.Max(22, (int)Math.Ceiling(sz.Height) + 8);
            return (w, h);
        }

        private static void DrawSep(Graphics g, int y)
        {
            using var pen = new Pen(Color.FromArgb(90, 90, 90));
            g.DrawLine(pen, 8, y, 172, y);
        }

        private static void DrawRowLabel(Graphics g, int x, int y, string text)
        {
            using var font  = new Font("맑은 고딕", 8.5f);
            using var brush = new SolidBrush(Color.FromArgb(190, 190, 190));
            g.DrawString(text, font, brush, x, y);
        }

        private static void DrawCursor(Graphics g, int x, int y)
        {
            var pts = new Point[]
            {
                new(x, y), new(x, y+14), new(x+4, y+10),
                new(x+7, y+16), new(x+9, y+15), new(x+6, y+9), new(x+11, y+9)
            };
            using var brush = new SolidBrush(Color.White);
            using var pen   = new Pen(Color.Black, 1f);
            g.FillPolygon(brush, pts);
            g.DrawPolygon(pen, pts);
        }

        private static void DrawBadge(Graphics g, int x, int y, int w, int h,
            Font font, Color bg, Color fg, string label)
        {
            using var bgBrush = new SolidBrush(bg);
            using var path    = RoundedRect(new Rectangle(x, y, w, h), 5);
            g.FillPath(bgBrush, path);

            var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags   = StringFormatFlags.NoClip
            };
            using var fgBrush = new SolidBrush(fg);
            g.DrawString(label, font, fgBrush, new RectangleF(x, y, w, h), sf);
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

        private void UpdatePreview() => _previewPanel?.Invalidate();

        private static Label MakeLabel(string text, int x, int y, int w = 80) =>
            new Label
            {
                Text      = text, Location  = new Point(x, y),
                Size      = new Size(w, 28), TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("맑은 고딕", 10f)
            };

        private Button MakeColorButton(int x, int y, Color color, Action<Color> onChanged)
        {
            var btn = new Button
            {
                Location  = new Point(x, y), Size      = new Size(72, 28),
                BackColor = color, FlatStyle = FlatStyle.Flat, Text = ""
            };
            btn.FlatAppearance.BorderColor = Color.Gray;
            btn.Click += (s, e) =>
            {
                using var dlg = new ColorDialog
                    { Color = btn.BackColor, FullOpen = true, AllowFullOpen = true };
                if (dlg.ShowDialog() == DialogResult.OK)
                    onChanged(dlg.Color);
            };
            return btn;
        }

        private void UpdateSecVisible()
        {
            bool show       = _rbOnClick.Checked;
            _lblSec.Visible     = show;
            _nudSec.Visible     = show;
            _lblSecUnit.Visible = show;
        }

        private void LoadValues()
        {
            _rbAlways.Checked   = _settings.AlwaysShow;
            _rbOnClick.Checked  = !_settings.AlwaysShow;
            _nudSec.Value       = Math.Clamp(_settings.ShowDurationMs / 1000, 1, 30);
            _chkStartup.Checked = _settings.RunAtStartup;
            UpdateSecVisible();

            int ov = (int)Math.Round(_settings.MaxOpacity * 100);
            _tbOpacity.Value  = ov;
            _nudOpacity.Value = ov;

            _tbFont.Value  = (int)Math.Round(_settings.FontSize * 10);
            _nudFont.Value = Math.Clamp((decimal)Math.Round(_settings.FontSize, 1), 6, 30);

            _txtHangulLabel.Text = _settings.HangulLabel;
            _txtEngLower.Text    = _settings.EngLowerLabel;
            _txtEngUpper.Text    = _settings.EngUpperLabel;

            UpdatePreview();
        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            _settings.AlwaysShow     = _rbAlways.Checked;
            _settings.ShowDurationMs = (int)_nudSec.Value * 1000;
            _settings.RunAtStartup   = _chkStartup.Checked;
            _settings.MaxOpacity     = (double)_nudOpacity.Value / 100.0;
            _settings.FontSize       = (float)_nudFont.Value;
            _settings.HangulBgColor  = _hangulBg;
            _settings.HangulFgColor  = _hangulFg;
            _settings.HangulLabel    = string.IsNullOrEmpty(_txtHangulLabel.Text) ? "한" : _txtHangulLabel.Text;
            _settings.EngBgColor     = _engBg;
            _settings.EngFgColor     = _engFg;
            _settings.EngLowerLabel  = string.IsNullOrEmpty(_txtEngLower.Text) ? "a" : _txtEngLower.Text;
            _settings.EngUpperLabel  = string.IsNullOrEmpty(_txtEngUpper.Text) ? "A" : _txtEngUpper.Text;
            _settings.Save();
        }
    }
}
