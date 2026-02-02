using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// 最初のシーン
    /// </summary>
    public class SampleScene01 : IScene
    {
        private class myInputTimeCount
        {
            public float fElapsedTime;
            public bool isJust;
            public bool isPress;
        }

        // 押された瞬間を保持
        private Dictionary<string, myInputTimeCount> dictTimeCount = new Dictionary<string, myInputTimeCount>(StringComparer.OrdinalIgnoreCase);

        // アニメーション状態保持用
        private TonAnimState anim = new TonAnimState
        {
            x1 = 0,
            y1 = 0,
            width = 64,
            height = 64,
            FrameCount = 6,
            FrameDuration = 125,
            IsLoop = true
        };

        // アニメーション状態保持用2
        private TonAnimState anim2 = new TonAnimState
        {
            x1 = 0,
            y1 = 64,
            width = 64,
            height = 64,
            FrameCount = 6,
            FrameDuration = 180,
            IsLoop = false
        };

        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // 永続的に使用しないリソースはここでロード(LoadTexture()など)してください

            // 初期化処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Initialized.");
        }

        public void Terminate()
        {
            // 終了処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminating.");

            // 永続的に使用しないリソースはここでアンロード(UnloadTexture()など)してください(自動解放を待ちたくない場合)

            // タイムカウンタ破棄
            dictTimeCount.Clear();
            dictTimeCount = null;

            // 終了処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminated.");
        }

        public void Update(GameTime gameTime)
        {
            // ゲームパッドの状態取得
            UpdateInput("Left", gameTime);
            UpdateInput("Right", gameTime);
            UpdateInput("Up", gameTime);
            UpdateInput("Down", gameTime);
            UpdateInput("A", gameTime);
            UpdateInput("B", gameTime);
            UpdateInput("X", gameTime);
            UpdateInput("Y", gameTime);
            UpdateInput("R", gameTime);
            UpdateInput("L", gameTime);

            // アニメーション状態更新
            anim.Update(gameTime);

            // アニメーション状態更新2
            anim2.Update(gameTime);
            if(anim2.IsFinished && anim2.TimeAfterFinished >= 1.0f)
            {
                // アニメーション終了から1秒経過したら頭から再生
                anim2.Reset();
            }

            // Aボタン押下時間更新
            if(Ton.Input.GetPressedDuration("A") > 1.0f)
            {
                // Aボタンを1秒以上押していたら次のシーンへ移動(フェードアウト・フェードイン時間を指定可能)
                Ton.Scene.Change(new SampleScene02(), 0.5f, 0.2f, Color.White);

                // 押下時間クリア(次のシーンに影響を与えないように)
                Ton.Input.ClearPressedDuration("A");
            }
        }

        public void Draw()
        {
            // 描画処理

            // 背景を表示する
            Ton.Gra.DrawBackground("landscape");

            // テキストを表示(ユーザが登録したフォントを使用)
            Ton.Gra.DrawText("Font Test", 10, 10, 0.7f);
            Ton.Gra.DrawText("日本語にほんごニホンゴ*nihongo123", 10, 60, 0.8f);
            Ton.Gra.DrawText("日本語にほんごニホンゴ*nihongo123", 10, 100, "hanazome", 0.8f);
            Ton.Gra.DrawText("日本語にほんごニホンゴ*nihongo123", 10, 140, "rounded", 0.8f);
            Ton.Gra.DrawText("日本語にほんごニホンゴ*nihongo123", 10, 180, "tanuki", 0.8f);
            Ton.Gra.DrawText("日本語にほんごニホンゴ*nihongo123", 10, 220, "tegaki", 0.8f);

            ////////////////////////
            // ゲームパッド処理
            ////////////////////////

            // テキスト
            Ton.Gra.DrawText("Gamepad Test", 10, 280, 0.7f);
            
            // ゲームパッド描画
            Ton.Gra.Draw("controller", 10, 310, 0, 0, 256, 192);

            // ボタン描画
            DrawButton("Left", new Point(4, 92), new Point(40, 40));
            DrawButton("Right", new Point(84, 92), new Point(40, 40));
            DrawButton("Up", new Point(44, 52), new Point(40, 40));
            DrawButton("Down", new Point(44, 132), new Point(40, 40));
            DrawButton("A", new Point(172, 132), new Point(40, 40));
            DrawButton("B", new Point(212, 92), new Point(40, 40));
            DrawButton("X", new Point(132, 92), new Point(40, 40));
            DrawButton("Y", new Point(172, 52), new Point(40, 40));
            DrawButton("L", new Point(8, 20), new Point(56, 24));
            DrawButton("R", new Point(196, 20), new Point(56, 24));

            ////////////////////////
            // 描画テスト
            ////////////////////////

            // テキスト
            Ton.Gra.DrawText("Draw Test", 320, 280, 0.7f);

            // 通常描画
            Ton.Gra.Draw("coin_animation", 320, 330, 0, 0, 64, 64);

            // 加算合成
            Ton.Gra.SetBlendState(TonBlendState.Additive);
            Ton.Gra.Draw("coin_animation", 390, 330, 0, 256, 64, 64);

            // 不透明合成
            Ton.Gra.SetBlendState(TonBlendState.NonPremultiplied);
            Ton.Gra.Draw("coin_animation", 460, 330, 0, 64, 64, 64);
            
            // ブレンドモードを戻す
            Ton.Gra.SetBlendState(TonBlendState.AlphaBlend);

            // 左右反転
            TonDrawParam param = new TonDrawParam();
            param.FlipH = true;
            Ton.Gra.Draw("coin_animation", 530, 330, 128, 256, 64, 64, param);

            // 上下反転
            param.FlipH = false;
            param.FlipV = true;
            Ton.Gra.Draw("coin_animation", 600, 330, 128, 256, 64, 64, param);

            // 上下左右反転
            param.FlipH = true;
            Ton.Gra.Draw("coin_animation", 670, 330, 128, 256, 64, 64, param);

            // アルファ値の計算(Ton.Game.TotalGameTimeはDraw()内でも使用できる経過時間変数です)
            float fAlpha = (float)((Math.Sin(Ton.Game.TotalGameTime.TotalSeconds * 4.0) * 0.5) + 0.5);

            // アルファ値変更
            param.FlipH = false;
            param.Alpha = fAlpha;
            Ton.Gra.Draw("coin_animation", 730, 330, 192, 192, 64, 64, param);

            // カラー変更
            param.Alpha = 1.0f;
            param.Color = new Color(fAlpha, fAlpha, 1.0f);
            Ton.Gra.Draw("coin_animation", 800, 330, 192, 192, 64, 64, param);

            // 拡大表示
            TonDrawParamEx paramex = new TonDrawParamEx();
            paramex.ScaleX = (float)((Math.Sin(Ton.Game.TotalGameTime.TotalSeconds * 4.0) * 1.0) + 1.5);
            paramex.ScaleY = (float)((Math.Sin(Ton.Game.TotalGameTime.TotalSeconds * 6.0) * 1.0) + 1.5);
            Ton.Gra.DrawEx("coin_animation", 400.0f, 480.0f, 0, 0, 64, 64, paramex);

            // 拡大回転表示
            paramex.Angle = (float)(Ton.Game.TotalGameTime.TotalSeconds * 4.0);
            paramex.FlipH = true;
            Ton.Gra.DrawEx("coin_animation", 550.0f, 480.0f, 0, 0, 64, 64, paramex);

            // 拡大回転アルファ値変更
            paramex.Alpha = fAlpha;
            paramex.Angle = -paramex.Angle;
            Ton.Gra.DrawEx("coin_animation", 700.0f, 480.0f, 0, 0, 64, 64, paramex);

            // 拡大回転カラー変更
            paramex.Alpha = 1.0f;
            paramex.Angle = -paramex.Angle;
            paramex.Color = Color.Gray;
            Ton.Gra.DrawEx("coin_animation", 850.0f, 480.0f, 0, 0, 64, 64, paramex);

            // アニメーション(動きの更新はUpdate()でTonAnimState.Update()を実行)
            Ton.Gra.DrawAnim("coin_animation", 320, 560, anim);
            Ton.Gra.DrawAnim("coin_animation", 400, 560, anim2);

            // アニメーション(TonAnimState.Update()不要で簡易ループアニメする場合はCreateLoop()を使用)
            TonAnimState anim3 = TonAnimState.CreateLoop(0, 0, 64, 64, 6, 50, Ton.Game.TotalGameTime.TotalSeconds);
            Ton.Gra.DrawAnim("coin_animation", 480, 560, anim3);

            // アニメーション拡大回転半透明表示
            paramex.Angle = (float)(Ton.Game.TotalGameTime.TotalSeconds * 2.0);
            paramex.FlipH = false;
            paramex.Alpha = fAlpha;
            paramex.Color = Color.White;
            Ton.Gra.DrawAnimEx("coin_animation", 640.0f, 600.0f, anim3, paramex);

            // FPS情報を表示
            String str = String.Format("FPS: (Update {0}, Draw {1}) FullScreen ({2}) Virtual Resolution ({3},{4})"
                , Math.Round(Ton.Game.UpdateFPS, MidpointRounding.AwayFromZero)
                , Math.Round(Ton.Game.DrawFPS, MidpointRounding.AwayFromZero)
                , Ton.Game.IsFullScreen, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight);
            Ton.Gra.DrawText(str, 10, Ton.Game.VirtualHeight - 30, 0.6f);

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(Ton.Input.GetPressedDuration("A") * 400.0f), 160, 0.6f + (float)Ton.Input.GetPressedDuration("A"));
        }

        // ボタンの描画
        private void DrawButton(string Key, Point pos, Point size)
        {
            int just = 256;
            int pressed = 512;

            if (dictTimeCount.TryGetValue(Key, out var c))
            {
                if (c.fElapsedTime < 0.12f)
                {
                    // 押した瞬間、離した瞬間、0.12秒だけ青くなる
                    Ton.Gra.Draw("controller", 10 + pos.X, 310 + pos.Y, pos.X + just, pos.Y, size.X, size.Y);
                }
                else if (c.isPress)
                {
                    // 押した状態は赤くなる
                    Ton.Gra.Draw("controller", 10 + pos.X, 310 + pos.Y, pos.X + pressed, pos.Y, size.X, size.Y);
                }
            }
        }

        // ボタンの状態取得・更新
        private void UpdateInput(string Key, GameTime gameTime)
        {
            myInputTimeCount c;

            // 辞書に存在しなければ追加
            if (!dictTimeCount.ContainsKey(Key))
            {
                myInputTimeCount add;
                add = new myInputTimeCount();
                add.fElapsedTime = 999.0f;
                dictTimeCount[Key] = new myInputTimeCount();
            }

            // 辞書から取得
            c = dictTimeCount[Key];

            if (Ton.Input.IsJustPressed(Key) || Ton.Input.IsJustReleased(Key))
            {
                // ボタンが押された瞬間、離された瞬間
                c.isJust = true;
                c.isPress = false;
                c.fElapsedTime = 0.0f;
            }
            else if (Ton.Input.IsPressed(Key))
            {
                // ボタンが押されている状態
                c.isJust = false;
                c.isPress = true;
            }else
            {
                // ボタンが押されていない状態
                c.isJust = false;
                c.isPress = false;
            }

            // 経過時間更新
            c.fElapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // 辞書に再登録
            dictTimeCount[Key] = c;
        }
    }
}
