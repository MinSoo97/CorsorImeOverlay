using System;
using System.Drawing;
using Microsoft.Win32;

namespace ImeOverlay
{
    internal class Settings
    {
        private const string REG_KEY  = @"Software\ImeOverlay";
        private const string RUN_KEY  = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RUN_NAME = "ImeOverlay";

        // ── 1페이지: 표시 모드 ────────────────────────────────
        public bool   AlwaysShow     { get; set; } = false;
        public int    ShowDurationMs { get; set; } = 3000;
        public bool   RunAtStartup   { get; set; } = false;

        // ── 2페이지: 스타일 ───────────────────────────────────
        public double MaxOpacity     { get; set; } = 1.0;
        public float  FontSize       { get; set; } = 11.5f;

        // 한글 모드
        public Color  HangulBgColor  { get; set; } = Color.FromArgb(83, 74, 183);
        public Color  HangulFgColor  { get; set; } = Color.FromArgb(238, 237, 254);
        public string HangulLabel    { get; set; } = "한";

        // 영문 소문자 모드
        public Color  EngBgColor     { get; set; } = Color.FromArgb(15, 110, 86);
        public Color  EngFgColor     { get; set; } = Color.FromArgb(225, 245, 238);
        public string EngLowerLabel  { get; set; } = "a";
        public string EngUpperLabel  { get; set; } = "A";

        public static Settings Load()
        {
            var s = new Settings();
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REG_KEY);
                if (key == null) return s;

                s.AlwaysShow     = (int)(key.GetValue("AlwaysShow",     0)    ?? 0)    == 1;
                s.ShowDurationMs = (int)(key.GetValue("ShowDurationMs", 3000) ?? 3000);
                s.MaxOpacity     = Math.Clamp(Convert.ToDouble(key.GetValue("MaxOpacity", 100)) / 100.0, 0.01, 1.0);
                s.FontSize       = Math.Clamp(Convert.ToSingle(key.GetValue("FontSize",   115)) / 10.0f, 6f, 30f);

                s.HangulBgColor  = LoadColor(key, "HangulBg",  s.HangulBgColor);
                s.HangulFgColor  = LoadColor(key, "HangulFg",  s.HangulFgColor);
                s.HangulLabel    = (key.GetValue("HangulLabel",   "한") as string) ?? "한";

                s.EngBgColor     = LoadColor(key, "EngBg",     s.EngBgColor);
                s.EngFgColor     = LoadColor(key, "EngFg",     s.EngFgColor);
                s.EngLowerLabel  = (key.GetValue("EngLowerLabel", "a") as string) ?? "a";
                s.EngUpperLabel  = (key.GetValue("EngUpperLabel", "A") as string) ?? "A";

                using var runKey = Registry.CurrentUser.OpenSubKey(RUN_KEY);
                s.RunAtStartup = runKey?.GetValue(RUN_NAME) != null;
            }
            catch { }
            return s;
        }

        public void Save()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(REG_KEY);
                key.SetValue("AlwaysShow",     AlwaysShow ? 1 : 0);
                key.SetValue("ShowDurationMs", ShowDurationMs);
                key.SetValue("MaxOpacity",     (int)Math.Round(MaxOpacity * 100));
                key.SetValue("FontSize",       (int)Math.Round(FontSize * 10));

                SaveColor(key, "HangulBg", HangulBgColor);
                SaveColor(key, "HangulFg", HangulFgColor);
                key.SetValue("HangulLabel",   HangulLabel);

                SaveColor(key, "EngBg", EngBgColor);
                SaveColor(key, "EngFg", EngFgColor);
                key.SetValue("EngLowerLabel", EngLowerLabel);
                key.SetValue("EngUpperLabel", EngUpperLabel);

                using var runKey = Registry.CurrentUser.OpenSubKey(RUN_KEY, writable: true);
                if (runKey == null) return;
                if (RunAtStartup)
                {
                    string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
                    runKey.SetValue(RUN_NAME, $"\"{exe}\"");
                }
                else
                {
                    runKey.DeleteValue(RUN_NAME, throwOnMissingValue: false);
                }
            }
            catch { }
        }

        private static Color LoadColor(RegistryKey key, string name, Color def)
        {
            if (key.GetValue(name) is int argb)
                return Color.FromArgb(argb);
            return def;
        }

        private static void SaveColor(RegistryKey key, string name, Color c)
            => key.SetValue(name, c.ToArgb(), RegistryValueKind.DWord);
    }
}
