using System;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
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
    }
}
