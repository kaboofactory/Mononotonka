using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// アニメーションの並び方向
    /// </summary>
    public enum AnimDirection { 
        /// <summary>左から右</summary>
        LeftToRight, 
        /// <summary>上から下</summary>
        TopToBottom 
    }

    /// <summary>
    /// スクリーンフィルターの種類
    /// </summary>
    public enum ScreenFilterType
    {
        /// <summary>なし</summary>
        None,
        /// <summary>グレースケール</summary>
        Greyscale,
        /// <summary>セピア</summary>
        Sepia,
        /// <summary>走査線</summary>
        ScanLine,
        /// <summary>モザイク</summary>
        Mosaic,
        /// <summary>ブラー（ぼかし）</summary>
        Blur,
        /// <summary>色収差</summary>
        ChromaticAberration,
        /// <summary>ビネット</summary>
        Vignette,
        /// <summary>階調反転</summary>
        Invert,
        /// <summary>歪み（揺らぎ）</summary>
        Distortion,
        /// <summary>ノイズ</summary>
        Noise,
        /// <summary>エッジ検出</summary>
        EdgeDetect,
        /// <summary>ラジアルブラー</summary>
        RadialBlur,
        /// <summary>ポスタライズ</summary>
        Posterize,
        /// <summary>魚眼レンズ</summary>
        FishEye
    }

    /// <summary>
    /// アニメーション状態を管理するクラスです。
    /// 現在のフレーム、経過時間、フレーム矩形の計算などを担当します。
    /// </summary>
    public class TonAnimState
    {
        /// <summary>ループ再生するかどうか</summary>
        public bool IsLoop = true;
        /// <summary>総フレーム数</summary>
        public int FrameCount = 1;
        /// <summary>1フレームの表示時間(ms)</summary>
        public int FrameDuration = 100; // ms
        /// <summary>アニメーション画像の並び方向</summary>
        public AnimDirection direction = AnimDirection.LeftToRight;

        // 状態（外部からの変更不要）
        /// <summary>現在の経過時間(秒)</summary>
        public float Timer { get; private set; } = 0f;
        /// <summary>現在のフレーム番号</summary>
        public int CurrentFrame { get; private set; } = 0;
        /// <summary>終了後の経過時間(秒)</summary>
        public float TimeAfterFinished { get; private set; } = 0f;

        /// <summary>切り出し開始X座標</summary>
        public int x1;
        /// <summary>切り出し開始Y座標</summary>
        public int y1;
        /// <summary>1フレームの幅</summary>
        public int width;
        /// <summary>1フレームの高さ</summary>
        public int height;

        /// <summary>
        /// アニメーションが終了しているか（非ループ時のみ有効）
        /// </summary>
        public bool IsFinished => TimeAfterFinished > 0;

        /// <summary>
        /// 状態をリセットします
        /// </summary>
        public void Reset()
        {
            Timer = 0f;
            CurrentFrame = 0;
            TimeAfterFinished = 0f;
        }

        /// <summary>
        /// 指定時刻に基づいてループアニメーション状態を生成します（Update不要）。
        /// Draw内で都度呼び出すことで、ステートレスにアニメーションを描画できます。
        /// </summary>
        public static TonAnimState CreateLoop(int x1, int y1, int width, int height, int frameCount, int durationMs, double totalSeconds)
        {
            var anim = new TonAnimState
            {
                x1 = x1,
                y1 = y1,
                width = width,
                height = height,
                FrameCount = frameCount,
                FrameDuration = durationMs,
                IsLoop = true
            };

            float durationSec = durationMs / 1000f;
            if (durationSec > 0)
            {
                long totalFrames = (long)(totalSeconds / durationSec);
                anim.CurrentFrame = (int)(totalFrames % frameCount);
            }

            return anim;
        }

        /// <summary>
        /// 現在のフレームに対応するソース矩形（テクスチャ上の切り取り範囲）を取得します。
        /// </summary>
        public Rectangle GetSourceRect()
        {
            int dx = 0, dy = 0;
            if (direction == AnimDirection.LeftToRight) dx = CurrentFrame * width;
            else dy = CurrentFrame * height;
            return new Rectangle(x1 + dx, y1 + dy, width, height);
        }

        /// <summary>
        /// アニメーションを更新します。
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float durationSec = FrameDuration / 1000f;
            if (durationSec <= 0f) return;

            // アニメーション終了済みの場合
            if (IsFinished)
            {
                TimeAfterFinished += (float)gameTime.ElapsedGameTime.TotalSeconds;
                return;
            }

            Timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            while (Timer >= durationSec)
            {
                Timer -= durationSec;
                
                // 次のフレームへ
                if (IsLoop)
                {
                    CurrentFrame = (CurrentFrame + 1) % FrameCount;
                }
                else
                {
                    if (CurrentFrame < FrameCount - 1)
                    {
                        CurrentFrame++;
                    }
                    else
                    {
                        // 最後のフレームで時間経過 -> 終了状態へ
                        // Timerに残った時間はTimeAfterFinishedに引き継ぐ
                        TimeAfterFinished = Timer; 
                        Timer = 0; // Timerは0に戻す（あるいは最後のフレームのままにする？）
                                   // ここでは終了後はTimer=0, TimeAfterFinished > 0 とする
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 基本的な描画パラメータクラスです。
    /// 色、透明度、反転などを指定します。
    /// </summary>
    public class TonDrawParam
    {
        /// <summary>不透明度(0.0-1.0)</summary>
        public float Alpha = 1.0f;
        /// <summary>乗算色</summary>
        public Color Color = Color.White;
        /// <summary>左右反転</summary>
        public bool FlipH = false;
        /// <summary>上下反転</summary>
        public bool FlipV = false;

        public TonDrawParam() { }
        public TonDrawParam(Color color) { Color = color; }
    }

    /// <summary>
    /// 拡張描画パラメータクラスです。
    /// 拡大縮小、回転、モザイクなどの追加効果を指定できます。
    /// </summary>
    public class TonDrawParamEx
    {
        /// <summary>X軸方向スケール</summary>
        public float ScaleX = 1.0f;
        /// <summary>Y軸方向スケール</summary>
        public float ScaleY = 1.0f;
        /// <summary>回転角度(ラジアン)</summary>
        public float Angle = 0.0f;
        /// <summary>不透明度(0.0-1.0)</summary>
        public float Alpha = 1.0f;
        /// <summary>乗算色</summary>
        public Color Color = Color.White;
        /// <summary>左右反転</summary>
        public bool FlipH = false;
        /// <summary>上下反転</summary>
        public bool FlipV = false;
        /// <summary>モザイクサイズ(未実装)</summary>
        public float MosaicSize = 0.0f;

        public TonDrawParamEx() { }
        public TonDrawParamEx(float scale) { ScaleX = scale; ScaleY = scale; }
        public TonDrawParamEx(float scale, float angle) { ScaleX = scale; ScaleY = scale; Angle = angle; }
        public TonDrawParamEx(float scale, float angle, Color color) { ScaleX = scale; ScaleY = scale; Angle = angle; Color = color; }
    }

    /// <summary>
    /// 画面フィルター用のパラメータクラスです。
    /// </summary>
    public class TonFilterParam
    {
        /// <summary>フィルターの種類</summary>
        public ScreenFilterType Type;
        /// <summary>適用強度(0.0-1.0)</summary>
        public float Amount;

        public TonFilterParam(ScreenFilterType type, float amount = 1.0f)
        {
            Type = type;
            Amount = amount;
        }
    }

    /// <summary>
    /// ブレンドステート（合成モード）のラッパークラスです。
    /// </summary>
    public class TonBlendState
    {
        public Microsoft.Xna.Framework.Graphics.BlendState State { get; private set; }

        public TonBlendState(Microsoft.Xna.Framework.Graphics.BlendState state)
        {
            State = state;
        }

        /// <summary>通常合成（アルファブレンド）</summary>
        public static readonly TonBlendState AlphaBlend = new TonBlendState(Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend);
        /// <summary>加算合成</summary>
        public static readonly TonBlendState Additive = new TonBlendState(Microsoft.Xna.Framework.Graphics.BlendState.Additive);
        /// <summary>乗算済みアルファを使用しない合成</summary>
        public static readonly TonBlendState NonPremultiplied = new TonBlendState(Microsoft.Xna.Framework.Graphics.BlendState.NonPremultiplied);
        /// <summary>不透明合成（上書き）</summary>
        public static readonly TonBlendState Opaque = new TonBlendState(Microsoft.Xna.Framework.Graphics.BlendState.Opaque);
    }
}
