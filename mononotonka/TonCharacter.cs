using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// キャラクターのアニメーションタイプ定義
    /// </summary>
    public enum CharacterAnimType {
        /// <summary>待機</summary>
        Idle,
        /// <summary>歩行</summary>
        Walk,
        /// <summary>走行</summary>
        Run,
        /// <summary>ジャンプ</summary>
        Jump,
        /// <summary>驚き</summary>
        Surprise,
        /// <summary>パニック</summary>
        Panic,
        /// <summary>喜び</summary>
        Happy,
        /// <summary>ダメージ</summary>
        Damage,
        /// <summary>死亡</summary>
        Die,
        /// <summary>ユーザー定義</summary>
        UserDefine
    }

    /// <summary>
    /// キャラクター管理クラスです。
    /// 複数のキャラクター（エージェント）の座標、アニメーション、物理挙動（重力、物理演算）を管理します。
    /// </summary>
    public class TonCharacter
    {
        private class TonAnimStateInfo
        {
             public string ImageName;
             public int Width, Height;
             public int Duration;
             public bool IsLoop;
             public int X1, Y1;
             public int FrameCount;
        }

        /// <summary>
        /// 個々のキャラクター情報を保持する内部クラス。
        /// </summary>
        private class TonCharacterAgent
        {
            public string Id;
            public Vector2 Position;
            public Vector2 Velocity;
            public float GroundY;
            public bool UseGravity = true;
            public bool CheckGround = true;
            public float Friction = 5.0f;
            public float Gravity = 1800.0f;
            
            // アニメーション管理
            public Dictionary<CharacterAnimType, TonAnimStateInfo> Anims = new Dictionary<CharacterAnimType, TonAnimStateInfo>();
            public TonAnimState CurrentAnimState;
            public CharacterAnimType CurrentAnimType;
            public bool InAction = false;
            
            // 移動ロジック用
            public bool IsMovingTo = false;
            public float MoveTargetX;
            public float MoveSpeed;
            
            public bool IsRoundTrip = false;
            public float RoundBaseX;
            public float RoundDist;
            public float RoundSpeed;
            public int RoundDir = 1;
            
            // Movement Animation Type (Default: Walk)
            public CharacterAnimType MoveAnimType = CharacterAnimType.Walk;

            // Clipping
            public bool UseClipping = false;
            public int ClipMinX;
            public int ClipMaxX;

            // Display
            public float Scale = 1.0f;

            public TonCharacterAgent(string id, int x, int y)
            {
                Id = id;
                Position = new Vector2(x, y);
                GroundY = y;
                CurrentAnimState = new TonAnimState();
            }
        }

        private Dictionary<string, TonCharacterAgent> _agents = new Dictionary<string, TonCharacterAgent>();

        /// <summary>
        /// 新しいキャラクターを追加します。
        /// </summary>
        /// <param name="id">キャラクターID</param>
        /// <param name="x">初期X座標</param>
        /// <param name="y">初期Y座標（兼、地面の高さ）</param>
        /// <param name="gravity">重力加速度 (Default: 1800.0f px/s^2)</param>
        public void AddCharacter(string id, int x, int y, float gravity = 1800.0f)
        {
            if (_agents.ContainsKey(id))
            {
                Ton.Log.Error($"TonCharacter.AddCharacter: Character '{id}' already exists.");
                return;
            }

            var agent = new TonCharacterAgent(id, x, y);
            agent.Gravity = gravity;
            _agents[id] = agent;
        }

        /// <summary>
        /// キャラクターを削除します。
        /// </summary>
        /// <param name="id">削除するキャラクターID（nullの場合は全て削除）</param>
        public void RemoveCharacter(string id = null)
        {
            if (id == null)
            {
                _agents.Clear();
                return;
            }

            if (!_agents.ContainsKey(id))
            {
                Ton.Log.Error($"TonCharacter.RemoveCharacter: Character '{id}' not found.");
                return;
            }

            _agents.Remove(id);
        }

        /// <summary>
        /// アニメーション設定用構造体
        ///  </summary>
        /// <param name="imageName">使用画像名</param>
        /// <param name="x1">画像内の開始X座標</param>
        /// <param name="y1">画像内の開始Y座標</param>
        /// <param name="w">1フレームの幅</param>
        /// <param name="h">1フレームの高さ</param>
        /// <param name="frameCount">アニメーションの総フレーム数</param>
        /// <param name="duration">フレーム切り替え間隔(ms)</param>
        /// <param name="isLoop">ループするかどうか</param>
        public struct AnimConfig
        {
            public string ImageName;
            public int X, Y;
            public int Width, Height;
            public int FrameCount;
            public int Duration;
            public bool IsLoop;

            public AnimConfig(string imageName, int x, int y, int w, int h, int frameCount, int duration, bool isLoop = true)
            {
                ImageName = imageName;
                X = x; Y = y;
                Width = w; Height = h;
                FrameCount = frameCount;
                Duration = duration;
                IsLoop = isLoop;
            }
        }

        /// <summary>
        /// アニメーション定義を追加します。
        /// </summary>
        /// <param name="id">キャラクターID</param>
        /// <param name="type">アニメーションタイプ</param>
        /// <param name="config">アニメーション設定</param>
        public void AddAnim(string id, CharacterAnimType type, AnimConfig config)
        {
             if (!_agents.ContainsKey(id))
            {
                Ton.Log.Error($"TonCharacter.AddAnim: Character '{id}' not found.");
                return;
            }

            _agents[id].Anims[type] = new TonAnimStateInfo
            {
                ImageName = config.ImageName, Width = config.Width, Height = config.Height, Duration = config.Duration, IsLoop = config.IsLoop,
                X1 = config.X, Y1 = config.Y, FrameCount = config.FrameCount
            };

            if (type == CharacterAnimType.Idle)
            {
                SetAnim(id, type);
            }
        }

        /// <summary>
        /// アニメーション定義を追加します。（旧形式互換、将来的に非推奨の可能性あり）
        /// </summary>
        public void AddAnim(string id, CharacterAnimType type, string imageName, int x1, int y1, int w, int h, int frameCount, int duration, bool isLoop = true)
        {
            AddAnim(id, type, new AnimConfig(imageName, x1, y1, w, h, frameCount, duration, isLoop));
        }

        /// <summary>
        /// 物理挙動パラメータを設定します。
        /// </summary>
        /// <param name="id">キャラクターID</param>
        /// <param name="useGravity">重力を有効にするか</param>
        /// <param name="checkGround">地面との当たり判定を行うか</param>
        /// <param name="friction">摩擦係数（0.0-1.0）</param>
        public void SetPhysics(string id, bool useGravity, bool checkGround, float friction)
        {
            if (!_agents.ContainsKey(id))
            {
                 Ton.Log.Error($"TonCharacter.SetPhysics: Character '{id}' not found.");
                 return;
            }

            var a = _agents[id];
            a.UseGravity = useGravity;
            a.CheckGround = checkGround;
            a.Friction = friction;
        }

        public void SetVelocity(string id, float vx, float vy)
        {
             if (!_agents.ContainsKey(id))
             {
                 Ton.Log.Error($"TonCharacter.SetVelocity: Character '{id}' not found.");
                 return;
             }

             _agents[id].Velocity = new Vector2(vx, vy);
             _agents[id].IsMovingTo = false; // 自動移動キャンセル
             _agents[id].IsRoundTrip = false;
        }

        /// <summary>
        /// キャラクターの描画倍率を設定します。
        /// </summary>
        /// <param name="id">キャラクターID</param>
        /// <param name="scale">倍率（1.0で等倍）</param>
        public void SetScale(string id, float scale)
        {
             if (!_agents.ContainsKey(id))
             {
                 Ton.Log.Error($"TonCharacter.SetScale: Character '{id}' not found.");
                 return;
             }
             _agents[id].Scale = scale;
        }

        /// <summary>
        /// 指定座標へ移動させます（自動移動）。到達すると停止します。
        /// </summary>
        /// <param name="id">キャラクターID</param>
        /// <param name="targetX">目標X座標</param>
        /// <param name="speed">移動速度</param>
        /// <param name="moveAnim">移動時のアニメーション（指定しない場合はWalk）</param>
        public void MoveTo(string id, int targetX, float speed, CharacterAnimType moveAnim = CharacterAnimType.Walk)
        {
            if (!_agents.ContainsKey(id))
            {
                Ton.Log.Error($"TonCharacter.MoveTo: Character '{id}' not found.");
                return;
            }

            var a = _agents[id];
            a.IsMovingTo = true;
            a.IsRoundTrip = false;
            a.MoveTargetX = targetX;
            a.MoveSpeed = Math.Abs(speed);
            a.MoveAnimType = moveAnim;
        }

        /// <summary>
        /// 往復移動を開始させます。
        /// </summary>
        /// <param name="id">キャラクターID</param>
        /// <param name="distance">現在の位置からの片道の移動距離</param>
        /// <param name="speed">移動速度</param>
        /// <param name="moveAnim">移動時のアニメーション（指定しない場合はWalk）</param>
        public void RoundTrip(string id, int distance, float speed, CharacterAnimType moveAnim = CharacterAnimType.Walk)
        {
            if (!_agents.ContainsKey(id))
            {
                Ton.Log.Error($"TonCharacter.RoundTrip: Character '{id}' not found.");
                return;
            }

            var a = _agents[id];
            a.IsRoundTrip = true;
            a.IsMovingTo = false;
            a.RoundBaseX = a.Position.X;
            a.RoundDist = distance;
            a.RoundSpeed = Math.Abs(speed);
            a.RoundDir = 1;
            a.MoveAnimType = moveAnim;
        }

        /// <summary>
        /// アクションアニメーションを再生します
        /// </summary>
        /// <param name="id">キャラクターID</param>
        /// <param name="type">再生するアニメーションタイプ</param>
        public void PlayAction(string id, CharacterAnimType type)
        {
            if (!_agents.ContainsKey(id))
            {
                Ton.Log.Error($"TonCharacter.PlayAction: Character '{id}' not found.");
                return;
            }

            SetAnim(id, type);
            _agents[id].InAction = true;
        }

        /// <summary>
        /// キャラクターを停止させます。
        /// </summary>
        /// <param name="id">キャラクターID</param>
        public void Stop(string id)
        {
             if (!_agents.ContainsKey(id))
            {
                Ton.Log.Error($"TonCharacter.Stop: Character '{id}' not found.");
                return;
            }

            var a = _agents[id];
            a.Velocity = Vector2.Zero;
            a.IsMovingTo = false;
            a.IsRoundTrip = false;
            a.InAction = false;
            SetAnim(id, CharacterAnimType.Idle);
        }

        /// <summary>
        /// 移動範囲制限（クリッピング）を設定します。
        /// </summary>
        /// <param name="id">キャラクターID</param>
        /// <param name="enable">有効無効</param>
        /// <param name="minX">最小X</param>
        /// <param name="maxX">最大X</param>
        public void SetClipping(string id, bool enable, int minX = 0, int maxX = 0)
        {
            if((minX == 0) && (minX == maxX))
            {
                // 既定値の場合、画面端に設定
                minX = 0;
                maxX = Ton.Game.VirtualWidth;
            }

            if (!_agents.ContainsKey(id))
            {
                Ton.Log.Error($"TonCharacter.SetClipping: Character '{id}' not found.");
                return;
            }

            var a = _agents[id];
            a.UseClipping = enable;
            a.ClipMinX = minX;
            a.ClipMaxX = maxX;
        }

        /// <summary>
        /// キャラクターをジャンプ（または強制速度付与）させます。
        /// </summary>
        /// <param name="id">キャラクターID</param>
        /// <param name="vx">X速度</param>
        /// <param name="vy">Y速度（上方向はマイナス）</param>
        public void Jump(string id, float vx, float vy)
        {
            if (!_agents.ContainsKey(id))
            {
                Ton.Log.Error($"TonCharacter.Jump: Character '{id}' not found.");
                return;
            }

            var a = _agents[id];
            a.Velocity = new Vector2(vx, vy);
            a.IsMovingTo = false; // 自動移動キャンセル
            a.IsRoundTrip = false;
        }

        /// <summary>
        /// キャラクターが自動移動中かどうかを返します。
        /// </summary>
        public bool IsMoving(string id)
        {
             if (_agents.ContainsKey(id)) return _agents[id].IsMovingTo;
             return false;
        }

        /// <summary>
        /// キャラクターが接地しているかを返します。
        /// </summary>
        public bool IsOnGround(string id)
        {
             if (_agents.ContainsKey(id))
             {
                 var a = _agents[id];
                 if (!a.CheckGround) return false; // 接地判定無効ならfalse
                 // 接地高さ（GroundY）にいるか、またはそれより下にいるとみなされる場合
                 // 厳密には Position.Y >= GroundY だが、浮動小数点の誤差を考慮して少し緩める手もあるが、
                 // Update内で補正されているので == GroundY判定に近い形でOK。
                 // ここでは速度も加味するのが安全（上昇中は接地していない）
                 return (a.Position.Y >= a.GroundY && a.Velocity.Y >= 0);
             }
             return false;
        }

        /// <summary>
        /// キャラクターの現在座標を取得します。
        /// </summary>
        /// <param name="id">キャラクターID</param>
        /// <returns>座標 (存在しない場合はZero)</returns>
        public Vector2 GetPos(string id)
        {
             if (_agents.ContainsKey(id)) return _agents[id].Position;
             return Vector2.Zero;
        }

        /// <summary>
        /// 全キャラクターの更新処理を行います。
        /// 物理演算、移動ロジック、アニメーション更新が含まれます。
        /// </summary>
        public void Update(GameTime gameTime)
        {
             // 物理演算用（簡易的なオイラー積分）
             float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            foreach (var agent in _agents.Values)
            {
                // 自動移動ロジック
                if (agent.IsMovingTo)
                {
                    float dist = agent.MoveTargetX - agent.Position.X;
                    float step = agent.MoveSpeed * dt;

                    if (Math.Abs(dist) <= step)
                    {
                        agent.Position.X = agent.MoveTargetX;
                        agent.Velocity.X = 0;
                        agent.IsMovingTo = false;
                    }
                    else
                    {
                        // 速度を直接設定（Position加算は後で行うのでここではVelocityを設定するだけ）
                        // しかし以下のロジックだと dt をかけずにセットして、後で dt をかける必要がある。
                        // MoveSpeed は px/sec なので、Velocity.X = ±MoveSpeed とすればよい。
                        agent.Velocity.X = Math.Sign(dist) * agent.MoveSpeed;
                    }
                }
                else if (agent.IsRoundTrip)
                {
                    float minX = agent.RoundBaseX - agent.RoundDist;
                    float maxX = agent.RoundBaseX + agent.RoundDist;
                    
                    if (agent.RoundDir > 0)
                    {
                        agent.Velocity.X = agent.RoundSpeed;
                        if (agent.Position.X >= maxX) agent.RoundDir = -1;
                    }
                    else
                    {
                        agent.Velocity.X = -agent.RoundSpeed;
                        if (agent.Position.X <= minX) agent.RoundDir = 1;
                    }
                }
                else
                {
                    // 摩擦抵抗 (Velocity減衰 = Friction * dt ではなく、割合で減らすか、固定値で減らすか)
                    // 元のコード: agent.Velocity.X *= (1.0f - agent.Friction); はフレーム毎の減衰率。
                    // 時間依存にするなら: Velocity.X -= Velocity.X * Friction * dt;
                    if (agent.CheckGround && agent.Position.Y >= agent.GroundY)
                    {
                        // Frictionの値を調整する必要あり。元の0.2fは毎フレーム20%減衰。
                        // ここでは Friction を減衰係数(1/s)とみなす。
                        // 例えば Friction=5.0f なら 1秒でほぼ止まる計算。
                        agent.Velocity.X -= agent.Velocity.X * agent.Friction * dt;
                        
                        if (Math.Abs(agent.Velocity.X) < 10.0f) agent.Velocity.X = 0;
                    }
                }

                // 重力計算
                if (agent.UseGravity)
                {
                    agent.Velocity.Y += agent.Gravity * dt; // 重力加速度 * 時間
                }

                // 速度適用
                agent.Position += agent.Velocity * dt;

                // クリッピング判定（移動後に行う）
                if (agent.UseClipping)
                {
                    if (agent.Position.X < agent.ClipMinX)
                    {
                        // 左端を超えた -> 反転して押し戻す
                        float over = agent.ClipMinX - agent.Position.X;
                        agent.Position.X = agent.ClipMinX + over;
                        agent.Velocity.X *= -1; // 速度反転
                        agent.IsMovingTo = false; // MoveToキャンセル
                        
                        // RoundTripの場合は方向反転ロジックが既にあるが、ここで速度反転させると
                        // 次のフレームでRoundTripロジックと競合する可能性がある。
                        // しかしRoundTripはPositionを見てRoundDirを変えているので、座標さえ戻せば整合するはず。
                        // 安全のためRoundDirも同期させる
                        if (agent.IsRoundTrip) agent.RoundDir = 1; 
                    }
                    else if (agent.Position.X > agent.ClipMaxX)
                    {
                        // 右端を超えた
                        float over = agent.Position.X - agent.ClipMaxX;
                        agent.Position.X = agent.ClipMaxX - over;
                        agent.Velocity.X *= -1;
                        agent.IsMovingTo = false;

                        if (agent.IsRoundTrip) agent.RoundDir = -1;
                    }
                }

                // 地面判定
                if (agent.CheckGround)
                {
                    if (agent.Position.Y >= agent.GroundY)
                    {
                        agent.Position.Y = agent.GroundY;
                        agent.Velocity.Y = 0;
                    }
                }

                // アニメーション状態の自動遷移
                if (agent.InAction)
                {
                    // Action（PlayActionで指定されたワンショット動作）中は、
                    // ユーザーが明示的にStop等を呼ぶか、ループなしアニメが終了するのを待つ。
                    
                    // アクション終了チェック (非ループの場合のみ)
                    if (agent.CurrentAnimState.CurrentFrame >= agent.CurrentAnimState.FrameCount - 1 && !agent.CurrentAnimState.IsLoop)
                    {
                        agent.InAction = false;
                        // 次のフレームで自動遷移ロジックにより適切な状態へ移行する
                    }
                }
                else
                {
                    // ----------------------------------------------------
                    // 物理状態に基づくアニメーション自動決定 (Auto-State)
                    // ----------------------------------------------------
                    CharacterAnimType targetType = CharacterAnimType.Idle;

                    // 優先順位 1: 空中 (Jump)
                    // 接地しておらず、重力が有効な場合
                    if (agent.UseGravity && (!agent.CheckGround || agent.Position.Y < agent.GroundY))
                    {
                        targetType = CharacterAnimType.Jump;
                    }
                    // 優先順位 2: 移動中 (MoveAnimType)
                    else if (Math.Abs(agent.Velocity.X) > 0.1f) // 微小な揺らぎは無視
                    {
                        targetType = agent.MoveAnimType;
                    }
                    // 優先順位 3: 停止中 (Idle)
                    else
                    {
                        targetType = CharacterAnimType.Idle;
                    }

                    // 状態が変化する場合に適用 (SetAnim内部で重複チェックしてるので呼んでもいいが、明示的に)
                    if (agent.CurrentAnimType != targetType)
                    {
                        // フォールバック処理
                        // 指定されたアニメーションが存在しない場合、Idleを試みる
                        if (agent.Anims.ContainsKey(targetType))
                        {
                            SetAnim(agent.Id, targetType);
                        }
                        else
                        {
                             // ターゲットが存在しない場合、とりあえずIdleにする
                             // (Idleすら登録されていない場合はSetAnim内で何もしないので安全)
                             if (agent.Anims.ContainsKey(CharacterAnimType.Idle))
                             {
                                 SetAnim(agent.Id, CharacterAnimType.Idle);
                             }
                        }
                    }
                }

                // 向き補正
                if (agent.Velocity.X > 0) agent.CurrentAnimState.direction = AnimDirection.LeftToRight; // 描画時のFlip判定に使用

                // アニメーション更新
                agent.CurrentAnimState.Update(gameTime);
            }
        }
        
        // アニメーション設定ヘルパー
        private void SetAnim(string id, CharacterAnimType type)
        {
            var agent = _agents[id];
            if (agent.Anims.ContainsKey(type))
            {
                var info = agent.Anims[type];
                agent.CurrentAnimType = type;
                agent.CurrentAnimState = new TonAnimState
                {
                    IsLoop = info.IsLoop,
                    FrameCount = info.FrameCount,
                    FrameDuration = info.Duration,
                    width = info.Width,
                    height = info.Height,
                    x1 = info.X1, y1 = info.Y1
                };
            }
        }

        /// <summary>
        /// 全キャラクターの描画を行います。
        /// </summary>
        public void Draw()
        {
            foreach (var agent in _agents.Values)
            {
                if (agent.Anims.ContainsKey(agent.CurrentAnimType))
                {
                    var info = agent.Anims[agent.CurrentAnimType];
                    // 速度が負なら反転
                    bool flipH = (agent.Velocity.X < 0); 
                    
                    var param = new TonDrawParamEx
                    { 
                        FlipH = flipH,
                        ScaleX = agent.Scale,
                        ScaleY = agent.Scale
                    };
                    
                    // 足元中心からの描画位置補正
                    // DrawAnimExは中心基準で描画するため、足元のY座標から「画像の中心Y座標」を求めます。
                    // 画像の高さが Height * Scale なので、中心は 足元Y - (Height * Scale) / 2
                    float drawX = agent.Position.X;
                    float drawY = agent.Position.Y - ((info.Height * agent.Scale) / 2.0f);
                    
                    Ton.Gra.DrawAnimEx(info.ImageName, drawX, drawY, agent.CurrentAnimState, param);
                }
            }
        }
    }
}
