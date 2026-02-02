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
    public class SampleScene02 : IScene
    {
        private float fMoveX = 0.0f;

        // Aボタン押下時間
        float fHoldAButton = 0.0f;
        
        /// <summary>
        /// シーン開始時に一度だけ呼ばれます。リソースのロードや変数の初期化を行います。
        /// </summary>
        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // TODO: ここに初期化処理を記述
            // 例: Ton.Gra.LoadTexture("image/player", "player");

            // 仮想画面1を作成
            Ton.Gra.CreateRenderTarget("VScreen", 320, 240);

            // 仮想画面2を作成
            Ton.Gra.CreateRenderTarget("VScreen2", 320, 240);

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

            // 仮想画面を破棄
            Ton.Gra.ReleaseRenderTarget("VScreen");
            Ton.Gra.ReleaseRenderTarget("VScreen2");

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

            // 移動処理
            fMoveX += 100.0f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if(fMoveX >= 360.0f)
            {
                fMoveX -= 400.0f;
            }

            // Aボタン押下時間更新
            if (Ton.Input.IsPressed("A"))
            {
                fHoldAButton += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (fHoldAButton >= 1.0f)
                {
                    // Aボタンを1秒以上押していたら次のシーンへ移動(フェードアウト・フェードイン時間を指定可能)
                    Ton.Scene.Change(new SampleScene03(), 0.5f, 0.5f, Color.Red);
                }
            }
            else
            {
                fHoldAButton = 0.0f;
            }
        }

        /// <summary>
        /// 毎フレーム描画処理が呼ばれます。描画コードを記述します。
        /// </summary>
        public void Draw()
        {
            // TODO: ここに描画処理を記述

            // 画面クリア
            Ton.Gra.Clear(Color.Green);

            // 仮想画面(2)に描画開始
            Ton.Gra.SetRenderTarget("VScreen2");

            // 仮想画面(2)の画面クリア
            Ton.Gra.Clear(Color.DarkRed);

            // 仮想画面(2)に描画する
            TonAnimState anim = TonAnimState.CreateLoop(0, 0, 80, 80, 2, 300, Ton.Game.TotalGameTime.TotalSeconds);
            Ton.Gra.DrawAnim("cat_animation", (int)fMoveX, 10, anim);
            Ton.Gra.DrawAnim("cat_animation", (int)fMoveX, 150, anim);

            // 仮想画面(1)に描画開始
            Ton.Gra.SetRenderTarget("VScreen");

            // 仮想画面(1)の画面クリア
            Ton.Gra.Clear(Color.CornflowerBlue);

            // 仮想画面(1)に描画する
            Ton.Gra.DrawAnim("cat_animation", (int)fMoveX, 10, anim);
            Ton.Gra.DrawAnim("cat_animation", (int)fMoveX, 150, anim);

            // 仮想画面(1)に仮想画面(2)を描画する
            TonDrawParamEx paramex = new TonDrawParamEx();
            paramex.Angle = (float)Ton.Game.TotalGameTime.TotalSeconds;
            paramex.ScaleX = 0.5f;
            paramex.ScaleY = 0.5f;
            Ton.Gra.DrawEx("VScreen2", 160.0f, 120.0f, 0, 0, Ton.Gra.GetTextureWidth("VScreen2"), Ton.Gra.GetTextureHeight("VScreen2"), paramex);

            // 仮想画面(1)への描画終了
            Ton.Gra.SetRenderTarget(null);

            // 画面への描画
            TonDrawParamEx paramex2 = new TonDrawParamEx();
            paramex2.ScaleX = (float)(2.0f + Math.Sin(Ton.Game.TotalGameTime.TotalSeconds));
            paramex2.ScaleY = paramex2.ScaleX;

            Ton.Gra.DrawEx("VScreen", 500.0f, 300.0f, 0, 0, Ton.Gra.GetTextureWidth("VScreen"), Ton.Gra.GetTextureHeight("VScreen"), paramex2);

            // テキスト
            Ton.Gra.DrawText("Render Target Test", 10, 10, 0.7f);

            // FPS情報を表示
            String str = String.Format("FPS: (Update {0}, Draw {1}) FullScreen ({2}) Virtual Resolution ({3},{4})"
                , Math.Round(Ton.Game.UpdateFPS, MidpointRounding.AwayFromZero)
                , Math.Round(Ton.Game.DrawFPS, MidpointRounding.AwayFromZero)
                , Ton.Game.IsFullScreen, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight);
            Ton.Gra.DrawText(str, 10, Ton.Game.VirtualHeight - 30, 0.6f);

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(fHoldAButton * 400.0f), 160, 0.6f + (fHoldAButton));
        }
    }
}
