using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// 魔法エフェクトのレベル定義
    /// </summary>
    public enum MagicLevel
    {
        /// <summary>レベル1</summary>
        Level1 = 1,
        /// <summary>レベル2</summary>
        Level2 = 2,
        /// <summary>レベル3</summary>
        Level3 = 3,
        /// <summary>レベル4</summary>
        Level4 = 4
    }

    /// <summary>
    /// 魔法エフェクトのパラメータ定義クラス。
    /// </summary>
    public class TonMagicEffectParam
    {
        /// <summary>パーティクル発生数</summary>
        public int ParticleCount = 20;
        /// <summary>エフェクト全体のスケール</summary>
        public float Scale = 1.0f;
        /// <summary>演出時間（秒）</summary>
        public float Duration = 1.0f;
    }

    /// <summary>
    /// 魔法エフェクト管理クラスです。
    /// FF風の攻撃エフェクトを簡単に発動できます。
    /// </summary>
    public class TonMagicEffect
    {
        // アクティブなエフェクト管理
        private class ActiveEffect
        {
            public string Type;           // エフェクト種類
            public float X, Y;            // 発生位置
            public float ElapsedTime;     // 経過時間
            public float Duration;        // 総演出時間
            public TonMagicEffectParam Param;
            public int Phase;             // フェーズ (0:上昇, 1:爆発, 2:残り火)
            public bool PhaseTriggered;   // 現フェーズのパーティクル発生済み
        }

        private List<ActiveEffect> _activeEffects = new List<ActiveEffect>();
        private bool _initialized = false;

        // パーティクル登録名のプレフィックス（ファイア系）
        private const string FIRE_RISE_PREFIX = "_magic_fire_rise_";
        private const string FIRE_BURST_PREFIX = "_magic_fire_burst_";
        private const string FIRE_EMBER_PREFIX = "_magic_fire_ember_";
        private const string FIRE_CORE_PREFIX = "_magic_fire_core_"; // 明るいコア用
        private const string FIRE_SWIRL_PREFIX = "_magic_fire_swirl_"; // 根本で回る炎
        
        // パーティクル登録名のプレフィックス（ブリザド系）
        private const string ICE_SHARD_PREFIX = "_magic_ice_shard_";   // 氷の破片
        private const string ICE_BURST_PREFIX = "_magic_ice_burst_";   // 氷の爆発
        private const string ICE_FROST_PREFIX = "_magic_ice_frost_";   // 霜の結晶
        private const string ICE_SWIRL_PREFIX = "_magic_ice_swirl_";   // 回転する氷
        
        // パーティクル登録名のプレフィックス（エアロ系）
        private const string WIND_GUST_PREFIX = "_magic_wind_gust_";   // 突風
        private const string WIND_SWIRL_PREFIX = "_magic_wind_swirl_"; // 渦巻き
        private const string WIND_LEAF_PREFIX = "_magic_wind_leaf_";   // 舞う葉
        
        // パーティクル登録名のプレフィックス（クエイク系）
        private const string EARTH_ROCK_PREFIX = "_magic_earth_rock_";   // 岩の破片
        private const string EARTH_BURST_PREFIX = "_magic_earth_burst_"; // 岩の爆発
        private const string EARTH_DUST_PREFIX = "_magic_earth_dust_";   // 土埃
        private const string EARTH_SWIRL_PREFIX = "_magic_earth_swirl_"; // 回転する岩
        
        // パーティクル登録名のプレフィックス（ケアル系）
        private const string HEAL_SPARKLE_PREFIX = "_magic_heal_sparkle_"; // きらめき
        private const string HEAL_RISE_PREFIX = "_magic_heal_rise_";       // 上昇光
        private const string HEAL_SWIRL_PREFIX = "_magic_heal_swirl_";     // 回転光
        
        // パーティクル登録名のプレフィックス（ポイズン系）
        private const string POISON_DROP_PREFIX = "_magic_poison_drop_";   // 毒液の滴り
        private const string POISON_BUBBLE_PREFIX = "_magic_poison_bubble_"; // 毒泡
        private const string POISON_SPLASH_PREFIX = "_magic_poison_splash_"; // 飛沫
        private const string POISON_SWIRL_PREFIX = "_magic_poison_swirl_";   // 渦巻き
        
        // パーティクル登録名のプレフィックス（ホーリー系）
        private const string LIGHT_SPARKLE_PREFIX = "_magic_light_sparkle_"; // キラキラ
        private const string LIGHT_BURST_PREFIX = "_magic_light_burst_";     // 光の爆発
        private const string LIGHT_RAY_PREFIX = "_magic_light_ray_";         // 光線
        private const string LIGHT_SWIRL_PREFIX = "_magic_light_swirl_";     // 回転する光

        /// <summary>
        /// 初期化処理。最初のエフェクト発動時に自動的に呼ばれます。
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            // ファイア系のパーティクルパラメータを各レベル分登録
            for (int level = 1; level <= 4; level++)
            {
                float scale = GetScaleForLevel(level);
                
                // 噴き上げフェーズ用 - 地面から勢いよく上昇
                Ton.Particle.Register(FIRE_RISE_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/fire_particle",
                    MinLife = 400,
                    MaxLife = 800,
                    MinSpeed = 16.0f * scale,  // 速い初速
                    MaxSpeed = 20.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.05f, // ほぼ真上（-90度 ± 17度）
                    MaxAngle = -MathHelper.PiOver2 + 0.05f,
                    MinScale = 0.4f * scale,
                    MaxScale = 0.8f * scale,
                    Gravity = 1.0f,  // 強い重力で落下
                    StartColor = new Color(255, 220, 100),
                    EndColor = new Color(255, 80, 0, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -4.0f,
                    MaxRotationSpeed = 4.0f,
                    HasShadow = true,
                    ShadowColor = new Color(80, 30, 0, 90),
                    ShadowScale = 1.3f
                });

                // 噴出フェーズ用 - 扇状に広がる炎
                Ton.Particle.Register(FIRE_BURST_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/fire_particle",
                    MinLife = 400,
                    MaxLife = 800,
                    MinSpeed = 12.0f * scale,
                    MaxSpeed = 16.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.15f,  // 上方向に扇状（約120度の扇）
                    MaxAngle = -MathHelper.PiOver2 + 0.15f,
                    MinScale = 0.5f * scale,
                    MaxScale = 1.0f * scale,
                    Gravity = 1.0f,  // 落下
                    StartColor = new Color(255, 180, 50),
                    EndColor = new Color(255, 50, 0, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -5.0f,
                    MaxRotationSpeed = 5.0f,
                    HasShadow = true,
                    ShadowColor = new Color(80, 30, 0, 90),
                    ShadowScale = 1.3f
                });

                // コア用 - 中心の明るい炎（上昇後すぐ消える）
                Ton.Particle.Register(FIRE_CORE_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/fire_particle",
                    MinLife = 300,
                    MaxLife = 700,
                    MinSpeed = 4.0f * scale,
                    MaxSpeed = 12.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.5f,
                    MaxAngle = -MathHelper.PiOver2 + 0.5f,
                    MinScale = 0.3f * scale,
                    MaxScale = 0.6f * scale,
                    Gravity = 0.5f,
                    StartColor = new Color(255, 255, 220), // 白熱コア
                    EndColor = new Color(255, 200, 100, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -6.0f,
                    MaxRotationSpeed = 6.0f
                });

                // 残り火フェーズ用 - ゆっくり漂う火の粉
                Ton.Particle.Register(FIRE_EMBER_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/fire_particle",
                    MinLife = 300,
                    MaxLife = 600,
                    MinSpeed = 1.0f * scale,
                    MaxSpeed = 4.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.1f,
                    MaxAngle = -MathHelper.PiOver2 + 0.1f,
                    MinScale = 0.15f * scale,
                    MaxScale = 0.3f * scale,
                    Gravity = 0.08f,  // ゆっくり落下
                    StartColor = new Color(255, 150, 50, 220),
                    EndColor = new Color(150, 50, 0, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -2.0f,
                    MaxRotationSpeed = 2.0f
                });

                // 根本で回転する炎（円軌道）
                Ton.Particle.Register(FIRE_SWIRL_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/fire_particle",
                    MinLife = 400,
                    MaxLife = 800,
                    MinSpeed = 0f,  // 軌道運動なので直線速度は不要
                    MaxSpeed = 0f,
                    MinScale = 0.4f * scale,
                    MaxScale = 0.7f * scale,
                    Gravity = 0f,  // 重力なし
                    StartColor = new Color(255, 200, 80),
                    EndColor = new Color(255, 100, 30, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -3.0f,
                    MaxRotationSpeed = 3.0f,
                    OrbitalRadius = 30f * scale,  // 回転半径
                    OrbitalSpeed = 8.0f  // 回転速度（ラジアン/秒）
                });
            }

            // ブリザド系のパーティクルパラメータを各レベル分登録
            for (int level = 1; level <= 4; level++)
            {
                float scale = GetScaleForLevel(level);
                
                // 氷の破片 - 上から降ってくる
                Ton.Particle.Register(ICE_SHARD_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/ice_particle",
                    MinLife = 500,
                    MaxLife = 900,
                    MinSpeed = 4.0f * scale,
                    MaxSpeed = 8.0f * scale,
                    MinAngle = MathHelper.PiOver2 - 0.3f,  // 下方向（90度 ± 17度）
                    MaxAngle = MathHelper.PiOver2 + 0.3f,
                    MinScale = 0.3f * scale,
                    MaxScale = 0.6f * scale,
                    Gravity = 0.2f,  // 落下加速
                    StartColor = new Color(200, 230, 255),
                    EndColor = new Color(150, 200, 255, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -5.0f,
                    MaxRotationSpeed = 5.0f,
                    HasShadow = true,
                    ShadowColor = new Color(20, 40, 80, 90),
                    ShadowScale = 1.3f
                });

                // 氷の爆発 - 放射状に広がる
                Ton.Particle.Register(ICE_BURST_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/ice_particle",
                    MinLife = 400,
                    MaxLife = 700,
                    MinSpeed = 6.0f * scale,
                    MaxSpeed = 10.0f * scale,
                    MinAngle = 0,
                    MaxAngle = MathHelper.TwoPi,  // 全方向
                    MinScale = 0.4f * scale,
                    MaxScale = 0.8f * scale,
                    Gravity = 0.05f,
                    StartColor = new Color(220, 240, 255),
                    EndColor = new Color(100, 180, 255, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -6.0f,
                    MaxRotationSpeed = 6.0f,
                    HasShadow = true,
                    ShadowColor = new Color(20, 40, 80, 90),
                    ShadowScale = 1.3f
                });

                // 霜の結晶 - ゆっくり漂う
                Ton.Particle.Register(ICE_FROST_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/ice_particle",
                    MinLife = 600,
                    MaxLife = 1200,
                    MinSpeed = 0.5f * scale,
                    MaxSpeed = 2.0f * scale,
                    MinAngle = 0,
                    MaxAngle = MathHelper.TwoPi,
                    MinScale = 0.15f * scale,
                    MaxScale = 0.35f * scale,
                    Gravity = -0.02f,  // わずかに上昇
                    StartColor = new Color(180, 220, 255, 200),
                    EndColor = new Color(100, 150, 200, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -2.0f,
                    MaxRotationSpeed = 2.0f
                });

                // 回転する氷リング
                Ton.Particle.Register(ICE_SWIRL_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/ice_particle",
                    MinLife = 500,
                    MaxLife = 800,
                    MinSpeed = 0f,
                    MaxSpeed = 0f,
                    MinScale = 0.35f * scale,
                    MaxScale = 0.6f * scale,
                    Gravity = 0f,
                    StartColor = new Color(180, 220, 255),
                    EndColor = new Color(100, 180, 255, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -4.0f,
                    MaxRotationSpeed = 4.0f,
                    OrbitalRadius = 40f * scale,
                    OrbitalSpeed = -6.0f  // 時計回り（ファイアと逆）
                });
            }

            // エアロ系のパーティクルパラメータを各レベル分登録
            for (int level = 1; level <= 4; level++)
            {
                float scale = GetScaleForLevel(level);
                
                // 突風 - 横方向に高速で流れる
                Ton.Particle.Register(WIND_GUST_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/wind_particle",
                    MinLife = 300,
                    MaxLife = 600,
                    MinSpeed = 12.0f * scale,
                    MaxSpeed = 18.0f * scale,
                    MinAngle = 0.0f,
                    MaxAngle = MathHelper.TwoPi,
                    MinScale = 0.3f * scale,
                    MaxScale = 0.6f * scale,
                    Gravity = 0f,
                    StartColor = new Color(180, 255, 180),
                    EndColor = new Color(100, 200, 100, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -8.0f,
                    MaxRotationSpeed = 8.0f,
                    HasShadow = true,
                    ShadowColor = new Color(30, 60, 30, 90),
                    ShadowScale = 1.3f
                });

                // 渦巻き - 中心を回る竜巻
                Ton.Particle.Register(WIND_SWIRL_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/wind_particle",
                    MinLife = 500,
                    MaxLife = 900,
                    MinSpeed = 0f,
                    MaxSpeed = 0f,
                    MinScale = 0.4f * scale,
                    MaxScale = 0.7f * scale,
                    Gravity = -0.1f,  // 上昇
                    StartColor = new Color(200, 255, 200),
                    EndColor = new Color(150, 220, 150, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -6.0f,
                    MaxRotationSpeed = 6.0f,
                    OrbitalRadius = 35f * scale,
                    OrbitalSpeed = 10.0f,  // 高速回転
                    HasShadow = true,
                    ShadowColor = new Color(30, 60, 30, 90),
                    ShadowScale = 1.2f
                });

                // 舞う葉 - 散らばって漂う
                Ton.Particle.Register(WIND_LEAF_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/wind_particle",
                    MinLife = 600,
                    MaxLife = 1200,
                    MinSpeed = 2.0f * scale,
                    MaxSpeed = 5.0f * scale,
                    MinAngle = 0,
                    MaxAngle = MathHelper.TwoPi,
                    MinScale = 0.15f * scale,
                    MaxScale = 0.3f * scale,
                    Gravity = 0.02f,  // ゆっくり落下
                    StartColor = new Color(150, 230, 150, 200),
                    EndColor = new Color(100, 180, 100, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -3.0f,
                    MaxRotationSpeed = 3.0f
                });
            }

            // クエイク系のパーティクルパラメータを各レベル分登録
            for (int level = 1; level <= 4; level++)
            {
                float scale = GetScaleForLevel(level);
                
                // 岩の破片 - 上に飛び散って落下
                Ton.Particle.Register(EARTH_ROCK_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/earth_particle",
                    MinLife = 500,
                    MaxLife = 900,
                    MinSpeed = 16.0f * scale,
                    MaxSpeed = 28.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.2f,  // 上方向に広く
                    MaxAngle = -MathHelper.PiOver2 + 0.2f,
                    MinScale = 0.25f * scale,
                    MaxScale = 0.5f * scale,
                    Gravity = 1.8f,  // 重い落下
                    StartColor = new Color(200, 150, 100),
                    EndColor = new Color(120, 80, 50, 0),
                    IsAdditive = false,  // 通常合成（岩は光らない）
                    MinRotationSpeed = -6.0f,
                    MaxRotationSpeed = 6.0f,
                    HasShadow = true,
                    ShadowColor = new Color(40, 30, 20, 120),
                    ShadowScale = 1.2f
                });

                // 岩の爆発 - 放射状に広がる
                Ton.Particle.Register(EARTH_BURST_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/earth_particle",
                    MinLife = 400,
                    MaxLife = 700,
                    MinSpeed = 10.0f * scale,
                    MaxSpeed = 16.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.4f,  // 上方向に広く
                    MaxAngle = -MathHelper.PiOver2 + 0.4f,
                    MinScale = 0.3f * scale,
                    MaxScale = 0.6f * scale,
                    Gravity = 1.5f,
                    StartColor = new Color(180, 130, 80),
                    EndColor = new Color(100, 70, 40, 0),
                    IsAdditive = false,
                    MinRotationSpeed = -8.0f,
                    MaxRotationSpeed = 8.0f,
                    HasShadow = true,
                    ShadowColor = new Color(40, 30, 20, 120),
                    ShadowScale = 1.2f
                });

                // 土埃 - ゆっくり漂う
                Ton.Particle.Register(EARTH_DUST_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/earth_particle",
                    MinLife = 600,
                    MaxLife = 1200,
                    MinSpeed = 1.0f * scale,
                    MaxSpeed = 3.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 1.8f,  // 上方向に広く
                    MaxAngle = -MathHelper.PiOver2 + 1.8f,
                    MinScale = 0.1f * scale,
                    MaxScale = 0.2f * scale,
                    Gravity = 0.4f,  // わずかに上昇
                    StartColor = new Color(150, 120, 80, 180),
                    EndColor = new Color(100, 80, 50, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -2.0f,
                    MaxRotationSpeed = 2.0f
                });

                // 回転する岩 - 円軌道で回る
                Ton.Particle.Register(EARTH_SWIRL_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/earth_particle",
                    MinLife = 500,
                    MaxLife = 800,
                    MinSpeed = 0f,
                    MaxSpeed = 0f,
                    MinScale = 0.2f * scale,
                    MaxScale = 0.4f * scale,
                    Gravity = -0.05f,  // わずかに上昇
                    StartColor = new Color(180, 140, 100),
                    EndColor = new Color(120, 90, 60, 0),
                    IsAdditive = false,
                    MinRotationSpeed = -5.0f,
                    MaxRotationSpeed = 5.0f,
                    OrbitalRadius = 45f * scale,
                    OrbitalSpeed = 8.0f,  // 高速回転
                    HasShadow = true,
                    ShadowColor = new Color(40, 30, 20, 100),
                    ShadowScale = 1.2f
                });
            }

            // ケアル系のパーティクルパラメータを各レベル分登録
            for (int level = 1; level <= 4; level++)
            {
                float scale = GetScaleForLevel(level);
                
                // きらめき - 上昇しながら消える
                Ton.Particle.Register(HEAL_SPARKLE_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/heal_particle",
                    MinLife = 400,
                    MaxLife = 800,
                    MinSpeed = 1.0f * scale,
                    MaxSpeed = 3.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.5f,  // 上方向
                    MaxAngle = -MathHelper.PiOver2 + 0.5f,
                    MinScale = 0.2f * scale,
                    MaxScale = 0.5f * scale,
                    Gravity = -0.1f,  // 上昇
                    StartColor = new Color(200, 255, 200),
                    EndColor = new Color(150, 255, 150, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -2.0f,
                    MaxRotationSpeed = 2.0f,
                    HasShadow = true,
                    ShadowColor = new Color(50, 100, 50, 60),
                    ShadowScale = 1.3f
                });

                // 上昇光 - まっすぐ上に昇る
                Ton.Particle.Register(HEAL_RISE_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/heal_particle",
                    MinLife = 500,
                    MaxLife = 900,
                    MinSpeed = 4.0f * scale,
                    MaxSpeed = 6.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.1f,  // ほぼ真上
                    MaxAngle = -MathHelper.PiOver2 + 0.1f,
                    MinScale = 0.3f * scale,
                    MaxScale = 0.6f * scale,
                    Gravity = -0.05f,  // わずかに加速
                    StartColor = new Color(220, 255, 220),
                    EndColor = new Color(180, 255, 180, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -3.0f,
                    MaxRotationSpeed = 3.0f
                });

                // 回転光 - 円軌道で回る
                Ton.Particle.Register(HEAL_SWIRL_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/heal_particle",
                    MinLife = 600,
                    MaxLife = 1000,
                    MinSpeed = 0f,
                    MaxSpeed = 0f,
                    MinScale = 0.25f * scale,
                    MaxScale = 0.5f * scale,
                    Gravity = -0.08f,  // 上昇
                    StartColor = new Color(180, 255, 180),
                    EndColor = new Color(120, 200, 120, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -4.0f,
                    MaxRotationSpeed = 4.0f,
                    OrbitalRadius = 40f * scale,
                    OrbitalSpeed = 5.0f,  // ゆっくり回転
                    HasShadow = true,
                    ShadowColor = new Color(50, 100, 50, 60),
                    ShadowScale = 1.2f
                });
            }

            // ポイズン系のパーティクルパラメータを各レベル分登録
            for (int level = 1; level <= 4; level++)
            {
                float scale = GetScaleForLevel(level);
                
                // 毒の渦巻き（内側）- うようよ回る
                Ton.Particle.Register(POISON_DROP_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/poison_particle",
                    MinLife = 600,
                    MaxLife = 1200,
                    MinSpeed = 0f,
                    MaxSpeed = 0f,
                    MinScale = 0.2f * scale,
                    MaxScale = 0.4f * scale,
                    Gravity = 0f,
                    StartColor = new Color(180, 100, 255),
                    EndColor = new Color(100, 200, 50, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -2.0f,
                    MaxRotationSpeed = 2.0f,
                    OrbitalRadius = 20f * scale,  // 内側の軌道
                    OrbitalSpeed = 4.0f,  // ゆっくり回転
                    HasShadow = true,
                    ShadowColor = new Color(50, 20, 80, 100),
                    ShadowScale = 1.3f
                });

                // 毒泡 - ぐるぐる回る（中間）
                Ton.Particle.Register(POISON_BUBBLE_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/poison_particle",
                    MinLife = 700,
                    MaxLife = 1400,
                    MinSpeed = 0f,
                    MaxSpeed = 0f,
                    MinScale = 0.25f * scale,
                    MaxScale = 0.5f * scale,
                    Gravity = -0.02f,  // わずかに上昇
                    StartColor = new Color(150, 255, 100),
                    EndColor = new Color(100, 50, 200, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -3.0f,
                    MaxRotationSpeed = 3.0f,
                    OrbitalRadius = 40f * scale,  // 中間の軌道
                    OrbitalSpeed = -5.0f  // 逆回転
                });

                // 毒の飛沫 - うようよとうごめく（外側）
                Ton.Particle.Register(POISON_SPLASH_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/poison_particle",
                    MinLife = 800,
                    MaxLife = 1500,
                    MinSpeed = 0f,
                    MaxSpeed = 0f,
                    MinScale = 0.3f * scale,
                    MaxScale = 0.55f * scale,
                    Gravity = 0f,
                    StartColor = new Color(200, 150, 255),
                    EndColor = new Color(80, 180, 80, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -4.0f,
                    MaxRotationSpeed = 4.0f,
                    OrbitalRadius = 60f * scale,  // 外側の軌道
                    OrbitalSpeed = 3.0f,  // ゆっくり追従
                    HasShadow = true,
                    ShadowColor = new Color(50, 20, 80, 80),
                    ShadowScale = 1.2f
                });

                // 渦巻き - 毒の渦（最外周）
                Ton.Particle.Register(POISON_SWIRL_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/poison_particle",
                    MinLife = 600,
                    MaxLife = 1100,
                    MinSpeed = 0f,
                    MaxSpeed = 0f,
                    MinScale = 0.35f * scale,
                    MaxScale = 0.65f * scale,
                    Gravity = 0.01f,  // わずかに下降
                    StartColor = new Color(160, 200, 100),
                    EndColor = new Color(120, 80, 180, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -5.0f,
                    MaxRotationSpeed = 5.0f,
                    OrbitalRadius = 80f * scale,  // 最外周の軌道
                    OrbitalSpeed = -6.0f  // 逆回転（速い）
                });
            }

            // ホーリー（光）系のパーティクルパラメータを各レベル分登録
            for (int level = 1; level <= 4; level++)
            {
                float scale = GetScaleForLevel(level);
                
                // 光の噴水 - 上に勢いよく噴き上げる（メイン）
                Ton.Particle.Register(LIGHT_SPARKLE_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/light_particle",
                    MinLife = 500,
                    MaxLife = 1000,
                    MinSpeed = 12.0f * scale,
                    MaxSpeed = 32.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.1f,  // ほぼ真上
                    MaxAngle = -MathHelper.PiOver2 + 0.1f,
                    MinScale = 0.3f * scale,
                    MaxScale = 0.6f * scale,
                    Gravity = 1.5f,  // 強い重力で落下
                    StartColor = new Color(255, 255, 255),
                    EndColor = new Color(255, 220, 100, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -6.0f,
                    MaxRotationSpeed = 6.0f,
                    HasShadow = true,
                    ShadowColor = new Color(100, 80, 40, 80),
                    ShadowScale = 1.3f
                });

                // 光の飛沫 - 横にも広がる噴水
                Ton.Particle.Register(LIGHT_BURST_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/light_particle",
                    MinLife = 400,
                    MaxLife = 800,
                    MinSpeed = 12.0f * scale,
                    MaxSpeed = 28.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.15f,  // 上方向に扇状
                    MaxAngle = -MathHelper.PiOver2 + 0.15f,
                    MinScale = 0.2f * scale,
                    MaxScale = 0.45f * scale,
                    Gravity = 1.2f,  // 重力で落下
                    StartColor = new Color(255, 255, 200),
                    EndColor = new Color(255, 200, 100, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -8.0f,
                    MaxRotationSpeed = 8.0f
                });

                // 光の粒 - 更に広がる噴水
                Ton.Particle.Register(LIGHT_RAY_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/light_particle",
                    MinLife = 300,
                    MaxLife = 700,
                    MinSpeed = 8.0f * scale,
                    MaxSpeed = 14.0f * scale,
                    MinAngle = -MathHelper.PiOver2 - 0.6f,  // 上方向に広く
                    MaxAngle = -MathHelper.PiOver2 + 0.6f,
                    MinScale = 0.15f * scale,
                    MaxScale = 0.35f * scale,
                    Gravity = 0.8f,  // 重力で落下
                    StartColor = new Color(255, 240, 180),
                    EndColor = new Color(200, 180, 100, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -5.0f,
                    MaxRotationSpeed = 5.0f
                });

                // 回転する光
                Ton.Particle.Register(LIGHT_SWIRL_PREFIX + level, new TonParticleParam
                {
                    ImageName = "shader/image/light_particle",
                    MinLife = 500,
                    MaxLife = 900,
                    MinSpeed = 0f,
                    MaxSpeed = 0f,
                    MinScale = 0.3f * scale,
                    MaxScale = 0.6f * scale,
                    Gravity = 0f,
                    StartColor = new Color(255, 255, 220),
                    EndColor = new Color(255, 200, 120, 0),
                    IsAdditive = true,
                    MinRotationSpeed = -6.0f,
                    MaxRotationSpeed = 6.0f,
                    OrbitalRadius = 45f * scale,
                    OrbitalSpeed = -8.0f,  // 時計回り
                    HasShadow = true,
                    ShadowColor = new Color(100, 80, 40, 60),
                    ShadowScale = 1.2f
                });
            }
        }

        private float GetScaleForLevel(int level)
        {
            return level switch
            {
                1 => 1.0f,
                2 => 1.3f,
                3 => 1.6f,
                4 => 2.0f,
                _ => 1.0f
            };
        }

        private TonMagicEffectParam GetParamForLevel(int level)
        {
            return level switch
            {
                1 => new TonMagicEffectParam
                {
                    ParticleCount = 15,
                    Scale = 1.0f,
                    Duration = 0.8f
                },
                2 => new TonMagicEffectParam
                {
                    ParticleCount = 30,
                    Scale = 1.3f,
                    Duration = 1.0f
                },
                3 => new TonMagicEffectParam
                {
                    ParticleCount = 50,
                    Scale = 1.6f,
                    Duration = 1.2f
                },
                4 => new TonMagicEffectParam
                {
                    ParticleCount = 80,
                    Scale = 2.0f,
                    Duration = 1.5f
                },
                _ => new TonMagicEffectParam()
            };
        }

        /// <summary>
        /// ファイア系魔法エフェクトを発動します。
        /// </summary>
        /// <param name="x">発生X座標</param>
        /// <param name="y">発生Y座標</param>
        /// <param name="level">魔法レベル（1-4）</param>
        public void Fire(float x, float y, int level = 1)
        {
            EnsureInitialized();
            level = Math.Clamp(level, 1, 4);

            var param = GetParamForLevel(level);
            
            var effect = new ActiveEffect
            {
                Type = "Fire",
                X = x,
                Y = y,
                ElapsedTime = 0,
                Duration = param.Duration,
                Param = param,
                Phase = 0,
                PhaseTriggered = false
            };

            _activeEffects.Add(effect);

            // 上昇フェーズのパーティクル発生
            int riseCount = param.ParticleCount / 3;
            Ton.Particle.Play(FIRE_RISE_PREFIX + level, x, y, riseCount);
            
            // 根本で回転する炎
            int swirlCount = param.ParticleCount / 4;
            Ton.Particle.Play(FIRE_SWIRL_PREFIX + level, x, y, swirlCount);
        }

        /// <summary>
        /// ファイア系魔法エフェクトを発動します（列挙体版）。
        /// </summary>
        /// <param name="x">発生X座標</param>
        /// <param name="y">発生Y座標</param>
        /// <param name="level">魔法レベル</param>
        public void Fire(float x, float y, MagicLevel level)
        {
            Fire(x, y, (int)level);
        }

        /// <summary>
        /// ブリザド系魔法エフェクトを発動します。
        /// </summary>
        /// <param name="x">発生X座標</param>
        /// <param name="y">発生Y座標</param>
        /// <param name="level">魔法レベル（1-4）</param>
        public void Ice(float x, float y, int level = 1)
        {
            EnsureInitialized();
            level = Math.Clamp(level, 1, 4);

            var param = GetParamForLevel(level);
            
            var effect = new ActiveEffect
            {
                Type = "Ice",
                X = x,
                Y = y,
                ElapsedTime = 0,
                Duration = param.Duration,
                Param = param,
                Phase = 0,
                PhaseTriggered = false
            };

            _activeEffects.Add(effect);

            // 氷の破片が上から降ってくる
            int shardCount = param.ParticleCount / 3;
            Ton.Particle.Play(ICE_SHARD_PREFIX + level, x, y - 100, shardCount);
            
            // 回転する氷リング
            int swirlCount = param.ParticleCount / 4;
            Ton.Particle.Play(ICE_SWIRL_PREFIX + level, x, y, swirlCount);
        }

        /// <summary>
        /// ブリザド系魔法エフェクトを発動します（列挙体版）。
        /// </summary>
        public void Ice(float x, float y, MagicLevel level)
        {
            Ice(x, y, (int)level);
        }

        /// <summary>
        /// エアロ系魔法エフェクトを発動します。
        /// </summary>
        /// <param name="x">発生X座標</param>
        /// <param name="y">発生Y座標</param>
        /// <param name="level">魔法レベル（1-4）</param>
        public void Wind(float x, float y, int level = 1)
        {
            EnsureInitialized();
            level = Math.Clamp(level, 1, 4);

            var param = GetParamForLevel(level);
            
            var effect = new ActiveEffect
            {
                Type = "Wind",
                X = x,
                Y = y,
                ElapsedTime = 0,
                Duration = param.Duration,
                Param = param,
                Phase = 0,
                PhaseTriggered = false
            };

            _activeEffects.Add(effect);

            // 渦巻きパーティクル（メイン）
            int swirlCount = param.ParticleCount / 3;
            Ton.Particle.Play(WIND_SWIRL_PREFIX + level, x, y, swirlCount);
            
            // 突風パーティクル
            int gustCount = param.ParticleCount / 4;
            Ton.Particle.Play(WIND_GUST_PREFIX + level, x, y, gustCount);
        }

        /// <summary>
        /// エアロ系魔法エフェクトを発動します（列挙体版）。
        /// </summary>
        public void Wind(float x, float y, MagicLevel level)
        {
            Wind(x, y, (int)level);
        }

        /// <summary>
        /// クエイク系魔法エフェクトを発動します。
        /// </summary>
        /// <param name="x">発生X座標</param>
        /// <param name="y">発生Y座標</param>
        /// <param name="level">魔法レベル（1-4）</param>
        public void Earth(float x, float y, int level = 1)
        {
            EnsureInitialized();
            level = Math.Clamp(level, 1, 4);

            var param = GetParamForLevel(level);
            
            var effect = new ActiveEffect
            {
                Type = "Earth",
                X = x,
                Y = y,
                ElapsedTime = 0,
                Duration = param.Duration,
                Param = param,
                Phase = 0,
                PhaseTriggered = false
            };

            _activeEffects.Add(effect);

            // 岩の破片が飛び散る
            int rockCount = param.ParticleCount / 3;
            Ton.Particle.Play(EARTH_ROCK_PREFIX + level, x, y, rockCount);
            
            // 土埃
            int dustCount = param.ParticleCount / 4;
            Ton.Particle.Play(EARTH_DUST_PREFIX + level, x, y, dustCount);
            
            // 回転する岩
            int swirlCount = param.ParticleCount / 4;
            Ton.Particle.Play(EARTH_SWIRL_PREFIX + level, x, y, swirlCount);
        }

        /// <summary>
        /// クエイク系魔法エフェクトを発動します（列挙体版）。
        /// </summary>
        public void Earth(float x, float y, MagicLevel level)
        {
            Earth(x, y, (int)level);
        }

        /// <summary>
        /// ケアル系魔法エフェクトを発動します。
        /// </summary>
        /// <param name="x">発生X座標</param>
        /// <param name="y">発生Y座標</param>
        /// <param name="level">魔法レベル（1-4）</param>
        public void Heal(float x, float y, int level = 1)
        {
            EnsureInitialized();
            level = Math.Clamp(level, 1, 4);

            var param = GetParamForLevel(level);
            
            var effect = new ActiveEffect
            {
                Type = "Heal",
                X = x,
                Y = y,
                ElapsedTime = 0,
                Duration = param.Duration,
                Param = param,
                Phase = 0,
                PhaseTriggered = false
            };

            _activeEffects.Add(effect);

            // きらめき
            int sparkleCount = param.ParticleCount / 3;
            Ton.Particle.Play(HEAL_SPARKLE_PREFIX + level, x, y, sparkleCount);
            
            // 回転光
            int swirlCount = param.ParticleCount / 4;
            Ton.Particle.Play(HEAL_SWIRL_PREFIX + level, x, y, swirlCount);
        }

        /// <summary>
        /// ケアル系魔法エフェクトを発動します（列挙体版）。
        /// </summary>
        public void Heal(float x, float y, MagicLevel level)
        {
            Heal(x, y, (int)level);
        }

        /// <summary>
        /// ポイズン系魔法エフェクトを発動します。
        /// 毒が周囲をうようよぐるぐる回る演出です。
        /// </summary>
        /// <param name="x">発生X座標</param>
        /// <param name="y">発生Y座標</param>
        /// <param name="level">魔法レベル（1-4）</param>
        public void Poison(float x, float y, int level = 1)
        {
            EnsureInitialized();
            level = Math.Clamp(level, 1, 4);

            var param = GetParamForLevel(level);
            
            var effect = new ActiveEffect
            {
                Type = "Poison",
                X = x,
                Y = y,
                ElapsedTime = 0,
                Duration = param.Duration,
                Param = param,
                Phase = 0,
                PhaseTriggered = false
            };

            _activeEffects.Add(effect);

            // 内側の渦巻き
            int innerCount = param.ParticleCount / 4;
            Ton.Particle.Play(POISON_DROP_PREFIX + level, x, y, innerCount);
            
            // 中間の渦巻き
            int midCount = param.ParticleCount / 4;
            Ton.Particle.Play(POISON_BUBBLE_PREFIX + level, x, y, midCount);
            
            // 外側の渦巻き
            int outerCount = param.ParticleCount / 4;
            Ton.Particle.Play(POISON_SPLASH_PREFIX + level, x, y, outerCount);
            
            // 最外周の渦巻き
            int swirlCount = param.ParticleCount / 4;
            Ton.Particle.Play(POISON_SWIRL_PREFIX + level, x, y, swirlCount);
        }

        /// <summary>
        /// ポイズン系魔法エフェクトを発動します（列挙体版）。
        /// </summary>
        public void Poison(float x, float y, MagicLevel level)
        {
            Poison(x, y, (int)level);
        }

        /// <summary>
        /// ホーリー（光）系魔法エフェクトを発動します。
        /// 噴水のように光が噴き上がる演出です。
        /// </summary>
        /// <param name="x">発生X座標</param>
        /// <param name="y">発生Y座標</param>
        /// <param name="level">魔法レベル（1-4）</param>
        public void Light(float x, float y, int level = 1)
        {
            EnsureInitialized();
            level = Math.Clamp(level, 1, 4);

            var param = GetParamForLevel(level);
            
            var effect = new ActiveEffect
            {
                Type = "Light",
                X = x,
                Y = y,
                ElapsedTime = 0,
                Duration = param.Duration,
                Param = param,
                Phase = 0,
                PhaseTriggered = false
            };

            _activeEffects.Add(effect);

            // メインの噴水
            int mainCount = param.ParticleCount / 3;
            Ton.Particle.Play(LIGHT_SPARKLE_PREFIX + level, x, y, mainCount);
            
            // 横に広がる噴水
            int burstCount = param.ParticleCount / 3;
            Ton.Particle.Play(LIGHT_BURST_PREFIX + level, x, y, burstCount);
        }

        /// <summary>
        /// ホーリー（光）系魔法エフェクトを発動します（列挙体版）。
        /// </summary>
        public void Light(float x, float y, MagicLevel level)
        {
            Light(x, y, (int)level);
        }

        /// <summary>
        /// エフェクトの状態を更新します。
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (_activeEffects.Count == 0) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.ElapsedTime += dt;

                float progress = effect.ElapsedTime / effect.Duration;

                // フェーズ遷移
                if (progress >= 0.0f && effect.Phase == 0)
                {
                    // 爆発フェーズへ移行
                    effect.Phase = 1;
                    effect.PhaseTriggered = false;
                }
                else if (progress >= 0.15f && effect.Phase == 1)
                {
                    // 残り火フェーズへ移行
                    effect.Phase = 2;
                    effect.PhaseTriggered = false;
                }

                // フェーズごとのパーティクル発生
                if (!effect.PhaseTriggered)
                {
                    int level = GetLevelFromParam(effect.Param);
                    
                    if (effect.Type == "Fire")
                    {
                        switch (effect.Phase)
                        {
                            case 1: // 爆発フェーズ
                                int burstCount = effect.Param.ParticleCount / 2;
                                int coreCount = effect.Param.ParticleCount / 4;
                                Ton.Particle.Play(FIRE_BURST_PREFIX + level, effect.X, effect.Y, burstCount);
                                Ton.Particle.Play(FIRE_CORE_PREFIX + level, effect.X, effect.Y, coreCount);
                                break;
                                
                            case 2: // 残り火フェーズ
                                int emberCount = effect.Param.ParticleCount / 4;
                                Ton.Particle.Play(FIRE_EMBER_PREFIX + level, effect.X, effect.Y, emberCount);
                                break;
                        }
                    }
                    else if (effect.Type == "Ice")
                    {
                        switch (effect.Phase)
                        {
                            case 1: // 爆発フェーズ - 放射状に氷が広がる
                                int iceBurstCount = effect.Param.ParticleCount / 2;
                                Ton.Particle.Play(ICE_BURST_PREFIX + level, effect.X, effect.Y, iceBurstCount);
                                break;
                                
                            case 2: // 霜フェーズ - 霜の結晶が漂う
                                int frostCount = effect.Param.ParticleCount / 3;
                                Ton.Particle.Play(ICE_FROST_PREFIX + level, effect.X, effect.Y, frostCount);
                                break;
                        }
                    }
                    else if (effect.Type == "Wind")
                    {
                        switch (effect.Phase)
                        {
                            case 1: // 強風フェーズ - 追加の渦巻き
                                int windSwirlCount = effect.Param.ParticleCount / 3;
                                Ton.Particle.Play(WIND_SWIRL_PREFIX + level, effect.X, effect.Y, windSwirlCount);
                                break;
                                
                            case 2: // 舞う葉フェーズ
                                int leafCount = effect.Param.ParticleCount / 3;
                                Ton.Particle.Play(WIND_LEAF_PREFIX + level, effect.X, effect.Y, leafCount);
                                break;
                        }
                    }
                    else if (effect.Type == "Earth")
                    {
                        switch (effect.Phase)
                        {
                            case 1: // 爆発フェーズ - 岩が放射状に飛び散る
                                int earthBurstCount = effect.Param.ParticleCount / 2;
                                Ton.Particle.Play(EARTH_BURST_PREFIX + level, effect.X, effect.Y, earthBurstCount);
                                break;
                                
                            case 2: // 土埃フェーズ - 追加の土埃
                                int dustCount = effect.Param.ParticleCount / 3;
                                Ton.Particle.Play(EARTH_DUST_PREFIX + level, effect.X, effect.Y, dustCount);
                                break;
                        }
                    }
                    else if (effect.Type == "Heal")
                    {
                        switch (effect.Phase)
                        {
                            case 1: // 上昇光フェーズ
                                int riseCount = effect.Param.ParticleCount / 3;
                                Ton.Particle.Play(HEAL_RISE_PREFIX + level, effect.X, effect.Y, riseCount);
                                break;
                                
                            case 2: // 追加きらめきフェーズ
                                int sparkleCount = effect.Param.ParticleCount / 4;
                                Ton.Particle.Play(HEAL_SPARKLE_PREFIX + level, effect.X, effect.Y, sparkleCount);
                                break;
                        }
                    }
                    else if (effect.Type == "Poison")
                    {
                        switch (effect.Phase)
                        {
                            case 1: // 追加の渦巻き
                                int innerCount = effect.Param.ParticleCount / 4;
                                Ton.Particle.Play(POISON_DROP_PREFIX + level, effect.X, effect.Y, innerCount);
                                int midCount = effect.Param.ParticleCount / 4;
                                Ton.Particle.Play(POISON_BUBBLE_PREFIX + level, effect.X, effect.Y, midCount);
                                break;
                                
                            case 2: // さらに追加の渦巻き
                                int outerCount = effect.Param.ParticleCount / 4;
                                Ton.Particle.Play(POISON_SPLASH_PREFIX + level, effect.X, effect.Y, outerCount);
                                int swirlCount2 = effect.Param.ParticleCount / 4;
                                Ton.Particle.Play(POISON_SWIRL_PREFIX + level, effect.X, effect.Y, swirlCount2);
                                break;
                        }
                    }
                    else if (effect.Type == "Light")
                    {
                        switch (effect.Phase)
                        {
                            case 1: // 追加の噴水
                                int rayCount = effect.Param.ParticleCount / 3;
                                Ton.Particle.Play(LIGHT_RAY_PREFIX + level, effect.X, effect.Y, rayCount);
                                break;
                                
                            case 2: // さらに噴水
                                int burstCount = effect.Param.ParticleCount / 4;
                                Ton.Particle.Play(LIGHT_BURST_PREFIX + level, effect.X, effect.Y, burstCount);
                                int sparkleCount = effect.Param.ParticleCount / 4;
                                Ton.Particle.Play(LIGHT_SPARKLE_PREFIX + level, effect.X, effect.Y, sparkleCount);
                                break;
                        }
                    }
                    
                    effect.PhaseTriggered = true;
                }

                // エフェクト終了判定
                if (effect.ElapsedTime >= effect.Duration)
                {
                    _activeEffects.RemoveAt(i);
                }
            }
        }

        private int GetLevelFromParam(TonMagicEffectParam param)
        {
            // パーティクル数からレベルを逆算
            if (param.ParticleCount >= 80) return 4;
            if (param.ParticleCount >= 50) return 3;
            if (param.ParticleCount >= 30) return 2;
            return 1;
        }

        /// <summary>
        /// 実行中のエフェクト数を取得します。
        /// </summary>
        public int ActiveCount => _activeEffects.Count;

        /// <summary>
        /// すべてのアクティブエフェクトをクリアします。
        /// </summary>
        public void Clear()
        {
            _activeEffects.Clear();
        }
    }
}
