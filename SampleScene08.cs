using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mononotonka;

namespace Mononotonka
{
    // テスト保存用データクラス
    public class TestSaveData
    {
        public int Counter { get; set; } = 0;
        public string Message { get; set; } = "Hello Storage";
        public float PlayerX { get; set; } = 100f;
    }

    /// <summary>
    /// TonStorage と TonConfigMenu のテスト用シーン
    /// </summary>
    public class SampleScene08 : IScene
    {
        private TestSaveData _data;
        private string _statusMessage = "Ready.";
        private const string SAVE_FILE_NAME = "test_data.json";
        private Random _random = new Random(DateTime.Now.Millisecond + (int)DateTime.Now.Ticks);

        // Rボタン押下時間
        float fHoldRButton = 0.0f;

        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            _data = new TestSaveData();
            // 初期化時にロードはしない（明示的にテストするため）

            // BGMロード(マスタボリュームテスト用)
            Ton.Sound.LoadBGM("sample_assets/sound/bgm/tutorial", "tutorial");
            Ton.Sound.PlayBGM("tutorial");

            // 初期化処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Initialized.");
        }

        public void Terminate()
        {
            // 終了処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminating.");

            // サウンドアンロード
            Ton.Sound.UnloadAll();

            // 終了処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminated.");
        }

        public void Update(GameTime gameTime)
        {
            // メニューオープン
            if (Ton.Input.IsJustPressed("B"))
            {
                Ton.ConfigMenu.Open();
                return;
            }

            // Rボタン押下時間更新
            if (Ton.Input.IsPressed("R"))
            {
                fHoldRButton += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (fHoldRButton >= 1.0f)
                {
                    // Rボタンを1秒以上押していたら次のシーンへ移動(フェードアウト・フェードイン時間を指定可能)
                    Ton.Scene.Change(new SampleScene09(), 0.5f, 0.5f, Color.Red);
                }
            }
            else
            {
                fHoldRButton = 0.0f;
            }

            // データ操作
            if (Ton.Input.IsJustPressed("A"))
            {
                _data.Counter++;
                _data.Message = "Hello:" + (_random.NextDouble() * 1000.0).ToString();
                _statusMessage = "Made Data.";
            }
            
            if (Ton.Input.IsPressed("Right")) _data.PlayerX += 10f;
            if (Ton.Input.IsPressed("Left")) _data.PlayerX -= 10f;

            // セーブ
            if (Ton.Input.IsJustPressed("X"))
            {
                Ton.Storage.Save(SAVE_FILE_NAME, _data);
                _statusMessage = "Data Saved.";
            }

            // ロード
            if (Ton.Input.IsJustPressed("Y"))
            {
                var loaded = Ton.Storage.Load<TestSaveData>(SAVE_FILE_NAME);
                if (loaded != null)
                {
                    _data = loaded;
                    _statusMessage = "Data Loaded.";
                }
                else
                {
                    _statusMessage = "Load Failed (No File?)";
                }
            }

        }

        public void Draw()
        {
            Ton.Gra.Clear(Color.DarkSlateBlue);

            // 情報表示
            int y = 50;
            Ton.Gra.DrawText("Eight Scene: Storage & Config Test", 50, y, 0.7f);
            y += 40;
            Ton.Gra.DrawText($"Status: {_statusMessage}", 50, y, Color.Yellow, 0.7f);
            y += 40;
            
            Ton.Gra.DrawText("--- Current Data ---", 50, y, Color.Cyan, 0.7f);
            y += 30;
            Ton.Gra.DrawText($"Counter: {_data.Counter}", 70, y, 0.7f);
            y += 30;
            Ton.Gra.DrawText($"PlayerX: {_data.PlayerX:F1}", 70, y, 0.7f);
            y += 30;
            Ton.Gra.DrawText($"Message: {_data.Message}", 70, y, 0.7f);
            y += 50;

            // 操作説明
            Ton.Gra.DrawText("[A] Inc Counter  [Left/Right] Change X", 50, 400, 0.7f);
            Ton.Gra.DrawText("[X] Save Data    [Y] Load Data", 50, 440, 0.7f);
            Ton.Gra.DrawText("[B] Open Config Menu", 50, 480, 0.7f);

            // コンフィグメニュー描画（最前面）
            if (Ton.ConfigMenu.IsOpen())
            {
                Ton.ConfigMenu.Draw();
            }

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the R button (Next Scene)", 700 - (int)(fHoldRButton * 400.0f), 600, 0.6f + (fHoldRButton));
        }
    }
}
