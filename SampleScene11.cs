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
    public class SampleScene11 : IScene
    {
        /// <summary>
        /// シーン開始時に一度だけ呼ばれます。リソースのロードや変数の初期化を行います。
        /// </summary>
        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // TODO: ここに初期化処理を記述
            // 例: Ton.Gra.LoadTexture("image/player", "player");

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

            if (Ton.Input.GetPressedDuration("A") > 1.0f)
            {
                Ton.Scene.Change(new SampleScene12(), 0.5f, 0.2f, Color.White);
            }

            if(Ton.Input.IsJustPressed("B"))
            {
                Ton.Input.Vibrate(3.0f, 0.5f);
            }
            if (Ton.Input.IsJustPressed("X"))
            {
                Ton.Gra.ShakeScreen(3.0f, 0.01f, 0.01f);
            }
        }

        /// <summary>
        /// 毎フレーム描画処理が呼ばれます。描画コードを記述します。
        /// </summary>
        public void Draw()
        {
            Ton.Gra.DrawText("Other Features", 10, 10, 0.7f);
            Ton.Gra.DrawText("[B] Vibration", 10, 50, 0.7f);
            Ton.Gra.DrawText("[X] Shaking screen", 10, 90, 0.7f);

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(Ton.Input.GetPressedDuration("A") * 400.0f), 160, 0.6f + (float)Ton.Input.GetPressedDuration("A"));
        }
    }
}
