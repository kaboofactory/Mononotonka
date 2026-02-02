using System;
using System.IO;
using System.Text.Json;

namespace Mononotonka
{
    /// <summary>
    /// ストレージ管理クラスです。
    /// データのセーブ・ロード（JSON形式）を担当します。
    /// </summary>
    public class TonStorage
    {
        private string _saveDir;

        /// <summary>
        /// コンストラクタ。セーブデータ保存ディレクトリを確保します。
        /// </summary>
        public TonStorage()
        {
            try
            {
                _saveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "save");
                if (!Directory.Exists(_saveDir))
                {
                    Directory.CreateDirectory(_saveDir);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TonStorage Init Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定したデータをJSON形式でファイルに保存します。
        /// </summary>
        /// <typeparam name="T">保存するデータの型</typeparam>
        /// <param name="fileName">保存ファイル名</param>
        /// <param name="data">保存するデータオブジェクト</param>
        public void Save<T>(string fileName, T data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                string path = Path.Combine(_saveDir, fileName);
                File.WriteAllText(path, json);
                Ton.Log.Info($"Saved data to {fileName}");
            }
            catch (Exception ex)
            {
                Ton.Log.Error($"Failed to save {fileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// 指定したファイルからデータを読み込みます。
        /// </summary>
        /// <typeparam name="T">読み込むデータの型</typeparam>
        /// <param name="fileName">ファイル名</param>
        /// <returns>読み込んだデータオブジェクト。失敗時はdefault値を返します。</returns>
        public T Load<T>(string fileName)
        {
            try
            {
                string path = Path.Combine(_saveDir, fileName);
                if (!File.Exists(path)) return default;

                string json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<T>(json);
                Ton.Log.Info($"Loaded data from {fileName}");
                return data;
            }
            catch (Exception ex)
            {
                Ton.Log.Error($"Failed to load {fileName}: {ex.Message}");
                        return default;
            }
        }

        /// <summary>
        /// 指定したファイルが存在するか確認します。
        /// </summary>
        public bool Exists(string fileName)
        {
            string path = Path.Combine(_saveDir, fileName);
            return File.Exists(path);
        }
    }
}
