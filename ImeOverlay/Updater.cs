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
        private const string REPO = "MinSoo97/CorsorImeOverlay.exe";
        private const string API  = $"https://api.github.com/repos/{REPO}/releases/latest";

        public static string CurrentVersion =>
            Assembly.GetExecutingAssembly()
                    .GetName().Version?.ToString(3) ?? "0.0.0";

        /// <summary>
        /// 버전 체크 후 업데이트 있으면 알림.
        /// silent=true(자동 백그라운드 체크)면 최신 버전이어도 메시지를 띄우지 않음.
        /// silent=false(수동 "업데이트 확인" 클릭)면 결과를 항상 알려줌.
        /// </summary>
        public static async Task CheckAsync(bool silent = true)
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
                    PromptUpdateOnUiThread(latest, downloadUrl);
                }
                else if (!silent)
                {
                    ShowNoUpdateOnUiThread(latest);
                }
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    ShowErrorOnUiThread(ex);
                }
            }
        }

        private static void PromptUpdateOnUiThread(string newVer, string downloadUrl)
        {
            RunOnUiThread(() => PromptUpdate(newVer, downloadUrl));
        }

        private static void ShowNoUpdateOnUiThread(string latest)
        {
            RunOnUiThread(() => UpdateDialog.ShowInfo(
                "최신 버전입니다",
                $"현재 최신 버전을 사용 중입니다.\n\n설치된 버전: v{CurrentVersion}"));
        }

        private static void ShowErrorOnUiThread(Exception ex)
        {
            RunOnUiThread(() => UpdateDialog.ShowWarning(
                "업데이트 확인 실패",
                $"업데이트 정보를 가져오지 못했습니다.\n\n{ex.Message}"));
        }

        private static void RunOnUiThread(Action action)
        {
            // 동기화 컨텍스트를 통해 UI 스레드에서 실행 (트레이 전용 앱 대응)
            if (System.Threading.SynchronizationContext.Current != null)
                System.Threading.SynchronizationContext.Current.Post(_ => action(), null);
            else
                action();
        }

        private static void PromptUpdate(string newVer, string downloadUrl)
        {
            var result = UpdateDialog.ShowYesNo(
                "새 버전이 있습니다",
                $"현재: v{CurrentVersion}\n최신: v{newVer}\n\n지금 업데이트 하시겠습니까?");


            if (result == DialogResult.Yes)
                DownloadAndReplace(downloadUrl);
        }

        private static async void DownloadAndReplace(string downloadUrl)
        {
            try
            {
                string currentExe = Process.GetCurrentProcess().MainModule!.FileName;
                string tempExe    = currentExe + ".new";

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

                // 현재 exe를 새 파일로 교체하는 PowerShell 스크립트 생성 후 실행
                // (cmd.exe의 bat 파일은 한글 경로 인코딩 문제가 발생하므로 PowerShell 사용)
                string logPath = Path.Combine(Path.GetTempPath(), "ime_update_log.txt");
                string ps1Path = Path.Combine(Path.GetTempPath(), "ime_update.ps1");

                string ps1 = $@"
$log = '{logPath.Replace("'", "''")}'
$tempExe = '{tempExe.Replace("'", "''")}'
$currentExe = '{currentExe.Replace("'", "''")}'

""[$(Get-Date)] update start"" | Out-File -FilePath $log -Encoding utf8

# 메인 프로세스가 완전히 종료될 때까지 대기 (최대 30초)
$waited = 0
while ((Get-Process -Name 'ImeOverlay' -ErrorAction SilentlyContinue) -and $waited -lt 30) {{
    Start-Sleep -Seconds 1
    $waited++
}}
""[$(Get-Date)] process exited after $waited s"" | Out-File -FilePath $log -Append -Encoding utf8

# 파일 교체 (최대 10회 재시도)
$retry = 0
$success = $false
while ($retry -lt 10 -and -not $success) {{
    try {{
        Move-Item -Path $tempExe -Destination $currentExe -Force -ErrorAction Stop
        $success = $true
    }} catch {{
        ""[$(Get-Date)] move attempt $retry failed: $($_.Exception.Message)"" | Out-File -FilePath $log -Append -Encoding utf8
        Start-Sleep -Seconds 1
        $retry++
    }}
}}

if ($success) {{
    ""[$(Get-Date)] move success, restarting"" | Out-File -FilePath $log -Append -Encoding utf8
    Start-Process -FilePath $currentExe
}} else {{
    ""[$(Get-Date)] move FAILED after retries"" | Out-File -FilePath $log -Append -Encoding utf8
}}
";

                File.WriteAllText(ps1Path, ps1, System.Text.Encoding.UTF8);

                // 메인 앱이 이미 관리자 권한(app.manifest)으로 실행 중이므로
                // 자식 PowerShell도 같은 권한을 상속받음 (별도 runas 불필요)
                Process.Start(new ProcessStartInfo
                {
                    FileName        = "powershell.exe",
                    Arguments       = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{ps1Path}\"",
                    CreateNoWindow  = true,
                    UseShellExecute = false,
                    WindowStyle     = ProcessWindowStyle.Hidden
                });

                Application.Exit();
            }
            catch (Exception ex)
            {
                UpdateDialog.ShowError("업데이트 실패", ex.Message);
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
        static readonly Color C_BG      = Color.FromArgb(18, 18, 28);
        static readonly Color C_TEXT    = Color.FromArgb(220, 220, 235);
        static readonly Color C_ACCENT  = Color.FromArgb(99, 87, 219);
        static readonly Color C_TRACK   = Color.FromArgb(40, 40, 58);

        private readonly Panel _track;
        private readonly Panel _fill;
        private readonly Label _lbl;
        private int _percent = 0;

        public ProgressForm()
        {
            Text            = "업데이트 다운로드 중...";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;
            ShowInTaskbar   = false;
            BackColor       = C_BG;
            ClientSize      = new Size(340, 110);
            Font            = new Font("맑은 고딕", 10f);

            _lbl = new Label
            {
                Text      = "다운로드 중... 0%",
                Location  = new Point(20, 22),
                Size      = new Size(300, 22),
                ForeColor = C_TEXT,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _track = new Panel
            {
                Location  = new Point(20, 56),
                Size      = new Size(300, 14),
                BackColor = C_TRACK
            };
            _fill = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(0, 14),
                BackColor = C_ACCENT
            };
            _track.Controls.Add(_fill);

            Controls.Add(_lbl);
            Controls.Add(_track);
        }

        public void SetProgress(int percent)
        {
            if (InvokeRequired) { Invoke(() => SetProgress(percent)); return; }
            _percent  = Math.Clamp(percent, 0, 100);
            _lbl.Text = $"다운로드 중... {_percent}%";
            _fill.Size = new Size((int)(_track.Width * (_percent / 100.0)), _track.Height);
        }
    }
}
