using System;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// シーンクラスのテンプレートです。
    /// 1. このファイルをコピーして新しい名前（例: SceneTitle.cs）に変更してください。
    /// 2. 下記のクラス名 "TonSceneTemplate" を新しいファイル名に合わせて変更してください。
    /// 3. Ton.Scene.Change(new SceneTitle()); のように呼び出して使用します。
    /// </summary>
    public class SampleScene03 : IScene
    {
        // Aボタン押下時間
        float fHoldAButton = 0.0f;

        // マウス座標
        Vector2 mousePosition;

        /// <summary>
        /// シーン開始時に一度だけ呼ばれます。リソースのロードや変数の初期化を行います。
        /// </summary>
        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // TODO: ここに初期化処理を記述
            // 例: Ton.Gra.LoadTexture("image/player", "player");

            // BGMロード
            Ton.Sound.LoadBGM("sample_assets/sound/bgm/tutorial", "tutorial");
            Ton.Sound.LoadBGM("sample_assets/sound/bgm/tutorial2", "tutorial2");
            Ton.Sound.LoadBGM("sample_assets/sound/bgm/tutorial3", "tutorial3");
            Ton.Sound.LoadBGM("sample_assets/sound/bgm/tutorial4", "tutorial4");

            // SEロード
            Ton.Sound.LoadSound("sample_assets/sound/se/coin", "coin");
            Ton.Sound.LoadSound("sample_assets/sound/se/jump", "jump");
            Ton.Sound.LoadSound("sample_assets/sound/se/lose", "lose");
            Ton.Sound.LoadSound("sample_assets/sound/se/zap", "zap");

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

            // サウンドアンロード
            Ton.Sound.UnloadAll();

            // 終了処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminated.");
        }

        /// <summary>
        /// 毎フレーム更新処理が呼ばれます。ロジックを記述します。
        /// </summary>
        /// <param name="gameTime">時間情報</param>
        public void Update(GameTime gameTime)
        {
            // TODO: ここに更新処理を記述

            // Aボタン押下時間更新
            if (Ton.Input.IsPressed("A"))
            {
                fHoldAButton += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (fHoldAButton >= 1.0f)
                {
                    // Aボタンを1秒以上押していたら次のシーンへ移動(フェードアウト・フェードイン時間を指定可能)
                    Ton.Scene.Change(new SampleScene04(), 0.5f, 0.5f, Color.Lime);
                }
            }
            else
            {
                fHoldAButton = 0.0f;
            }

            // ボタン押したかチェック
            Rectangle[] rectPlay = new Rectangle[]
            {
                new Rectangle(20, 70, 64, 64),
                new Rectangle(20, 150, 64, 64),
                new Rectangle(20, 230, 64, 64),
                new Rectangle(20, 310, 64, 64)
            };
            Rectangle[] rectPause = new Rectangle[]
            {
                new Rectangle(84, 70, 64, 64),
                new Rectangle(84, 150, 64, 64),
                new Rectangle(84, 230, 64, 64),
                new Rectangle(84, 310, 64, 64)
            };
            Rectangle[] rectStop = new Rectangle[]
            {
                new Rectangle(148, 70, 64, 64),
                new Rectangle(148, 150, 64, 64),
                new Rectangle(148, 230, 64, 64),
                new Rectangle(148, 310, 64, 64)
            };
            Rectangle[] rectSE = new Rectangle[]
            {
                new Rectangle(350, 70, 64, 64),
                new Rectangle(350, 150, 64, 64),
                new Rectangle(350, 230, 64, 64),
                new Rectangle(350, 310, 64, 64)
            };
            String[] strBGM = new string[]
            {
                "tutorial", "tutorial2","tutorial3","tutorial4"
            };
            String[] strSE = new string[]
            {
                "coin", "jump","lose","zap"
            };

            // マウス座標取得
            mousePosition = Ton.Input.GetMousePosition();
            Rectangle mouseRect = new Rectangle((int)(mousePosition.X - 2.0f), (int)(mousePosition.Y - 2.0f), 4, 4);
            
            if(Ton.Input.IsMouseJustPressed(MouseButton.Left))
            {
                // マウス判定
                for (int n = 0; n < 4; n++)
                {
                    if(Ton.Math.HitCheckRect(mouseRect, rectPlay[n]))
                    {
                        // BGM再生
                        Ton.Sound.PlayBGM(strBGM[n], 3.0f);
                    }else if(Ton.Math.HitCheckRect(mouseRect, rectPause[n]))
                    {
                        // ポーズ
                        Ton.Sound.StopBGM(0.5f, true);
                    }else if (Ton.Math.HitCheckRect(mouseRect, rectStop[n]))
                    {
                        // 停止
                        Ton.Sound.StopBGM(0.5f, false);
                    }else if(Ton.Math.HitCheckRect(mouseRect, rectSE[n]))
                    {
                        // SE再生
                        Ton.Sound.PlaySE(strSE[n]);
                    }
                }
            }

        }

        /// <summary>
        /// 毎フレーム描画処理が呼ばれます。描画コードを記述します。
        /// </summary>
        public void Draw()
        {
            // TODO: ここに描画処理を記述

            // 画面クリア
            Ton.Gra.Clear(Color.DarkBlue);

            // テキストを表示
            Ton.Gra.DrawText("BGM Test", 10, 10);
            Ton.Gra.DrawText("SE Test", 300, 10);

            // ボタンを表示
            for (int n = 0; n < 4; n++)
            {
                // 番超
                Ton.Gra.DrawText((n+1).ToString(), 247, 82 + (n * 80), 0.8f);

                // BGMの各ボタン
                Ton.Gra.Draw("coin_animation", 20, 70 + (n * 80), 192, 256, 192, 64);

                // SEの各ボタン
                Ton.Gra.Draw("coin_animation", 350, 70 + (n * 80), 192, 256, 64, 64);
            }

            // マウスカーソルの描画
            Ton.Gra.Draw("coin_animation", (int)(mousePosition.X - 31.0f), (int)(mousePosition.Y - 32.0f), 0, 320, 64, 64);

            // BGM再生位置を取得
            TimeSpan tsFrom = Ton.Sound.GetBGMPosition();
            TimeSpan tsTo = Ton.Sound.GetBGMLength();
            String strBGMPos = String.Format("BGM Position: {0:D2}:{1:D2} / {2:D2}:{3:D2}"
                , tsFrom.Minutes, tsFrom.Seconds
                , tsTo.Minutes, tsTo.Seconds);
            Ton.Gra.DrawText(strBGMPos, 10, Ton.Game.VirtualHeight - 90, 0.6f);

            // マウス座標を表示
            String str = String.Format("Mouse (X {0:F1}, Y {1:F1})"
                , mousePosition.X
                , mousePosition.Y);
            Ton.Gra.DrawText(str, 10, Ton.Game.VirtualHeight - 60, 0.6f);

            // FPS情報を表示
            String str2 = String.Format("FPS: (Update {0}, Draw {1}) FullScreen ({2}) Virtual Resolution ({3},{4})"
                , Math.Round(Ton.Game.UpdateFPS, MidpointRounding.AwayFromZero)
                , Math.Round(Ton.Game.DrawFPS, MidpointRounding.AwayFromZero)
                , Ton.Game.IsFullScreen, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight);
            Ton.Gra.DrawText(str2, 10, Ton.Game.VirtualHeight - 30, 0.6f);

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(fHoldAButton * 400.0f), 160, 0.6f + (fHoldAButton));
        }
    }
}
