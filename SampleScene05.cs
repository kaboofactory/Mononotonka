using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mononotonka
{
    /// <summary>
    /// TonMessageの動作確認用シーン
    /// </summary>
    public class SampleScene05 : IScene
    {
        private string strEvent = "";

        // Aボタン押下時間
        float fHoldAButton = 0.0f;

        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // TonMessage初期化
            Ton.Msg.Initialize();

            // ウィンドウ位置を明示的に設定 (画面下部)
            Ton.Msg.SetWindowRect(40, Ton.Game.VirtualHeight - 300, Ton.Game.VirtualWidth - 80, 260);

            // メッセージウィンドウのフォントサイズを設定
            Ton.Msg.SetTextStyle(0.7f);

            // 入力待ちアイコンの設定テスト
            Ton.Msg.SetInputWaitingIcon("heart");

            // スクリプト読み込み (ラベル指定)
            Ton.Msg.LoadScript("sample_assets/script/ton_test_script.txt", "TEST_PATTERN_1");

            // 初期化処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Initialized.");
        }

        /// <summary>
        /// シーン終了時（遷移時）に呼ばれます。リソースの破棄などを行います。
        /// </summary>
        public void Terminate()
        {
            // 終了処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminating.");

            // TODO: 必要であれば終了処理を記述
            // UnloadTextureなどはTonGraphicsが自動管理する場合が多いですが、
            // 明示的に解放したい場合はここに記述します。

            // 一応クリア
            Ton.Msg.Clear();

            // 終了処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminated.");
        }

        public void Update(GameTime gameTime)
        {
            // TonMessage更新
            // BボタンまたはAボタンでメッセージ進行
            Ton.Msg.Update(gameTime, Ton.Input.IsJustPressed("B"));

            // Aボタン押下時間更新
            if (Ton.Input.IsPressed("A"))
            {
                fHoldAButton += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (fHoldAButton >= 1.0f)
                {
                    // Aボタンを1秒以上押していたら次のシーンへ移動(フェードアウト・フェードイン時間を指定可能)
                    Ton.Scene.Change(new SampleScene06(), 0.5f, 0.5f, Color.Chocolate);
                }
            }
            else
            {
                fHoldAButton = 0.0f;
            }

            // イベントIDの取得テスト
            string eventId = Ton.Msg.GetEvent();
            if (eventId != null)
            {
                strEvent = "イベント受信:" + eventId;
            }
        }

        public void Draw()
        {
            Ton.Gra.Clear(Color.DarkSlateBlue);

            // シーンの説明
            Ton.Gra.DrawText("Fifth Scene: TonMessage Test", 20, 20, Color.White, 0.7f);
            Ton.Gra.DrawText("Press B Button Next Message.", 20, 50, Color.LightGray, 0.7f);
            
            // メッセージの背景ウィンドウを描画
            Ton.Gra.FillRoundedRect("9-patch", 20, Ton.Game.VirtualHeight - 320
                , Ton.Game.VirtualWidth - 40, 300, 16, 16);

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(fHoldAButton * 400.0f), 160, 0.6f + (fHoldAButton));

            // イベント表示
            if (strEvent.Length > 0)
            {
                Ton.Gra.DrawText(strEvent, 20, Ton.Game.VirtualHeight - 430, Color.Orange, 2.0f);
            }

            // メッセージ描画
            Ton.Msg.Draw();
        }
    }
}
