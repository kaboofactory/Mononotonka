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
    public class SampleScene12 : IScene
    {
        bool _ShowImage = true;
        string _State = "Initialized";

        /// <summary>
        /// シーン開始時に一度だけ呼ばれます。リソースのロードや変数の初期化を行います。
        /// </summary>
        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // TODO: ここに初期化処理を記述
            Ton.Gra.LoadTexture("sample_assets/image/group1-1", "group1-1", "Group1");
            Ton.Gra.LoadTexture("sample_assets/image/group1-2", "group1-2", "Group1");
            Ton.Gra.LoadTexture("sample_assets/image/group1-3", "group1-3", "Group1");
            Ton.Gra.LoadTexture("sample_assets/image/group2-1", "group2-1", "Group2");
            Ton.Gra.LoadTexture("sample_assets/image/group2-2", "group2-2", "Group2");
            Ton.Gra.LoadTexture("sample_assets/image/group2-3", "group2-3", "Group2");
            Ton.Gra.LoadTexture("sample_assets/image/group3-1", "group3-1", "Group3");
            Ton.Gra.LoadTexture("sample_assets/image/group3-2", "group3-2", "Group3");
            Ton.Gra.LoadTexture("sample_assets/image/group3-3", "group3-3", "Group3");

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
                Ton.Scene.Change(new SampleScene13(), 0.5f, 0.2f, Color.Gold);
            }

            if(Ton.Input.IsJustPressed("B"))
            {
                Ton.Gra.DebugForceExpireCache("Group1");
            }
            if (Ton.Input.IsJustPressed("X"))
            {
                Ton.Gra.DebugForceExpireCache("Group2");
            }
            if (Ton.Input.IsJustPressed("Y"))
            {
                Ton.Gra.DebugForceExpireCache("Group3");
            }
            if (Ton.Input.IsJustPressed("L"))
            {
                _ShowImage = !_ShowImage;
                if (_ShowImage)
                {
                    _State = "Images Shown";
                }
                else
                {
                    _State = "Images Hidden";
                }
            }

        }

        /// <summary>
        /// 毎フレーム描画処理が呼ばれます。描画コードを記述します。
        /// </summary>
        public void Draw()
        {
            Ton.Gra.DrawText("Image Resource Management Test", 10, 10, 0.7f);
            Ton.Gra.DrawText("[B] Release Group 1", 10, 50, 0.7f);
            Ton.Gra.DrawText("[X] Release Group 2", 10, 80, 0.7f);
            Ton.Gra.DrawText("[Y] Release Group 3", 10, 110, 0.7f);
            Ton.Gra.DrawText("[L] Hide/Show Image", 10, 140, 0.7f);
            Ton.Gra.DrawText("State: " + _State, 10, 170, 0.7f);

            // グループごとに描画
            if (_ShowImage)
            {
                Ton.Gra.Draw("Group1-1", 100, 250);
                Ton.Gra.Draw("Group1-2", 100, 350);
                Ton.Gra.Draw("Group1-3", 100, 450);
                Ton.Gra.Draw("Group2-1", 200, 250);
                Ton.Gra.Draw("Group2-2", 200, 350);
                Ton.Gra.Draw("Group2-3", 200, 450);
                Ton.Gra.Draw("Group3-1", 300, 250);
                Ton.Gra.Draw("Group3-2", 300, 350);
                Ton.Gra.Draw("Group3-3", 300, 450);
            }

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(Ton.Input.GetPressedDuration("A") * 400.0f), 160, 0.6f + (float)Ton.Input.GetPressedDuration("A"));
        }
    }
}
