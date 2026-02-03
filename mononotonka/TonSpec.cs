using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Win32;

namespace Mononotonka
{
    /// <summary>
    /// システムスペック情報を取得・ログ出力するためのクラスです。
    /// </summary>
    public static class TonSpec
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        /// <summary>
        /// スペック情報を取得してログに出力します。
        /// </summary>
        public static void LogSpecs()
        {
            Ton.Log.Info("========== SYSTEM SPECS ==========");
            
            // 1. OS Info
            Ton.Log.Info($"OS: {Environment.OSVersion} ({(Environment.Is64BitOperatingSystem ? "64bit" : "32bit")})");

            // 2. CPU Info
            string cpuName = GetCpuName();
            Ton.Log.Info($"CPU: {cpuName}");
            Ton.Log.Info($"Logical Cores: {Environment.ProcessorCount}");

            // 3. RAM Info
            LogMemoryInfo();

            // 4. GPU Info
            LogGpuInfo();

            Ton.Log.Info("==================================");
        }

        private static string GetCpuName()
        {
            // Windows専用APIを使用するためガード
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                    {
                        if (key != null)
                        {
                            var name = key.GetValue("ProcessorNameString");
                            if (name != null) return name.ToString().Trim();
                        }
                    }
                }
                catch { }
            }
            return "Unknown CPU (Platform independent info not available)";
        }

        private static void LogMemoryInfo()
        {
            // Windows専用APIを使用するためガード
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(memStatus))
                    {
                        Ton.Log.Info($"Total RAM: {FormatBytes(memStatus.ullTotalPhys)}");
                        Ton.Log.Info($"Free RAM:  {FormatBytes(memStatus.ullAvailPhys)}");
                        Ton.Log.Info($"Memory Load: {memStatus.dwMemoryLoad}%");
                    }
                    else
                    {
                        Ton.Log.Warning("Failed to retrieve memory status.");
                    }
                }
                catch (Exception ex)
                {
                    Ton.Log.Warning($"Error getting memory info: {ex.Message}");
                }
            }
            else
            {
                Ton.Log.Info("RAM Info: Not supported on this platform.");
            }
        }

        private static void LogGpuInfo()
        {
            try
            {
                var adapter = GraphicsAdapter.DefaultAdapter;
                if (adapter != null)
                {
                    Ton.Log.Info($"GPU: {adapter.Description}");
                    // RefreshRate property is not available in standard MonoGame DisplayMode.
                    // Removed to fix CS1061 error.
                    Ton.Log.Info($"Display Mode: {adapter.CurrentDisplayMode.Width}x{adapter.CurrentDisplayMode.Height} ({adapter.CurrentDisplayMode.Format})");
                }
                else
                {
                    Ton.Log.Warning("No Default Graphics Adapter found.");
                }
            }
            catch (Exception ex)
            {
                Ton.Log.Warning($"Error getting GPU info: {ex.Message}");
            }
        }

        private static string FormatBytes(ulong bytes)
        {
            double mb = bytes / (1024.0 * 1024.0);
            if (mb > 1024.0)
            {
                double gb = mb / 1024.0;
                return $"{gb:F2} GB";
            }
            return $"{mb:F2} MB";
        }
    }
}
