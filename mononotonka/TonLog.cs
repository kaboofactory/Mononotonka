using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Mononotonka
{
    /// <summary>
    /// ログ出力管理クラスです。
    /// テキストファイルへのログ保存と、デバッグコンソールへの出力を担当します。
    /// </summary>
    public class TonLog
    {
        private string _logFilePath;
        private const int MaxLogFiles = 30;
        private const string LogLevelInfo = "INFO";
        private const string LogLevelWarn = "WARN";
        private const string LogLevelError = "ERROR";
        private const string LogLevelDebug = "DEBUG";

        /// <summary>最後のログメッセージ</summary>
        public string LastLog { get; private set; } = "";

        /// <summary>
        /// コンストラクタ。ログファイルのセットアップを行います。
        /// </summary>
        public TonLog()
        {
            SetupLogFile();
        }

        private void SetupLogFile()
        {
            try
            {
                // 実行ファイルの場所にある log フォルダを使用
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                // 古いログを削除して整理（最新30件を保持）
                var files = Directory.GetFiles(logDir, "ton.*.log")
                                     .OrderByDescending(f => f)
                                     .ToList();
                
                while (files.Count >= MaxLogFiles)
                {
                    try
                    {
                        File.Delete(files.Last());
                        files.RemoveAt(files.Count - 1);
                    }
                    catch { break; }
                }

                // 新しいログファイル名（タイムスタンプ付き）
                string filename = $"ton.{DateTime.Now:yyyyMMddHHmmss}.log";
                _logFilePath = Path.Combine(logDir, filename);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TonLog Setup Failed: {ex.Message}");
            }
        }

        private void WriteLog(string level, string message, string file, int line)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string filename = Path.GetFileName(file);
                string logLine = $"[{timestamp}] [{level}] {message} ({filename}:{line}){Environment.NewLine}";
                LastLog = $"[{level}] {message}"; // 画面表示用に短く保持
                File.AppendAllText(_logFilePath, logLine);

                // WARNとERRORはVisual Studio等のデバッグ出力にも表示
                if (level == LogLevelWarn || level == LogLevelError)
                {
                    System.Diagnostics.Debug.Write(logLine);
                }
            }
            catch (Exception)
            {
                // ログ書き込みエラーは無視（再帰的なエラーを防ぐため）
            }
        }

        /// <summary>
        /// 情報ログ(INFO)を出力します。
        /// </summary>
        /// <param name="message">メッセージ内容</param>
        /// <param name="file">呼び出し元ファイル（自動付与）</param>
        /// <param name="line">呼び出し元行番号（自動付与）</param>
        public void Info(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            WriteLog(LogLevelInfo, message, file, line);
        }

        /// <summary>
        /// 警告ログ(WARN)を出力します。
        /// </summary>
        /// <param name="message">メッセージ内容</param>
        /// <param name="file">呼び出し元ファイル（自動付与）</param>
        /// <param name="line">呼び出し元行番号（自動付与）</param>
        public void Warning(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            WriteLog(LogLevelWarn, message, file, line);
        }

        /// <summary>
        /// エラーログ(ERROR)を出力します。
        /// </summary>
        /// <param name="message">メッセージ内容</param>
        /// <param name="file">呼び出し元ファイル（自動付与）</param>
        /// <param name="line">呼び出し元行番号（自動付与）</param>
        public void Error(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            WriteLog(LogLevelError, message, file, line);
        }

        /// <summary>
        /// デバッグログ(DEBUG)を出力します。
        /// </summary>
        /// <param name="message">メッセージ内容</param>
        /// <param name="file">呼び出し元ファイル（自動付与）</param>
        /// <param name="line">呼び出し元行番号（自動付与）</param>
        public void Debug(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            WriteLog(LogLevelDebug, message, file, line);
        }
    }
}
