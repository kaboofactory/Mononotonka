using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mononotonka;

namespace Mononotonka
{
    /// <summary>
    /// TonParticle 動作確認用シーン
    /// </summary>
    public class SampleScene07 : IScene
    {
        // Aボタン押下時間
        float fHoldAButton = 0.0f;

        // パーティクルクラス

        private string _infoText = "Click Left/Right Mouse Button to emit particles.";

        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // パーティクルシステムの初期化
            // _particles = new TonParticle(); // ローカル生成せずグローバルを使用

            // 1. 爆発風 (ハート使用)
            var explosionParam = new TonParticleParam
            {
                ImageName = "heart",
                MinLife = 500,
                MaxLife = 1000,
                MinSpeed = 2f,
                MaxSpeed = 6f,
                MinScale = 0.5f,
                MaxScale = 1.2f,
                StartColor = Color.Red,
                EndColor = Color.Transparent,
                IsAdditive = true // 加算合成で光らせる
            };
            Ton.Particle.Register("Explosion", explosionParam);

            // 2. 飛び散る火花風 (アイテムアイコン使用)
            var sparkParam = new TonParticleParam
            {
                ImageName = "item",
                MinLife = 800,
                MaxLife = 1500,
                MinSpeed = 3f,
                MaxSpeed = 8f,
                MinAngle = -MathHelper.Pi / 2 - 0.5f, // 上方向中心に拡散
                MaxAngle = -MathHelper.Pi / 2 + 0.5f,
                MinScale = 0.3f,
                MaxScale = 0.6f,
                Gravity = 0.2f, // 重力あり
                StartColor = Color.Yellow,
                EndColor = Color.Orange,
                IsAdditive = true
            };
            Ton.Particle.Register("Spark", sparkParam);

            // 初期化処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Initialized.");
        }

        public void Terminate()
        {
            // 終了処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminating.");

            // パーティクルのクリア処理
            Ton.Particle.Clear();

            // 終了処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminated.");
        }

        public void Update(GameTime gameTime)
        {
            // Aボタン押下時間更新
            if (Ton.Input.IsPressed("A"))
            {
                fHoldAButton += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (fHoldAButton >= 1.0f)
                {
                    // Aボタンを1秒以上押していたら次のシーンへ移動(フェードアウト・フェードイン時間を指定可能)
                    Ton.Scene.Change(new SampleScene08(), 0.5f, 0.5f, Color.Lime);
                }
            }
            else
            {
                fHoldAButton = 0.0f;
            }

            // マウス入力取得
            var mouseState = Mouse.GetState();

            // 左クリックで爆発
            if (Ton.Input.IsMouseJustPressed(MouseButton.Left))
            {
                // マウス位置で発生
                Ton.Particle.Play("Explosion", mouseState.X, mouseState.Y, 10);
                _infoText = $"Explosion at ({mouseState.X}, {mouseState.Y})";
            }

            // 右クリックで火花
            if (Ton.Input.IsMouseJustPressed(MouseButton.Right))
            {
                Ton.Particle.Play("Spark", mouseState.X, mouseState.Y, 5);
                _infoText = $"Spark at ({mouseState.X}, {mouseState.Y})";
            }
            
            // パーティクル更新はTon.Instance.Updateで行われるため不要
        }

        public void Draw()
        {
            Ton.Gra.Clear(Color.Black);

            // マウスカーソル表示
            Ton.Gra.Draw("finger", (int)Ton.Input.GetMousePosition().X - 58, (int)Ton.Input.GetMousePosition().Y - 32);

            // 説明テキスト
            Ton.Gra.DrawText("Seven Scene: TonParticle Test (Use Mouse)", 20, 10, Color.White, 0.8f);
            Ton.Gra.DrawText(_infoText, 20, 60, Color.Gray, 0.8f);
            Ton.Gra.DrawText("[L-Click] Explosion (Heart)   [R-Click] Spark (Item)   [Space/A] Go to Menu Test", 20, 680, Color.Cyan, 0.5f);

            // パーティクル描画はTon.Instance.Drawで行われるため不要

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(fHoldAButton * 400.0f), 160, 0.6f + (fHoldAButton));
        }
    }
}
