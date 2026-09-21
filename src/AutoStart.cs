using System;
using Microsoft.Win32;

namespace DeepBlue
{
    // 开机自启动：HKCU Run 键（用户级，无需管理员权限；卸载时删除键值即可）
    public static class AutoStart
    {
        private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string ValueName = "DeepBlue";

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (k == null) return false;
                    string v = k.GetValue(ValueName) as string;
                    return !string.IsNullOrEmpty(v);
                }
            }
            catch (Exception) { return false; }
        }

        // 写/删 Run 键值；失败（权限等）返回 false，调用方提示用户
        public static bool SetEnabled(bool on)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null) return false;
                    if (on)
                    {
                        k.SetValue(ValueName, "\"" + System.Windows.Forms.Application.ExecutablePath + "\"");
                    }
                    else if (k.GetValue(ValueName) != null)
                    {
                        k.DeleteValue(ValueName, false);
                    }
                    return true;
                }
            }
            catch (Exception) { return false; }
        }
    }
}
