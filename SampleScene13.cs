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
    public class SampleScene13 : IScene
    {
        int cursorX = 0;
        int cursorY = 0;
        string _State = "Initialized";

        /// <summary>
        /// シーン開始時に一度だけ呼ばれます。リソースのロードや変数の初期化を行います。
        /// </summary>
        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // TODO: ここに初期化処理を記述
            Ton.Sound.LoadSound("sample_assets/sound/se/group1-1", "group1-1", "Group1");
            Ton.Sound.LoadSound("sample_assets/sound/se/group1-2", "group1-2", "Group1");
            Ton.Sound.LoadSound("sample_assets/sound/se/group1-3", "group1-3", "Group1");
            Ton.Sound.LoadSound("sample_assets/sound/se/group2-1", "group2-1", "Group2");
            Ton.Sound.LoadSound("sample_assets/sound/se/group2-2", "group2-2", "Group2");
            Ton.Sound.LoadSound("sample_assets/sound/se/group2-3", "group2-3", "Group2");
            Ton.Sound.LoadSound("sample_assets/sound/se/group3-1", "group3-1", "Group3");
            Ton.Sound.LoadSound("sample_assets/sound/se/group3-2", "group3-2", "Group3");
            Ton.Sound.LoadSound("sample_assets/sound/se/group3-3", "group3-3", "Group3");

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
                Ton.Scene.Change(new SampleScene14(), 0.5f, 0.2f, Color.Gold);
            }

            if(Ton.Input.IsJustPressed("Right"))
            {
                cursorX++;
                if(cursorX >= 3)
                {
                    cursorX = 0;
                }
            }
            if (Ton.Input.IsJustPressed("Left"))
            {
                cursorX--;
                if (cursorX < 0)
                {
                    cursorX = 2;
                }
            }
            if (Ton.Input.IsJustPressed("Down"))
            {
                cursorY++;
                if (cursorY >= 3)
                {
                    cursorY = 0;
                }
            }
            if (Ton.Input.IsJustPressed("Up"))
            {
                cursorY--;
                if (cursorY < 0)
                {
                    cursorY = 2;
                }
            }

            if(Ton.Input.IsJustPressed("B"))
            {
                // SE再生
                Ton.Sound.PlaySE(String.Format("Group{0}-{1}", cursorX + 1, cursorY + 1));
            }
            if (Ton.Input.IsJustPressed("X"))
            {
                // SE強制時間経過
                Ton.Sound.DebugForceExpireCache(String.Format("Group{0}", cursorX + 1));
            }
        }

        /// <summary>
        /// 毎フレーム描画処理が呼ばれます。描画コードを記述します。
        /// </summary>
        public void Draw()
        {
            Ton.Gra.DrawText("Sound Resource Management Test", 10, 10, 0.7f);
            Ton.Gra.DrawText("[B] Play SE", 10, 50, 0.7f);
            Ton.Gra.DrawText("[X] Release Group", 10, 80, 0.7f);
            Ton.Gra.DrawText("State: " + _State, 10, 170, 0.7f);

            // グループごとに描画
            for(int x = 0;x < 3; x++)
            {
                for(int y = 0; y < 3; y++)
                {
                    if(cursorX == x && cursorY == y)
                    {
                        TonDrawParamEx paramex = new TonDrawParamEx();
                        paramex.ScaleX = 1.5f;
                        paramex.ScaleY = 1.5f;
                        paramex.Angle = (float)(Ton.Game.TotalGameTime.TotalSeconds);
                        Ton.Gra.DrawEx(String.Format("Group{0}-{1}", x + 1, y + 1), (float)(82 + (x * 100)), (float)(282 + (y * 100)), 0, 0, 64, 64, paramex);
                    }
                    else
                    {
                        Ton.Gra.Draw(String.Format("Group{0}-{1}", x + 1, y + 1), 50 + (x * 100), 250 + (y * 100));
                    }
                }
            }

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(Ton.Input.GetPressedDuration("A") * 400.0f), 160, 0.6f + (float)Ton.Input.GetPressedDuration("A"));
        }
    }
}
