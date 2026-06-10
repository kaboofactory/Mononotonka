using System;
using Microsoft.Xna.Framework;

namespace Mononotonka
{

    /// <summary>
    /// イージング関数の種類を表す列挙型。
    /// </summary>
    public enum EasingType
    {
        Linear,
        Quad,
        Cubic,
        Quart,
        Quint,
        Sine,
        Expo,
        Back,
        Circ,
        Elastic,
        Bounce
    }

    /// <summary>
    /// イージングの方向（モード）を表す列挙型。
    /// </summary>
    public enum EasingMode
    {
        In,
        Out,
        InOut
    }

    /// <summary>
    /// 数学・乱数関連のユーティリティクラスです。
    /// </summary>
    public class TonMath
    {
        private static Random _random = new Random();

        /// <summary>
        /// 指定範囲 [min, max) のランダムな整数を取得します。
        /// </summary>
        /// <param name="min">最小値（含む）</param>
        /// <param name="max">最大値（含まない）</param>
        /// <returns>ランダムな整数</returns>
        public int Rand(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// 指定範囲 [min, max) のランダムな実数(float)を取得します。
        /// </summary>
        /// <param name="min">最小値</param>
        /// <param name="max">最大値</param>
        /// <returns>ランダムな実数</returns>
        public float RandF(float min, float max)
        {
            return (float)(min + _random.NextDouble() * (max - min));
        }

        /// <summary>
        /// 2点間の角度(ラジアン)を計算します。
        /// </summary>
        public float GetAngle(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Atan2(y2 - y1, x2 - x1);
        }

        /// <summary>
        /// 2点間の距離を計算します。
        /// </summary>
        public float GetDistance(float x1, float y1, float x2, float y2)
        {
            return Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
        }

        /// <summary>
        /// 線形補間(Lerp)を行います。
        /// </summary>
        /// <param name="current">現在値</param>
        /// <param name="target">目標値</param>
        /// <param name="amount">補間係数(0.0～1.0)</param>
        /// <returns>補間された値</returns>
        public float Lerp(float current, float target, float amount)
        {
            return MathHelper.Lerp(current, target, amount);
        }

        /// <summary>
        /// 矩形同士の衝突判定を行います。
        /// </summary>
        public bool HitCheckRect(Rectangle rect1, Rectangle rect2)
        {
            return rect1.Intersects(rect2);
        }

        /// <summary>
        /// 円同士の衝突判定を行います。
        /// </summary>
        public bool HitCheckCircle(Vector2 pos1, float r1, Vector2 pos2, float r2)
        {
            float distSq = Vector2.DistanceSquared(pos1, pos2);
            float radiusSum = r1 + r2;
            return distSq <= (radiusSum * radiusSum);
        }

        /// <summary>
        /// 指定座標が矩形の中に入っているか判定します。
        /// </summary>
        public bool IsPointInRect(float x, float y, Rectangle rect)
        {
            return rect.Contains((int)x, (int)y);
        }

        /// <summary>
        /// 指定したイージングタイプとモードで時間割合 (0.0〜1.0) を補間した値を返します。
        /// (このイージング関数群は Robert Penner 氏のイージング方程式に基づいています)
        /// </summary>
        /// <param name="t">時間割合 (0.0～1.0)</param>
        /// <param name="type">イージングの種類</param>
        /// <param name="mode">イージングの方向（モード）</param>
        /// <returns>補間された値 (0.0～1.0)</returns>
        public float Ease(float t, EasingType type, EasingMode mode = EasingMode.In)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            if (type == EasingType.Linear)
            {
                return t;
            }

            switch (mode)
            {
                case EasingMode.In:
                    return EaseIn(t, type);
                case EasingMode.Out:
                    return 1f - EaseIn(1f - t, type);
                case EasingMode.InOut:
                    return t < 0.5f
                        ? EaseIn(t * 2f, type) * 0.5f
                        : 1f - EaseIn((1f - t) * 2f, type) * 0.5f;
                default:
                    return t;
            }
        }

        private static float EaseIn(float t, EasingType type)
        {
            switch (type)
            {
                case EasingType.Quad: return t * t;
                case EasingType.Cubic: return t * t * t;
                case EasingType.Quart: return t * t * t * t;
                case EasingType.Quint: return t * t * t * t * t;
                case EasingType.Sine: return 1f - MathF.Cos(t * MathF.PI * 0.5f);
                case EasingType.Expo: return t == 0f ? 0f : MathF.Pow(2f, 10f * (t - 1f));
                case EasingType.Back:
                    {
                        const float c1 = 1.70158f;
                        const float c3 = c1 + 1f;
                        return c3 * t * t * t - c1 * t * t;
                    }
                case EasingType.Circ:
                    return 1f - MathF.Sqrt(1f - t * t);
                case EasingType.Elastic:
                    return t == 0f ? 0f : (t == 1f ? 1f : -MathF.Pow(2f, 10f * t - 10f) * MathF.Sin((t * 10f - 10.75f) * ((2f * MathF.PI) / 3f)));
                case EasingType.Bounce:
                    return 1f - EaseOutBounce(1f - t);
                default: return t;
            }
        }

        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / d1;
                return n1 * t * t + 0.984375f;
            }
        }
    }

}

