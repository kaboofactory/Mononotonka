using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// シーンクラスのテンプレートです。
    /// 1. このファイルをコピーして新しい名前（例: SceneTitle.cs）に変更してください。
    /// 2. 下記のクラス名 "TonSceneTemplate" を新しいファイル名に合わせて変更してください。
    /// 3. Ton.Scene.Change(new SceneTitle()); のように呼び出して使用します。
    /// </summary>
    public class SampleScene04 : IScene
    {

        // Aボタン押下時間
        float fHoldAButton = 0.0f;

        // キャラクター個別の状態管理用クラス
        private class CharacterState
        {
            public string Id;
            public CharacterAnimType CurrentState;
            public double Timer;
        }

        private List<CharacterState> _characters = new List<CharacterState>();
        private Random _random = new Random();

        /// <summary>
        /// シーン開始時に一度だけ呼ばれます。リソースのロードや変数の初期化を行います。
        /// </summary>
        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // TODO: ここに初期化処理を記述
            // 例: Ton.Gra.LoadTexture("image/player", "player");

            // 4体のキャラクターを追加
            for (int i = 1; i <= 4; i++)
            {
                string cid = "chara" + i.ToString();
                int startX = ((Ton.Game.VirtualWidth / 6) * i) + 100;
                float gravity = 1800.0f - (i - 1) * 300.0f; // 重力を少しずつ変える (1800ベース)

                // キャラクターを追加
                Ton.Character.AddCharacter(cid, startX, 600, gravity);

                // 一人だけおっきくする
                if(i == 3)
                {
                    Ton.Character.SetScale(cid, 5.0f);
                }
                if (i == 4)
                {
                    Ton.Character.SetScale(cid, 0.5f);
                }

                // アニメーション設定
                int x = (i - 1) % 2;
                int y = (i - 1) / 2;
                Ton.Character.AddAnim(cid, CharacterAnimType.Idle, "chara_animation", x * 256, y * 256, 64, 64, 4, 125);
                Ton.Character.AddAnim(cid, CharacterAnimType.Walk, "chara_animation", x * 256, (y * 256) + 64, 64, 64, 4, 125);
                Ton.Character.AddAnim(cid, CharacterAnimType.Jump, "chara_animation", x * 256, (y * 256) + 128, 64, 64, 4, 125);
                Ton.Character.AddAnim(cid, CharacterAnimType.Panic, "chara_animation", x * 256, (y * 256) + 192, 64, 64, 4, 125);

                // クリップ設定
                Ton.Character.SetClipping(cid, true, 100, Ton.Game.VirtualWidth - 100);

                // 摩擦係数を設定する
                Ton.Character.SetPhysics(cid, true, true, 2.0f);

                // 状態管理オブジェクト追加
                _characters.Add(new CharacterState
                {
                    Id = cid,
                    CurrentState = CharacterAnimType.Idle,
                    Timer = 1.0 + _random.NextDouble() // ランダムな初期待機
                });
            }

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

            // キャラクターを全て削除
            Ton.Character.RemoveCharacter();

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
                    Ton.Scene.Change(new SampleScene05(), 0.5f, 0.5f, Color.Lime);
                }
            }
            else
            {
                fHoldAButton = 0.0f;
            }

            // キャラクターアニメーションの計算
            Ton.Character.Update(gameTime);

            // 各キャラクターの行動ロジック
            foreach (var charItem in _characters)
            {
                UpdateCharacterBehavior(charItem, gameTime);
            }
        }

        // キャラクターの自律行動処理
        private void UpdateCharacterBehavior(CharacterState c, GameTime gameTime)
        {
            switch (c.CurrentState)
            {
                // ==========================================
                // 待機状態 (Idle)
                // ==========================================
                case CharacterAnimType.Idle:
                    // タイマーを減らす
                    c.Timer -= gameTime.ElapsedGameTime.TotalSeconds;
                    
                    // 待機時間が終わったら次の行動をランダムに決定
                    if (c.Timer <= 0)
                    {
                        int next = _random.Next(3); // 0:移動, 1:ジャンプ, 2:パニック
                        
                        if (next == 0)
                        {
                            // 移動へ移行
                            c.CurrentState = CharacterAnimType.Walk;
                            
                            // ランダムな目的地を設定
                            int targetX = _random.Next(100, Ton.Game.VirtualWidth - 100);

                            // 移動速度をランダムに決定 (120.0f ～ 300.0f px/sec)
                            float speed = 120.0f + (float)_random.NextDouble() * 180.0f;
                            
                            // 移動命令を出すだけ。アニメーションは自動でWalkになる。
                            Ton.Character.MoveTo(c.Id, targetX, speed, CharacterAnimType.Walk);
                        }
                        else if (next == 1)
                        {
                            // ジャンプへ移行
                            c.CurrentState = CharacterAnimType.Jump;
                            
                            // X方向の飛び出し速度 (px/sec)
                            float vx = (float)(_random.NextDouble() * 1200.0 - 600.0);
                            // Y方向のジャンプ力 (上方向はマイナス, px/sec)
                            float vy = -720.0f; 
                            
                            // ジャンプ命令を出すだけ。空中判定になれば自動でJumpアニメになる。
                            Ton.Character.Jump(c.Id, vx, vy);
                        }
                        else
                        {
                            // パニック（往復移動）へ移行
                            c.CurrentState = CharacterAnimType.Panic;
                            
                            // 移動アニメーションとしてPanicを指定して往復移動 (300 px/sec)
                            Ton.Character.RoundTrip(c.Id, 100, 300.0f, CharacterAnimType.Panic);
                            
                            // パニック継続時間を決定 (2.0秒～4.0秒)
                            c.Timer = 2.0 + _random.NextDouble() * 2.0;
                        }
                    }
                    break;

                // ==========================================
                // 移動状態 (Walk)
                // ==========================================
                case CharacterAnimType.Walk:
                    // 自動移動が完了したかチェック
                    if (!Ton.Character.IsMoving(c.Id))
                    {
                        // 移動完了したのでIdleに戻る
                        c.CurrentState = CharacterAnimType.Idle;
                        c.Timer = 0.5 + _random.NextDouble() * 1.5; // 0.5-2.0s待機
                        
                        // 停止命令は不要（移動完了で自動停止し、Update内でIdleに戻る）
                    }
                    break;

                // ==========================================
                // ジャンプ状態 (Jump)
                // ==========================================
                case CharacterAnimType.Jump:
                     // 地面に着地したかチェック
                     if (Ton.Character.IsOnGround(c.Id))
                     {
                         // 着地したのでIdleに戻る
                         c.CurrentState = CharacterAnimType.Idle;
                         c.Timer = 0.5 + _random.NextDouble() * 1.5;
                         
                         // 着地時に慣性を消すためにStopを呼ぶのはありだが、
                         // 摩擦で自然に止まるなら必須ではない。ここでは明示的に止める。
                         // Ton.Character.Stop(c.Id);
                     }
                    break;

                // ==========================================
                // パニック状態 (Panic / RoundTrip)
                // ==========================================
                case CharacterAnimType.Panic:
                    // タイマーを減らす
                    c.Timer -= gameTime.ElapsedGameTime.TotalSeconds;
                    
                    // パニック時間が終わったら終了
                    if (c.Timer <= 0)
                    {
                        // 往復移動を停止
                        Ton.Character.Stop(c.Id);
                        
                        // Idleへ移行
                        c.CurrentState = CharacterAnimType.Idle;
                        c.Timer = 0.5 + _random.NextDouble() * 1.5;
                    }
                    break;
            }
        }

        /// <summary>
        /// 毎フレーム描画処理が呼ばれます。描画コードを記述します。
        /// </summary>
        public void Draw()
        {
            // 背景を描画
            Ton.Gra.DrawBackground("landscape");

            // テキストを表示
            Ton.Gra.DrawText("9-Patch Window Test", 20, 20, 0.6f);

            // 9-patch画像の描画
            Ton.Gra.FillRoundedRect("9-patch", 50, 80
                , 100 + (int)((Math.Cos(Ton.Game.TotalGameTime.TotalSeconds * 3.0) * 50.0))
                , 100 + (int)((Math.Sin(Ton.Game.TotalGameTime.TotalSeconds * 3.0) * 50.0))
                , 16, 16);

            // テキストを表示
            Ton.Gra.DrawText("Character Moving Test", 20, 240, 0.8f);

            // FPS情報を表示
            String str2 = String.Format("FPS: (Update {0}, Draw {1}) FullScreen ({2}) Virtual Resolution ({3},{4})"
                , Math.Round(Ton.Game.UpdateFPS, MidpointRounding.AwayFromZero)
                , Math.Round(Ton.Game.DrawFPS, MidpointRounding.AwayFromZero)
                , Ton.Game.IsFullScreen, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight);
            Ton.Gra.DrawText(str2, 10, Ton.Game.VirtualHeight - 30, 0.6f);

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(fHoldAButton * 400.0f), 160, 0.6f + (fHoldAButton));
            
            // 座標取得テスト (Debug)
            // 全キャラの座標を表示すると画面が埋まるので代表してchara1を表示、もしくは簡易表示
            int debugY = Ton.Game.VirtualHeight - 50;
            foreach(var c in _characters)
            {
                var pos = Ton.Character.GetPos(c.Id);
                Ton.Gra.DrawText($"{c.Id}: {pos.X:F0}, {pos.Y:F0}", 10 + (int.Parse(c.Id.Substring(5)) -1) * 200, debugY, 0.4f);
            }

            // キャラクターアニメの描画
            Ton.Character.Draw();
        }
    }
}
