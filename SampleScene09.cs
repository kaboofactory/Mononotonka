using System;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// TonSaveLoadMenuのテスト用シーン。
    /// Ton.Data と連携した簡略化APIのデモ。
    /// </summary>
    public class SampleScene09 : IScene
    {
        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // ロード完了時の挙動を定義
            Ton.SaveLoadMenu.OnLoaded = () =>
            {
                Ton.Log.Info("NineScene: Load Completed. Scene Transition Logic goes here.");
                // 例: 
                // if (Ton.Data.CurrentSceneName == "NineScene") { ... }
            };

            // 初期化処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Initialized.");
        }

        public void Terminate()
        {
            // 終了処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminating.");

            // 終了処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminated.");
        }

        public void Update(GameTime gameTime)
        {
            // ステータス操作テスト
            if (Ton.Input.IsJustPressed("L"))
            {
                Ton.Data.Level++;
                Ton.Data.HP += 10;
                Ton.Data.SetFlag("KeyGet");
                Ton.Log.Info($"[Stats Updated] HP:{Ton.Data.HP} Level:{Ton.Data.Level}");
            }
            if (Ton.Input.IsJustPressed("R"))
            {
                Ton.Data.Level = 1;
                Ton.Data.HP = 100;
                Ton.Data.RemoveFlag("KeyGet");
                Ton.Log.Info($"[Stats Reset] HP:{Ton.Data.HP} Level:{Ton.Data.Level}");
            }

            // メニュー呼び出し（引数不要）
            if (Ton.Input.IsJustPressed("A"))
            {
                Ton.SaveLoadMenu.Open(TonSaveLoadMode.SaveOnly);
            }
            if (Ton.Input.IsJustPressed("B"))
            {
                Ton.SaveLoadMenu.Open(TonSaveLoadMode.LoadOnly);
            }
            if (Ton.Input.IsJustPressed("X"))
            {
                Ton.SaveLoadMenu.Open(TonSaveLoadMode.BothDefaultSave);
            }

            // Yボタン押下時間更新
            if (Ton.Input.GetPressedDuration("Y") > 1.0f)
            {
                // Aボタンを1秒以上押していたら次のシーンへ移動(フェードアウト・フェードイン時間を指定可能)
                Ton.Scene.Change(new SampleScene10(), 0.5f, 0.2f, Color.White);

                // 押下時間クリア(次のシーンに影響を与えないように)
                Ton.Input.ClearPressedDuration("Y");
            }
        }

        public void Draw()
        {
            Ton.Gra.FillRect(0, 0, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight, Color.DarkSlateBlue);

            int y = 50;
            Ton.Gra.DrawText("- Simple Save/Load Test -", 50, y, Color.White);
            y += 50;
            
            // Current Status
            Ton.Gra.DrawText($"Current HP: {Ton.Data.HP}", 100, y, Color.Yellow, 0.7f); y+=30;
            Ton.Gra.DrawText($"Current Level: {Ton.Data.Level}", 100, y, Color.Yellow, 0.7f); y+=30;
            Ton.Gra.DrawText($"Flag [KeyGet]: {Ton.Data.CheckFlag("KeyGet")}", 100, y, Color.Yellow, 0.7f); y+=40;

            Ton.Gra.DrawText("[L] Update Stats", 100, y, Color.Cyan, 0.7f); y+=30;
            Ton.Gra.DrawText("[R] Reset Stats", 100, y, Color.Cyan, 0.7f); y+=40;

            Ton.Gra.DrawText("[A] Save Only", 100, y, Color.White, 0.7f); y+=30;
            Ton.Gra.DrawText("[B] Load Only", 100, y, Color.White, 0.7f); y+=30;
            Ton.Gra.DrawText("[X] Both (Save)", 100, y, Color.White, 0.7f); y+=30;

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the Y button (Next Scene)", 700 - (int)(Ton.Input.GetPressedDuration("Y") * 400.0f), 160, 0.6f + (float)Ton.Input.GetPressedDuration("Y"));
        }
    }
}
