using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImeOverlay
{
    internal static class Updater
    {
        // ← 본인 GitHub 저장소로 변경
        private const string REPO = "깃허브 주소";
        private const string API  = $"https://api.github.com/repos/{REPO}/releases/latest";

        public static string CurrentVersion =>
            Assembly.GetExecutingAssembly()
                    .GetName().Version?.ToString(3) ?? "0.0.0";

        /// <summary>
        /// 백그라운드에서 버전 체크 후 업데이트 있으면 알림
        /// </summary>
        public static async Task CheckAsync()
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "ImeOverlay");
                http.Timeout = TimeSpan.FromSeconds(5);

                string json = await http.GetStringAsync(API);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.GetProperty("tag_name").GetString() ?? "";
                string latest  = tagName.TrimStart('v');   // "v1.2.0" → "1.2.0"
                string downloadUrl = "";

                // assets 배열에서 ImeOverlay.exe 찾기
                if (root.TryGetProperty("assets", out var assets))
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        string name = asset.GetProperty("name").GetString() ?? "";
                        if (name.Equals("ImeOverlay.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                            break;
                        }
                    }
                }

                if (IsNewer(latest, CurrentVersion) && !string.IsNullOrEmpty(downloadUrl))
                {
                    // UI 스레드에서 알림
                    Application.OpenForms[0]?.Invoke(() =>
                        PromptUpdate(latest, downloadUrl));
                }
            }
            catch { /* 네트워크 없거나 실패 시 무시 */ }
        }

        private static void PromptUpdate(string newVer, string downloadUrl)
        {
            var result = MessageBox.Show(
                $"새 버전이 있습니다!\n\n현재: v{CurrentVersion}\n최신: v{newVer}\n\n지금 업데이트 하시겠습니까?",
                "ImeOverlay 업데이트",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
                DownloadAndReplace(downloadUrl);
        }

        private static async void DownloadAndReplace(string downloadUrl)
        {
            try
            {
                string currentExe = Process.GetCurrentProcess().MainModule!.FileName;
                string tempExe    = currentExe + ".new";
                string batPath    = Path.Combine(Path.GetTempPath(), "ime_update.bat");

                // 다운로드
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "ImeOverlay");
                http.Timeout = TimeSpan.FromSeconds(60);

                var prog = new ProgressForm();
                prog.Show();

                using var response = await http.GetAsync(downloadUrl,
                    HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long total   = response.Content.Headers.ContentLength ?? -1;
                long received = 0;

                await using var src  = await response.Content.ReadAsStreamAsync();
                await using var dest = File.Create(tempExe);

                var buffer = new byte[81920];
                int read;
                while ((read = await src.ReadAsync(buffer)) > 0)
                {
                    await dest.WriteAsync(buffer.AsMemory(0, read));
                    received += read;
                    if (total > 0)
                        prog.SetProgress((int)(received * 100 / total));
                }

                prog.Close();

                // 현재 exe를 새 파일로 교체하는 bat 파일 생성 후 실행
                // (실행 중인 exe는 직접 덮어쓸 수 없으므로 bat으로 처리)
                string bat = $@"@echo off
timeout /t 1 /nobreak >nul
move /y ""{tempExe}"" ""{currentExe}""
start """" ""{currentExe}""
del ""%~f0""";

                File.WriteAllText(batPath, bat);
                Process.Start(new ProcessStartInfo
                {
                    FileName        = batPath,
                    CreateNoWindow  = true,
                    UseShellExecute = true,
                    WindowStyle     = ProcessWindowStyle.Hidden
                });

                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"업데이트 실패:\n{ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>a가 b보다 최신인지 확인 (1.2.0 > 1.1.9)</summary>
        private static bool IsNewer(string a, string b)
        {
            if (Version.TryParse(a, out var va) && Version.TryParse(b, out var vb))
                return va > vb;
            return false;
        }
    }

    /// <summary>다운로드 진행률 폼</summary>
    internal class ProgressForm : Form
    {
        private readonly ProgressBar _pb;
        private readonly Label       _lbl;

        public ProgressForm()
        {
            Text            = "업데이트 다운로드 중...";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;
            Size            = new Size(320, 100);
            Font            = new Font("맑은 고딕", 9f);

            _lbl = new Label
            {
                Text      = "다운로드 중...",
                Location  = new Point(12, 12),
                Size      = new Size(280, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _pb = new ProgressBar
            {
                Location = new Point(12, 38),
                Size     = new Size(280, 22),
                Minimum  = 0,
                Maximum  = 100
            };
            Controls.Add(_lbl);
            Controls.Add(_pb);
        }

        public void SetProgress(int percent)
        {
            if (InvokeRequired) { Invoke(() => SetProgress(percent)); return; }
            _pb.Value  = Math.Clamp(percent, 0, 100);
            _lbl.Text  = $"다운로드 중... {percent}%";
        }
    }
}
