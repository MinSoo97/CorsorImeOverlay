using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ImeOverlay
{
    internal class SettingsForm : Form
    {
        static readonly Color C_BG      = Color.FromArgb(18, 18, 28);
        static readonly Color C_SIDEBAR = Color.FromArgb(25, 25, 38);
        static readonly Color C_PANEL   = Color.FromArgb(32, 32, 48);
        static readonly Color C_CARD    = Color.FromArgb(40, 40, 58);
        static readonly Color C_ACCENT  = Color.FromArgb(99, 87, 219);
        static readonly Color C_TEXT    = Color.FromArgb(220, 220, 235);
        static readonly Color C_SUBTEXT = Color.FromArgb(140, 140, 160);
        static readonly Color C_BORDER  = Color.FromArgb(55, 55, 75);

        private readonly Settings _settings;
        private bool _syncing = false;

        private DarkButton _btnPage1 = null!;
        private DarkButton _btnPage2 = null!;
        private Panel _page1 = null!;
        private Panel _page2 = null!;

        // 1페이지
        private DarkRadio   _rbAlways   = null!;
        private DarkRadio   _rbOnClick  = null!;
        private Label       _lblSecTitle = null!;
        private DarkNumeric _nudSec     = null!;
        private Label       _lblSecUnit = null!;
        private DarkCheck   _chkStartup = null!;

        // 2페이지
        private TrackBar    _tbOpacity  = null!;
        private DarkNumeric _nudOpacity = null!;
        private TrackBar    _tbFont     = null!;
        private DarkNumeric _nudFont    = null!;
        private DarkColorBtn _btnHangulBg = null!, _btnHangulFg = null!;
        private DarkColorBtn _btnEngBg    = null!, _btnEngFg    = null!;
        private DarkTextBox  _txtHangulLabel = null!;
        private DarkTextBox  _txtEngLower    = null!;
        private DarkTextBox  _txtEngUpper    = null!;
        private Panel         _previewPanel  = null!;

        private Color _hangulBg, _hangulFg, _engBg, _engFg;

        public SettingsForm(Settings s)
        {
            _settings = s;
            _hangulBg = s.HangulBgColor; _hangulFg = s.HangulFgColor;
            _engBg    = s.EngBgColor;    _engFg    = s.EngFgColor;
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
            ClientSize      = new Size(760, 600);
            BackColor       = C_BG;
            Font            = new Font("맑은 고딕", 10f);

            int sidebarW = 190;
            int btnBarH  = 58;
            int contentW = ClientSize.Width - sidebarW;
            int contentH = ClientSize.Height - btnBarH;

            // ── 사이드바 ──────────────────────────────────────
            var sidebar = new Panel
            {
                Location = new Point(0, 0),
                Size     = new Size(sidebarW, ClientSize.Height),
                BackColor = C_SIDEBAR
            };
            var lblTitle = new Label
            {
                Text = "ImeOverlay", Location = new Point(0, 28), Size = new Size(sidebarW, 36),
                ForeColor = C_TEXT, Font = new Font("맑은 고딕", 13f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblSub = new Label
            {
                Text = "환경설정", Location = new Point(0, 62), Size = new Size(sidebarW, 22),
                ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 9f),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var sep = new Panel { Location = new Point(20, 96), Size = new Size(sidebarW - 40, 1), BackColor = C_BORDER };

            _btnPage1 = new DarkButton("⚙  표시 설정", C_ACCENT, C_TEXT)
                { Location = new Point(0, 112), Size = new Size(sidebarW, 48) };
            _btnPage2 = new DarkButton("🎨  스타일", C_ACCENT, C_SUBTEXT)
                { Location = new Point(0, 160), Size = new Size(sidebarW, 48) };
            _btnPage1.Click += (s, e) => ShowPage(1);
            _btnPage2.Click += (s, e) => ShowPage(2);

            var lblVer = new Label
            {
                Text = $"v{Updater.CurrentVersion}", Location = new Point(0, ClientSize.Height - 36),
                Size = new Size(sidebarW, 28), ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 8.5f),
                TextAlign = ContentAlignment.MiddleCenter
            };
            sidebar.Controls.AddRange(new Control[] { lblTitle, lblSub, sep, _btnPage1, _btnPage2, lblVer });

            // ── 콘텐츠 패널들 (사이드바 옆, 버튼바 위까지) ────
            _page1 = new Panel
            {
                Location = new Point(sidebarW, 0), Size = new Size(contentW, contentH),
                BackColor = C_PANEL, Visible = true
            };
            _page2 = new Panel
            {
                Location = new Point(sidebarW, 0), Size = new Size(contentW, contentH),
                BackColor = C_PANEL, Visible = false
            };
            BuildPage1Content(_page1, contentW, contentH);
            BuildPage2Content(_page2, contentW, contentH);

            // ── 하단 버튼 바 ─────────────────────────────────
            var btnBar = new Panel
            {
                Location = new Point(sidebarW, contentH), Size = new Size(contentW, btnBarH),
                BackColor = C_SIDEBAR
            };
            var btnOk = new DarkButton("확인", C_ACCENT, Color.White)
                { Location = new Point(contentW - 210, 12), Size = new Size(96, 36) };
            var btnCancel = new DarkButton("취소", Color.Transparent, C_TEXT)
                { Location = new Point(contentW - 106, 12), Size = new Size(96, 36) };
            var btnReset = new DarkButton("초기화", Color.Transparent, Color.FromArgb(220, 100, 100))
                { Location = new Point(10, 12), Size = new Size(96, 36) };

            btnOk.DialogResult     = DialogResult.OK;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnOk.Click    += OnOkClick;
            btnReset.Click += OnResetClick;
            btnBar.Controls.AddRange(new Control[] { btnReset, btnOk, btnCancel });

            Controls.Add(sidebar);
            Controls.Add(_page1);
            Controls.Add(_page2);
            Controls.Add(btnBar);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void ShowPage(int n)
        {
            _page1.Visible = (n == 1);
            _page2.Visible = (n == 2);
            _btnPage1.SetActive(n == 1);
            _btnPage2.SetActive(n == 2);
        }

        // ── 1페이지 ───────────────────────────────────────────
        private void BuildPage1Content(Panel page, int w, int h)
        {
            int lx = 28, ly = 28, cardW = w - 56;

            PageTitle(page, "표시 설정", lx, ly); ly += 50;

            var card = DarkCard(lx, ly, cardW, 160); ly += 176;
            CardLabel(card, "표시 모드", 16, 14);

            _rbAlways = new DarkRadio("항상 표시")
                { Location = new Point(16, 46), Size = new Size(400, 28) };
            _rbOnClick = new DarkRadio("클릭 / 한영 / CapsLock 시 표시 후 사라짐")
                { Location = new Point(16, 80), Size = new Size(420, 28) };
            _rbOnClick.CheckedChanged += (s, e) => UpdateSecVisible();

            _lblSecTitle = new Label
            {
                Text = "표시 시간:", Location = new Point(36, 118), Size = new Size(76, 28),
                ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 9.5f), TextAlign = ContentAlignment.MiddleLeft
            };
            _nudSec = new DarkNumeric
                { Location = new Point(116, 114), Size = new Size(70, 28), Minimum = 1, Maximum = 30 };
            _lblSecUnit = new Label
            {
                Text = "초", Location = new Point(194, 118), Size = new Size(28, 28),
                ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 9.5f), TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.AddRange(new Control[] { _rbAlways, _rbOnClick, _lblSecTitle, _nudSec, _lblSecUnit });
            page.Controls.Add(card);

            var card2 = DarkCard(lx, ly, cardW, 64);
            CardLabel(card2, "시작 프로그램", 16, 14);
            _chkStartup = new DarkCheck("Windows 시작 시 자동 실행")
                { Location = new Point(16, 38), Size = new Size(380, 26) };
            card2.Controls.Add(_chkStartup);
            page.Controls.Add(card2);
        }

        // ── 2페이지 ───────────────────────────────────────────
        private void BuildPage2Content(Panel page, int w, int h)
        {
            int previewW = 200;
            int gap      = 16;
            int lx = 16, ly = 16;
            int leftW = w - previewW - gap * 3;

            PageTitle(page, "스타일", lx + 12, ly); ly += 50;

            // 투명도
            var cOpa = DarkCard(lx, ly, leftW, 72); ly += 84;
            CardLabel(cOpa, "최대 투명도", 14, 10);
            _tbOpacity = new TrackBar
            {
                Location = new Point(14, 34), Size = new Size(leftW - 130, 36),
                Minimum = 10, Maximum = 100, TickFrequency = 10, TickStyle = TickStyle.None
            };
            _nudOpacity = new DarkNumeric
                { Location = new Point(leftW - 110, 30), Size = new Size(60, 28), Minimum = 10, Maximum = 100 };
            var lblPct = new Label { Text = "%", Location = new Point(leftW - 42, 34), Size = new Size(24, 24),
                ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 9.5f), TextAlign = ContentAlignment.MiddleLeft };
            _tbOpacity.Scroll += (s, e) => { if (!_syncing) { _syncing = true; _nudOpacity.Value = _tbOpacity.Value; _syncing = false; UpdatePreview(); } };
            _nudOpacity.ValueChanged += (s, e) => { if (!_syncing) { _syncing = true; _tbOpacity.Value = (int)_nudOpacity.Value; _syncing = false; UpdatePreview(); } };
            cOpa.Controls.AddRange(new Control[] { _tbOpacity, _nudOpacity, lblPct });
            page.Controls.Add(cOpa);

            // 글자 크기
            var cFont = DarkCard(lx, ly, leftW, 72); ly += 84;
            CardLabel(cFont, "글자 크기", 14, 10);
            _tbFont = new TrackBar
            {
                Location = new Point(14, 34), Size = new Size(leftW - 140, 36),
                Minimum = 60, Maximum = 300, TickFrequency = 20, SmallChange = 5, TickStyle = TickStyle.None
            };
            _nudFont = new DarkNumeric
            {
                Location = new Point(leftW - 120, 30), Size = new Size(68, 28),
                Minimum = 6, Maximum = 30, DecimalPlaces = 1, Increment = 0.5m
            };
            var lblPt = new Label { Text = "pt", Location = new Point(leftW - 46, 34), Size = new Size(28, 24),
                ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 9.5f), TextAlign = ContentAlignment.MiddleLeft };
            _tbFont.Scroll += (s, e) => { if (!_syncing) { _syncing = true; _nudFont.Value = _tbFont.Value / 10m; _syncing = false; UpdatePreview(); } };
            _nudFont.ValueChanged += (s, e) => { if (!_syncing) { _syncing = true; _tbFont.Value = (int)Math.Round((double)_nudFont.Value * 10); _syncing = false; UpdatePreview(); } };
            cFont.Controls.AddRange(new Control[] { _tbFont, _nudFont, lblPt });
            page.Controls.Add(cFont);

            // 한글 모드 — 세로로 한 줄씩: 배경색 줄 / 글자색 줄 / 표시글자 줄
            var cH = DarkCard(lx, ly, leftW, 150); ly += 162;
            CardLabel(cH, "한글 모드", 14, 10);

            AddColorRow(cH, "배경색:", 14, 36, _hangulBg, c => { _hangulBg = c; UpdatePreview(); }, out _btnHangulBg);
            AddColorRow(cH, "글자색:", 14, 72, _hangulFg, c => { _hangulFg = c; UpdatePreview(); }, out _btnHangulFg);

            var lblHText = new Label
            {
                Text = "표시 글자:", Location = new Point(14, 110), Size = new Size(74, 28),
                ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 9.5f), TextAlign = ContentAlignment.MiddleLeft
            };
            _txtHangulLabel = new DarkTextBox
                { Location = new Point(94, 106), Size = new Size(130, 30), MaxLength = 5 };
            _txtHangulLabel.TextChanged += (s, e) => UpdatePreview();
            cH.Controls.Add(lblHText);
            cH.Controls.Add(_txtHangulLabel);
            page.Controls.Add(cH);

            // 영문 모드
            var cE = DarkCard(lx, ly, leftW, 150);
            CardLabel(cE, "영문 모드", 14, 10);

            AddColorRow(cE, "배경색:", 14, 36, _engBg, c => { _engBg = c; UpdatePreview(); }, out _btnEngBg);
            AddColorRow(cE, "글자색:", 14, 72, _engFg, c => { _engFg = c; UpdatePreview(); }, out _btnEngFg);

            var lblLower = new Label
            {
                Text = "소문자:", Location = new Point(14, 110), Size = new Size(60, 28),
                ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 9.5f), TextAlign = ContentAlignment.MiddleLeft
            };
            _txtEngLower = new DarkTextBox
                { Location = new Point(78, 106), Size = new Size(80, 30), MaxLength = 5 };
            _txtEngLower.TextChanged += (s, e) => UpdatePreview();

            var lblUpper = new Label
            {
                Text = "대문자:", Location = new Point(170, 110), Size = new Size(60, 28),
                ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 9.5f), TextAlign = ContentAlignment.MiddleLeft
            };
            _txtEngUpper = new DarkTextBox
                { Location = new Point(234, 106), Size = new Size(80, 30), MaxLength = 5 };
            _txtEngUpper.TextChanged += (s, e) => UpdatePreview();

            cE.Controls.AddRange(new Control[] { lblLower, _txtEngLower, lblUpper, _txtEngUpper });
            page.Controls.Add(cE);

            // ── 미리보기 (우측) ──────────────────────────────
            int previewX = lx + leftW + gap;
            var previewCard = DarkCard(previewX, 16, previewW, h - 32);
            CardLabel(previewCard, "미리보기", 14, 10);
            _previewPanel = new Panel
            {
                Location  = new Point(12, 40),
                Size      = new Size(previewW - 24, h - 32 - 56),
                BackColor = Color.FromArgb(50, 50, 66)
            };
            _previewPanel.Paint += OnPreviewPaint;
            previewCard.Controls.Add(_previewPanel);
            page.Controls.Add(previewCard);
        }

        private void AddColorRow(Panel parent, string labelText, int x, int y,
            Color color, Action<Color> onChanged, out DarkColorBtn btn)
        {
            var lbl = new Label
            {
                Text = labelText, Location = new Point(x, y), Size = new Size(60, 28),
                ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 9.5f), TextAlign = ContentAlignment.MiddleLeft
            };
            btn = new DarkColorBtn(color, onChanged)
                { Location = new Point(x + 64, y - 2), Size = new Size(64, 28) };
            parent.Controls.Add(lbl);
            parent.Controls.Add(btn);
        }

        // ── 미리보기 렌더링 ───────────────────────────────────
        private void OnPreviewPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.FromArgb(50, 50, 66));

            float fs = (float)(_nudFont?.Value ?? 11.5m);
            using var badgeFont = new Font("맑은 고딕", fs, FontStyle.Bold, GraphicsUnit.Point);

            string hLbl = _txtHangulLabel?.Text is { Length: > 0 } ht ? ht : "한";
            string lLbl = _txtEngLower?.Text    is { Length: > 0 } lt ? lt : "a";
            string uLbl = _txtEngUpper?.Text    is { Length: > 0 } ut ? ut : "A";

            int cx = 16, cy = 24;

            DrawCursor(g, cx, cy);
            var (hw, hh) = MeasureBadge(g, badgeFont, hLbl);
            DrawBadge(g, cx + 14, cy + 14, hw, hh, badgeFont, _hangulBg, _hangulFg, hLbl);
            DrawRowLabel(g, cx + 14 + hw + 6, cy + 14 + (hh - 14) / 2, "한글");

            int s1 = cy + 14 + hh + 16; DrawSep(g, s1, _previewPanel.Width);
            int cy2 = s1 + 12;
            DrawCursor(g, cx, cy2);
            var (lw, lh) = MeasureBadge(g, badgeFont, lLbl);
            DrawBadge(g, cx + 14, cy2 + 14, lw, lh, badgeFont, _engBg, _engFg, lLbl);
            DrawRowLabel(g, cx + 14 + lw + 6, cy2 + 14 + (lh - 14) / 2, "소문자");

            int s2 = cy2 + 14 + lh + 16; DrawSep(g, s2, _previewPanel.Width);
            int cy3 = s2 + 12;
            DrawCursor(g, cx, cy3);
            var (uw, uh) = MeasureBadge(g, badgeFont, uLbl);
            DrawBadge(g, cx + 14, cy3 + 14, uw, uh, badgeFont, _engBg, _engFg, uLbl);
            DrawRowLabel(g, cx + 14 + uw + 6, cy3 + 14 + (uh - 14) / 2, "대문자");
        }

        private static (int w, int h) MeasureBadge(Graphics g, Font f, string t)
        {
            var sz = g.MeasureString(t, f);
            return (Math.Max(32, (int)Math.Ceiling(sz.Width) + 14), Math.Max(22, (int)Math.Ceiling(sz.Height) + 8));
        }
        private static void DrawSep(Graphics g, int y, int w) { using var p = new Pen(Color.FromArgb(75, 75, 95)); g.DrawLine(p, 8, y, w - 8, y); }
        private static void DrawRowLabel(Graphics g, int x, int y, string t) { using var f = new Font("맑은 고딕", 8.5f); using var b = new SolidBrush(Color.FromArgb(170, 170, 190)); g.DrawString(t, f, b, x, y); }
        private static void DrawCursor(Graphics g, int x, int y)
        {
            var pts = new Point[] { new(x, y), new(x, y + 14), new(x + 4, y + 10), new(x + 7, y + 16), new(x + 9, y + 15), new(x + 6, y + 9), new(x + 11, y + 9) };
            using var br = new SolidBrush(Color.White); using var pn = new Pen(Color.Black, 1f);
            g.FillPolygon(br, pts); g.DrawPolygon(pn, pts);
        }
        private static void DrawBadge(Graphics g, int x, int y, int w, int h, Font f, Color bg, Color fg, string lbl)
        {
            using var br = new SolidBrush(bg); using var path = RoundedRect(new Rectangle(x, y, w, h), 5); g.FillPath(br, path);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoClip };
            using var fb = new SolidBrush(fg); g.DrawString(lbl, f, fb, new RectangleF(x, y, w, h), sf);
        }
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var p = new GraphicsPath(); int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
        private void UpdatePreview() => _previewPanel?.Invalidate();

        // ── 헬퍼 ─────────────────────────────────────────────
        private Panel DarkCard(int x, int y, int w, int h)
        {
            var c = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = C_CARD };
            c.Paint += (s, e) => { using var p = new Pen(C_BORDER); e.Graphics.DrawRectangle(p, 0, 0, c.Width - 1, c.Height - 1); };
            return c;
        }
        private void CardLabel(Panel card, string text, int x, int y)
        {
            card.Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(200, 20),
                ForeColor = C_SUBTEXT, Font = new Font("맑은 고딕", 8.5f), AutoSize = false });
        }
        private void PageTitle(Panel page, string text, int x, int y)
        {
            page.Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(300, 32),
                ForeColor = C_TEXT, Font = new Font("맑은 고딕", 14f, FontStyle.Bold), AutoSize = false });
        }

        private void UpdateSecVisible()
        {
            bool show = _rbOnClick.Checked;
            _lblSecTitle.Visible = show;
            _nudSec.Visible      = show;
            _lblSecUnit.Visible  = show;
        }

        private void LoadValues()
        {
            _rbAlways.Checked   = _settings.AlwaysShow;
            _rbOnClick.Checked  = !_settings.AlwaysShow;
            _nudSec.Value       = Math.Clamp(_settings.ShowDurationMs / 1000, 1, 30);
            _chkStartup.Checked = _settings.RunAtStartup;
            UpdateSecVisible();

            int ov = (int)Math.Round(_settings.MaxOpacity * 100);
            _tbOpacity.Value = ov; _nudOpacity.Value = ov;
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

        private void OnResetClick(object? sender, EventArgs e)
        {
            if (MessageBox.Show("모든 설정을 초기값으로 되돌리겠습니까?", "초기화",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            _settings.AlwaysShow     = false;
            _settings.ShowDurationMs = 3000;
            _settings.RunAtStartup   = false;
            _settings.MaxOpacity     = 1.0;
            _settings.FontSize       = 11.5f;
            _hangulBg = _settings.HangulBgColor = Color.FromArgb(83, 74, 183);
            _hangulFg = _settings.HangulFgColor = Color.FromArgb(238, 237, 254);
            _settings.HangulLabel   = "한";
            _engBg = _settings.EngBgColor = Color.FromArgb(15, 110, 86);
            _engFg = _settings.EngFgColor = Color.FromArgb(225, 245, 238);
            _settings.EngLowerLabel = "a";
            _settings.EngUpperLabel = "A";

            _btnHangulBg.UpdateColor(_hangulBg);
            _btnHangulFg.UpdateColor(_hangulFg);
            _btnEngBg.UpdateColor(_engBg);
            _btnEngFg.UpdateColor(_engFg);

            LoadValues();
        }
    }

    // ── 커스텀 컨트롤 ─────────────────────────────────────────
    internal class DarkButton : Button
    {
        private readonly Color _activeBg;
        private readonly Color _textColor;
        private bool _isActive;

        public DarkButton(string text, Color activeBg, Color textColor)
        {
            Text       = text;
            _activeBg  = activeBg;
            _textColor = textColor;
            FlatStyle  = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = ControlPaint.Light(activeBg, 0.1f);
            BackColor  = Color.Transparent;
            ForeColor  = textColor;
            Font       = new Font("맑은 고딕", 10f);
            Cursor     = Cursors.Hand;
            TextAlign  = ContentAlignment.MiddleCenter;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            BackColor = active ? _activeBg : Color.Transparent;
            ForeColor = active ? Color.White : _textColor;
            FlatAppearance.MouseOverBackColor = active ? _activeBg : Color.FromArgb(40, 40, 58);
            Invalidate();
        }
    }

    internal class DarkRadio : RadioButton
    {
        static readonly Color C_TEXT = Color.FromArgb(220, 220, 235);
        public DarkRadio(string text) { Text = text; ForeColor = C_TEXT; Font = new Font("맑은 고딕", 10f); BackColor = Color.Transparent; }
    }

    internal class DarkCheck : CheckBox
    {
        static readonly Color C_TEXT = Color.FromArgb(220, 220, 235);
        public DarkCheck(string text) { Text = text; ForeColor = C_TEXT; Font = new Font("맑은 고딕", 10f); BackColor = Color.Transparent; }
    }

    internal class DarkTextBox : TextBox
    {
        static readonly Color C_INPUT = Color.FromArgb(28, 28, 42);
        static readonly Color C_TEXT  = Color.FromArgb(220, 220, 235);
        public DarkTextBox()
        {
            BackColor = C_INPUT; ForeColor = C_TEXT;
            BorderStyle = BorderStyle.FixedSingle;
            Font = new Font("맑은 고딕", 10f);
            TextAlign = HorizontalAlignment.Center;
        }
    }

    internal class DarkNumeric : NumericUpDown
    {
        static readonly Color C_INPUT = Color.FromArgb(28, 28, 42);
        static readonly Color C_TEXT  = Color.FromArgb(220, 220, 235);
        public DarkNumeric()
        {
            BackColor = C_INPUT; ForeColor = C_TEXT;
            Font = new Font("맑은 고딕", 10f);
            TextAlign = HorizontalAlignment.Center;
        }
    }

    internal class DarkColorBtn : Button
    {
        private readonly Action<Color> _onChanged;
        public DarkColorBtn(Color color, Action<Color> onChanged)
        {
            _onChanged = onChanged;
            BackColor  = color;
            FlatStyle  = FlatStyle.Flat;
            FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
            FlatAppearance.BorderSize  = 1;
            Text   = "";
            Cursor = Cursors.Hand;
            Click += OnClick;
        }
        private void OnClick(object? s, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = BackColor, FullOpen = true, AllowFullOpen = true };
            if (dlg.ShowDialog() == DialogResult.OK) { BackColor = dlg.Color; _onChanged(dlg.Color); }
        }
        public void UpdateColor(Color c) { BackColor = c; _onChanged(c); }
    }
}
