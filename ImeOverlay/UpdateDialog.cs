using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImeOverlay
{
    /// <summary>SettingsForm과 동일한 다크 테마를 쓰는 공용 알림/확인 다이얼로그.</summary>
    internal class UpdateDialog : Form
    {
        static readonly Color C_BG      = Color.FromArgb(18, 18, 28);
        static readonly Color C_PANEL   = Color.FromArgb(32, 32, 48);
        static readonly Color C_ACCENT  = Color.FromArgb(99, 87, 219);
        static readonly Color C_TEXT    = Color.FromArgb(220, 220, 235);
        static readonly Color C_SUBTEXT = Color.FromArgb(140, 140, 160);
        static readonly Color C_BORDER  = Color.FromArgb(55, 55, 75);
        static readonly Color C_DANGER  = Color.FromArgb(220, 100, 100);

        public enum Kind { Info, Question, Warning, Error }

        private UpdateDialog(string title, string message, Kind kind, bool showCancel)
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;
            ShowInTaskbar   = false;
            BackColor       = C_BG;
            Font            = new Font("맑은 고딕", 10f);
            Text            = "ImeOverlay";

            int msgW = 306;

            // 메시지 줄 수에 맞춰 높이 계산 (줄당 약 22px + 여유)
            int lineCount = 1;
            foreach (var line in message.Split('\n')) lineCount++;
            int msgH = Math.Max(60, lineCount * 22);

            int msgTop    = 62;
            int btnTop    = msgTop + msgH + 16;
            int clientH   = btnTop + 34 + 20;

            ClientSize = new Size(420, clientH);

            // 아이콘 색상/문자
            var (iconBg, iconChar) = kind switch
            {
                Kind.Question => (C_ACCENT, "?"),
                Kind.Warning  => (Color.FromArgb(210, 160, 60), "!"),
                Kind.Error    => (C_DANGER, "!"),
                _             => (Color.FromArgb(70, 160, 130), "i"),
            };

            var iconPanel = new Panel
            {
                Location  = new Point(28, 28),
                Size      = new Size(44, 44),
                BackColor = Color.Transparent
            };
            iconPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var br = new SolidBrush(iconBg);
                g.FillEllipse(br, 0, 0, 44, 44);
                using var fnt = new Font("맑은 고딕", 18f, FontStyle.Bold);
                using var fg  = new SolidBrush(Color.White);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(iconChar, fnt, fg, new RectangleF(0, 0, 44, 44), sf);
            };

            var lblTitle = new Label
            {
                Text      = title,
                Location  = new Point(86, 28),
                Size      = new Size(msgW, 28),
                ForeColor = C_TEXT,
                Font      = new Font("맑은 고딕", 12f, FontStyle.Bold),
                AutoSize  = false
            };

            var lblMsg = new Label
            {
                Text      = message,
                Location  = new Point(86, msgTop),
                Size      = new Size(msgW, msgH),
                ForeColor = C_SUBTEXT,
                Font      = new Font("맑은 고딕", 10f),
                AutoSize  = false
            };

            Controls.Add(iconPanel);
            Controls.Add(lblTitle);
            Controls.Add(lblMsg);

            // 버튼
            if (showCancel)
            {
                var btnNo = new DarkDialogButton("취소", Color.Transparent, C_TEXT)
                    { Location = new Point(ClientSize.Width - 200, btnTop), Size = new Size(86, 34), DialogResult = DialogResult.No };
                var btnYes = new DarkDialogButton("업데이트", C_ACCENT, Color.White)
                    { Location = new Point(ClientSize.Width - 106, btnTop), Size = new Size(86, 34), DialogResult = DialogResult.Yes };
                Controls.Add(btnNo);
                Controls.Add(btnYes);
                AcceptButton = btnYes;
                CancelButton = btnNo;
            }
            else
            {
                var btnOk = new DarkDialogButton("확인", C_ACCENT, Color.White)
                    { Location = new Point(ClientSize.Width - 106, btnTop), Size = new Size(86, 34), DialogResult = DialogResult.OK };
                Controls.Add(btnOk);
                AcceptButton = btnOk;
                CancelButton = btnOk;
            }
        }

        public static DialogResult ShowYesNo(string title, string message) =>
            new UpdateDialog(title, message, Kind.Question, showCancel: true).ShowDialog();

        public static void ShowInfo(string title, string message) =>
            new UpdateDialog(title, message, Kind.Info, showCancel: false).ShowDialog();

        public static void ShowWarning(string title, string message) =>
            new UpdateDialog(title, message, Kind.Warning, showCancel: false).ShowDialog();

        public static void ShowError(string title, string message) =>
            new UpdateDialog(title, message, Kind.Error, showCancel: false).ShowDialog();
    }

    internal class DarkDialogButton : Button
    {
        public DarkDialogButton(string text, Color bg, Color fg)
        {
            Text       = text;
            BackColor  = bg;
            ForeColor  = fg;
            FlatStyle  = FlatStyle.Flat;
            FlatAppearance.BorderSize  = bg == Color.Transparent ? 1 : 0;
            FlatAppearance.BorderColor = Color.FromArgb(55, 55, 75);
            Font       = new Font("맑은 고딕", 10f);
            Cursor     = Cursors.Hand;
        }
    }
}
