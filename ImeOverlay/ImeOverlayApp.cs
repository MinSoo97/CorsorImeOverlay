using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImeOverlay
{
    internal class ImeOverlayApp : ApplicationContext
    {
        private Settings    _settings;
        private NotifyIcon  _tray;
        private OverlayForm _overlay;
        private ImeMonitor  _monitor;
        private bool        _enabled = true;

        public ImeOverlayApp()
        {
            _settings = Settings.Load();
            _overlay  = new OverlayForm(_settings);
            _overlay.Show();
            _overlay.ApplySettings(_settings);

            _monitor = new ImeMonitor();
            _monitor.ImeStateChanged += OnImeChanged;
            _monitor.KeyTriggered    += () =>
            {
                if (_enabled && _overlay.IsHandleCreated)
                    _overlay.Invoke((Action)_overlay.ShowOverlay);
            };

            try { _monitor.Start(); }
            catch (Exception ex)
            {
                MessageBox.Show($"IME 모니터 시작 실패:\n{ex.Message}",
                    "ImeOverlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            _tray = new NotifyIcon
            {
                Icon    = MakeTrayIcon(true, false),
                Visible = true,
                Text    = "ImeOverlay",
                ContextMenuStrip = BuildMenu()
            };
            _tray.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) ToggleEnabled();
            };

            // 시작 3초 후 백그라운드 업데이트 체크
            var updateTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            updateTimer.Tick += async (s, e) =>
            {
                updateTimer.Stop();
                updateTimer.Dispose();
                await Updater.CheckAsync();
            };
            updateTimer.Start();
        }

        private void OnImeChanged(bool isHangul, bool isCaps)
        {
            if (!_enabled) return;
            if (_overlay.InvokeRequired)
                _overlay.Invoke(() => Apply(isHangul, isCaps));
            else
                Apply(isHangul, isCaps);
        }

        private void Apply(bool isHangul, bool isCaps)
        {
            _overlay.UpdateState(isHangul, isCaps);
            _tray.Icon = MakeTrayIcon(isHangul, isCaps);
            string label = isHangul ? "한" : (isCaps ? "A" : "a");
            _tray.Text = $"ImeOverlay - {label}";
        }

        private void ToggleEnabled()
        {
            _enabled         = !_enabled;
            _overlay.Visible = _enabled;
            _tray.Text       = _enabled ? "ImeOverlay" : "ImeOverlay - 일시 정지";
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            var itemSettings = new ToolStripMenuItem("환경설정");
            itemSettings.Click += (s, e) => OpenSettings();
            menu.Items.Add(itemSettings);

            var itemUpdate = new ToolStripMenuItem("업데이트 확인");
            itemUpdate.Click += async (s, e) => await Updater.CheckAsync();
            menu.Items.Add(itemUpdate);

            menu.Items.Add(new ToolStripSeparator());

            var itemToggle = new ToolStripMenuItem("일시 정지 / 재개");
            itemToggle.Click += (s, e) => ToggleEnabled();
            menu.Items.Add(itemToggle);

            menu.Items.Add(new ToolStripSeparator());

            // 버전 표시 (클릭 불가)
            var itemVer = new ToolStripMenuItem($"v{Updater.CurrentVersion}")
            {
                Enabled = false
            };
            menu.Items.Add(itemVer);

            menu.Items.Add(new ToolStripSeparator());

            var itemExit = new ToolStripMenuItem("종료");
            itemExit.Click += (s, e) => ExitApp();
            menu.Items.Add(itemExit);

            return menu;
        }

        private void OpenSettings()
        {
            using var form = new SettingsForm(_settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _settings = Settings.Load();
                _overlay.ApplySettings(_settings);
            }
        }

        private void ExitApp()
        {
            _monitor.Dispose();
            _overlay.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            Application.Exit();
        }

        private static Icon MakeTrayIcon(bool isHangul, bool isCaps)
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            Color bg = isHangul ? Color.FromArgb(83, 74, 183) : Color.FromArgb(15, 110, 86);
            using var br = new SolidBrush(bg);
            g.FillEllipse(br, 1, 1, 13, 13);
            string label = isHangul ? "한" : (isCaps ? "A" : "a");
            using var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var font = new Font("맑은 고딕", 7f, FontStyle.Bold);
            using var fg   = new SolidBrush(Color.White);
            g.DrawString(label, font, fg, new RectangleF(0, 0, 16, 16), sf);
            return Icon.FromHandle(bmp.GetHicon());
        }
    }
}
