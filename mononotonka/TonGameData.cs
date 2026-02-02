using System;
using System.Collections.Generic;
using System.Text.Json.Serialization; // System.Text.Json用
// Newtonsoft.Jsonを使う場合は using Newtonsoft.Json;

namespace Mononotonka
{
    /// <summary>
    /// ゲームデータ管理クラス（兼セーブデータ）。
    /// このクラスのインスタンスがそのままJSONとして保存されます。
    /// 保存したくない変数は [JsonIgnore] 属性をつけてください。
    /// </summary>
    [Serializable]
    public class TonGameData
    {
        // ----------------------------------------------------
        // 保存されるデータ
        // ----------------------------------------------------
        public int HP { get; set; } = 100;
        public int MaxHP { get; set; } = 100;
        public int Level { get; set; } = 1;
        public int Exp { get; set; } = 0;
        public int Money { get; set; } = 0;
        
        // プレイヤー座標（Vector2などの構造体を直接シリアライズできない場合があるため、float推奨）
        public float PlayerX { get; set; }
        public float PlayerY { get; set; }
        public string CurrentSceneName { get; set; }

        // フラグ管理
        public HashSet<string> Flags { get; set; } = new HashSet<string>();
        // 汎用変数
        public Dictionary<string, int> Vars { get; set; } = new Dictionary<string, int>();

        // ----------------------------------------------------
        // 保存されないデータ (Runtime Only)
        // [JsonIgnore] をつけることで保存対象から除外されます
        // ----------------------------------------------------
        /// <summary>
        /// 一時的な計算用キャッシュなど（保存対象外）
        /// </summary>
        [JsonIgnore]
        public object TempCacheData { get; set; }

        // ----------------------------------------------------
        // ヘルパーメソッド
        // ----------------------------------------------------

        public void SetFlag(string flagName) => Flags.Add(flagName);
        public void RemoveFlag(string flagName) => Flags.Remove(flagName);
        public bool CheckFlag(string flagName) => Flags.Contains(flagName);

        public void SetVar(string name, int val) => Vars[name] = val;
        public int GetVar(string name) => Vars.ContainsKey(name) ? Vars[name] : 0;
        
        /// <summary>
        /// セーブ実行前に必要な情報を更新するメソッド。
        /// TonSaveLoadMenuから保存直前に呼ばれます。
        /// </summary>
        public void BeforeSave()
        {
        }

        /// <summary>
        /// ロード完了直後に呼ばれるメソッド。
        /// 復元したデータをゲーム内に反映（プレイヤー移動など）させます。
        /// </summary>
        public void AfterLoad()
        {
        }
    }
}
