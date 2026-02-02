using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mononotonka
{
    /// <summary>
    /// パーティクルのパラメータ定義クラス。
    /// 生成時の寿命、速度、角度、サイズ、重力、色などを設定します。
    /// </summary>
    public class TonParticleParam
    {
        /// <summary>使用する画像の登録名</summary>
        public string ImageName;
        /// <summary>最小生存時間(ms)</summary>
        public int MinLife = 500;
        /// <summary>最大生存時間(ms)</summary>
        public int MaxLife = 1000;
        /// <summary>最小速度</summary>
        public float MinSpeed = 1f;
        /// <summary>最大速度</summary>
        public float MaxSpeed = 3f;
        /// <summary>最小角度(ラジアン)</summary>
        public float MinAngle = 0f;
        /// <summary>最大角度(ラジアン)</summary>
        public float MaxAngle = MathHelper.TwoPi;
        /// <summary>最小スケール</summary>
        public float MinScale = 0.5f;
        /// <summary>最大スケール</summary>
        public float MaxScale = 1.0f;
        /// <summary>重力（Y軸方向の加速度）</summary>
        public float Gravity = 0f;
        /// <summary>開始時の色</summary>
        public Color StartColor = Color.White;
        /// <summary>終了時の色（フェードアウト用）</summary>
        public Color EndColor = Color.Transparent; // デフォルトでフェードアウト
        /// <summary>加算合成を使用するかどうか</summary>
        public bool IsAdditive = false; // 加算合成を使用するか
        /// <summary>最小回転速度（ラジアン/秒）</summary>
        public float MinRotationSpeed = 0f;
        /// <summary>最大回転速度（ラジアン/秒）</summary>
        public float MaxRotationSpeed = 0f;
        /// <summary>軌道半径（円運動用、0なら無効）</summary>
        public float OrbitalRadius = 0f;
        /// <summary>軌道速度（ラジアン/秒、正で反時計回り）</summary>
        public float OrbitalSpeed = 0f;
        /// <summary>影レイヤーを描画するか（明るい背景で見やすくなる）</summary>
        public bool HasShadow = false;
        /// <summary>影の色</summary>
        public Color ShadowColor = new Color(0, 0, 0, 150);
        /// <summary>影のスケール倍率（1.0より大きいと輪郭が見える）</summary>
        public float ShadowScale = 1.2f;
    }

    /// <summary>
    /// パーティクル管理クラスです。
    /// 大量のパーティクルをプール管理し、効率的に描画します。
    /// </summary>
    public class TonParticle
    {
        private class Particle
        {
            public bool Active;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;     // 残り生存時間 (秒)
            public float TotalLife;
            public float Scale;
            public float Rotation;      // 現在の回転角度（ラジアン）
            public float RotationSpeed; // 回転速度（ラジアン/秒）
            public Vector2 OriginPos;   // 軌道中心点
            public float OrbitalAngle;  // 軌道上の現在角度
            public Color StartColor;
            public Color EndColor;
            public TonParticleParam Param; // パラメータ参照
        }

        private Dictionary<string, TonParticleParam> _params = new Dictionary<string, TonParticleParam>();
        private List<Particle> _pool;
        private int _maxParticles = 1000;
        
        public TonParticle()
        {
            _pool = new List<Particle>(_maxParticles);
            for (int i = 0; i < _maxParticles; i++) _pool.Add(new Particle());
        }

        /// <summary>
        /// パーティクルのパラメータを登録します。
        /// </summary>
        /// <param name="name">登録名</param>
        /// <param name="param">パラメータ</param>
        public void Register(string name, TonParticleParam param)
        {
            _params[name] = param;
        }

        /// <summary>
        /// パーティクルを発生させます。
        /// </summary>
        /// <param name="name">登録名</param>
        /// <param name="x">発生X座標</param>
        /// <param name="y">発生Y座標</param>
        /// <param name="count">発生個数</param>
        public void Play(string name, float x, float y, int count = 1)
        {
            if (!_params.ContainsKey(name)) return;
            var param = _params[name];

            for (int i = 0; i < count; i++)
            {
                Spawn(param, x, y);
            }
        }

        private void Spawn(TonParticleParam param, float x, float y)
        {
            // 未使用のパーティクルを探して再利用
            Particle p = null;
            foreach (var existing in _pool)
            {
                if (!existing.Active)
                {
                    p = existing;
                    break;
                }
            }
            if (p == null) return; // プールが一杯なら生成しない

            p.Active = true;
            p.Param = param;
            p.Position = new Vector2(x, y);
            
            // ランダムパラメータ設定
            float speed = Ton.Math.RandF(param.MinSpeed, param.MaxSpeed);
            float angle = Ton.Math.RandF(param.MinAngle, param.MaxAngle);
            p.Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
            
            p.TotalLife = Ton.Math.Rand(param.MinLife, param.MaxLife) / 1000f;
            p.Life = p.TotalLife;
            p.Scale = Ton.Math.RandF(param.MinScale, param.MaxScale);
            p.Rotation = Ton.Math.RandF(0, MathHelper.TwoPi); // ランダム初期角度
            p.RotationSpeed = Ton.Math.RandF(param.MinRotationSpeed, param.MaxRotationSpeed);
            
            // 軌道運動の初期化
            p.OriginPos = new Vector2(x, y);
            p.OrbitalAngle = Ton.Math.RandF(0, MathHelper.TwoPi); // ランダムな開始位置
            if (param.OrbitalRadius > 0)
            {
                // 軌道上の初期位置に配置
                p.Position = p.OriginPos + new Vector2(
                    (float)Math.Cos(p.OrbitalAngle) * param.OrbitalRadius,
                    (float)Math.Sin(p.OrbitalAngle) * param.OrbitalRadius
                );
            }
            
            p.StartColor = param.StartColor;
            p.EndColor = param.EndColor;
        }

        /// <summary>
        /// パーティクルの状態を更新します。
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var p in _pool)
            {
                if (!p.Active) continue;

                p.Life -= dt;
                if (p.Life <= 0)
                {
                    p.Active = false;
                    continue;
                }

                if (p.Param.Gravity != 0)
                {
                    p.Velocity.Y += p.Param.Gravity * dt * 60f; // 重力加算（60FPS基準で正規化）
                }

                // 軌道運動（円運動）
                if (p.Param.OrbitalRadius > 0 && p.Param.OrbitalSpeed != 0)
                {
                    p.OrbitalAngle += p.Param.OrbitalSpeed * dt;
                    p.Position = p.OriginPos + new Vector2(
                        (float)Math.Cos(p.OrbitalAngle) * p.Param.OrbitalRadius,
                        (float)Math.Sin(p.OrbitalAngle) * p.Param.OrbitalRadius
                    );
                }
                else
                {
                    p.Position += p.Velocity * dt * 60f; // 通常移動（60FPS基準で正規化）
                }
                
                p.Rotation += p.RotationSpeed * dt; // 回転更新
            }
        }

        /// <summary>
        /// すべてのパーティクルを消去します。
        /// </summary>
        public void Clear()
        {
            foreach (var p in _pool) p.Active = false;
        }

        /// <summary>
        /// パーティクルを描画します。影→通常→加算の順で描画します。
        /// </summary>
        public void Draw()
        {
            // 1. 影レイヤーを先に描画（通常合成、暗い色で大きめ）
            foreach (var p in _pool)
            {
                if (!p.Active || !p.Param.HasShadow) continue;
                DrawParticleShadow(p);
            }

            // 2. 通常合成のパーティクルを描画
            foreach (var p in _pool)
            {
                if (!p.Active || p.Param.IsAdditive) continue;
                DrawParticle(p);
            }
            
            // 現在のブレンドステート保存
            var savedState = Ton.Gra.GetBlendState();
            
            // 3. 加算合成に切り替え
            Ton.Gra.SetBlendState(TonBlendState.Additive);
            
            foreach (var p in _pool)
            {
                if (!p.Active || !p.Param.IsAdditive) continue;
                DrawParticle(p);
            }
            
            // 元のブレンドステートに戻す
            if (savedState != null)
            {
                Ton.Gra.SetBlendState(savedState);
            }
            else
            {
                Ton.Gra.SetBlendState(TonBlendState.AlphaBlend);
            }
        }

        private void DrawParticleShadow(Particle p)
        {
            float t = 1.0f - (p.Life / p.TotalLife);
            float alpha = 1.0f - t; // フェードアウト
            Color shadowColor = p.Param.ShadowColor * alpha;
            
            float shadowScale = p.Scale * p.Param.ShadowScale;
            var param = new TonDrawParamEx
            {
                ScaleX = shadowScale, ScaleY = shadowScale,
                Color = shadowColor,
                Angle = p.Rotation
            };
            
            var tex = Ton.Gra.LoadTexture(p.Param.ImageName, p.Param.ImageName);
            int w = (tex != null) ? tex.Width : 0;
            int h = (tex != null) ? tex.Height : 0;

            Ton.Gra.DrawEx(p.Param.ImageName, p.Position.X, p.Position.Y, 0, 0, w, h, param);
        }

        private void DrawParticle(Particle p)
        {
            float t = 1.0f - (p.Life / p.TotalLife);
            Color c = Color.Lerp(p.StartColor, p.EndColor, t);
            
            var param = new TonDrawParamEx
            {
                ScaleX = p.Scale, ScaleY = p.Scale,
                Color = c,
                Angle = p.Rotation  // 回転角度を適用
            };
            
            // テクスチャサイズ取得
            var tex = Ton.Gra.LoadTexture(p.Param.ImageName, p.Param.ImageName);
            int w = (tex != null) ? tex.Width : 0;
            int h = (tex != null) ? tex.Height : 0;

            // 中心基準で描画
            Ton.Gra.DrawEx(p.Param.ImageName, p.Position.X, p.Position.Y, 0, 0, w, h, param);
        }
    }
}
