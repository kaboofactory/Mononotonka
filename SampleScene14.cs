using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Mononotonka
{
    public class SampleScene14 : IScene
    {
        private List<Vector2> _points = new List<Vector2>();
        private float _time = 0;
        private Vector2 _targetPos = new Vector2(600, 260);

        public void Initialize()
        {
            // 初期の点
            _points.Add(new Vector2(100, 260));
            _points.Add(new Vector2(300, 100));
            _points.Add(new Vector2(500, 400));
            _points.Add(_targetPos);
        }

        public void Update(GameTime gameTime)
        {
            _time += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ターゲットを動かす
            _targetPos.X = 600 + (float)Math.Sin(_time * 2.0f) * 150;
            _targetPos.Y = 360 + (float)Math.Cos(_time * 1.5f) * 150;

            // ポイントリスト更新
            // 必要に応じて動的計算
            _points.Clear();
            _points.Add(new Vector2(100, 360));
            
            // 中間の点をベジェっぽく動かす
            _points.Add(new Vector2(300 + (float)Math.Sin(_time) * 50, 200));
            _points.Add(new Vector2(500, 500 + (float)Math.Cos(_time) * 50));
            
            _points.Add(_targetPos);

            // Aボタンで戻る
            if (Ton.Input.GetPressedDuration("A") > 1.0f)
            {
                Ton.Scene.Change(new SampleScene15(), 0.5f, 0.2f, Color.Gold);
            }
        }

        public void Draw()
        {
            // 背景
            Ton.Gra.DrawBackground("landscape");

            // テクスチャ描画
            Ton.Gra.DrawText("Primitive Draw Test", 20, 20, 0.7f);

            // 矢印描画

            // 赤い矢印（縁取りあり）
            Ton.Primitive.DrawArrow(_points, 10f, Color.Red, 30f, Color.White, 3f);

            // 座標チェック
            foreach (var pt in _points)
            {
                Ton.Primitive.DrawCircle(pt, 3f, Color.White, 12);
            }

            // 緑のスプライン曲線（矢印なし）
            var greenPoints = new List<Vector2>();
            for(int i=0; i<8; i++)
            {
                greenPoints.Add(new Vector2(50 + i * 100, 400 + (float)Math.Sin(_time * 2 + i) * 100));
            }

            // 海の中（領域塗りつぶし）を描画
            // 半透明のグラデーション（上：水色、下：濃い青）
            Ton.Primitive.DrawSplineArea(greenPoints, 720, Color.Aquamarine * 0.8f, Color.DarkBlue * 1.0f);

            // スプラインの縁取りを表現したい場合は2回描画する
            Ton.Primitive.DrawSpline(greenPoints, 10f, Color.DarkGreen); // 縁（太い線）
            Ton.Primitive.DrawSpline(greenPoints, 6f, Color.LightGreen); // 本体（細い線）

            // 座標チェック
            foreach(var pt in greenPoints)
            {
                Ton.Primitive.DrawCircle(pt, 3f, Color.White, 12);
            }

            // トレース移動テスト
            // 時間に応じて t を変化させる (0 ～ 7 の間を往復)
            float t = (_time * 2.0f) % (greenPoints.Count - 1); // 一方向ループ
            // 往復させたい場合: float t = (float)(Math.Sin(_time) + 1.0) / 2.0f * (greenPoints.Count - 1);
            
            Vector2 tracePos;
            float traceRot;
            Ton.Primitive.GetSplineInfo(greenPoints, t, out tracePos, out traceRot);

            // トレースするアイコン描画 (回転させて表示)
            
            var drawParam = new TonDrawParamEx
            {
                Angle = traceRot,
                ScaleX = 1.0f,
                ScaleY = 1.0f
            };
            
            // アイコンのサイズを取得して中心に描画されるようにする
            int w = Ton.Gra.GetTextureWidth("icon");
            int h = Ton.Gra.GetTextureHeight("icon");
            
            // サイズが取得できれば描画 (GetTextureWidthはテクスチャが見つからない場合、フォールバックのサイズを返す可能性があるが、0の場合は描画しない)
            if (w > 0 && h > 0)
            {
                Ton.Gra.DrawEx("icon", tracePos.X, tracePos.Y, 0, 0, w, h, drawParam);
            }
            else
            {
                // テクスチャが無い場合（あるいは読み込み失敗時）は矢印で代用
                Ton.Primitive.DrawArrow(new List<Vector2> { tracePos, tracePos + new Vector2((float)Math.Cos(traceRot), (float)Math.Sin(traceRot)) * 40f }, 
                                    2f, Color.Magenta, 10f);
            }

            // 2. プリミティブ形状デモ（円・矩形）
            float cx = 800;
            float cy = 250;

            // 円（真円）
            Ton.Primitive.DrawCircle(new Vector2(cx, cy), 40f, Color.Yellow, 36);
            
            // 楕円（伸縮）
            float rX = 40f + (float)Math.Sin(_time * 4) * 10f;
            float rY = 30f + (float)Math.Cos(_time * 4) * 10f;
            Ton.Primitive.DrawCircle(new Vector2(cx + 100, cy), rX, rY, Color.Pink, 36);

            // 矩形（回転）
            // 中心基準で回転させる
            Ton.Primitive.DrawRectangle(new Vector2(cx, cy + 100), new Vector2(66, 46), Color.Navy, _time * 2.0f, null);
            
            // 矩形（左上基準で回転? - Origin指定）
            // ここではOriginを(0,0)にすることで左上基準回転に見せる
            Ton.Primitive.DrawRectangle(new Vector2(cx + 100, cy + 100), new Vector2(44, 44), Color.Green, _time * -2.0f, Vector2.Zero);

            // 4. 新機能デモ（扇形・破線）
            float cx2 = 200;
            float cy2 = 600;

            // 扇形 (視界)
            float lookAngle = (float)Math.Sin(_time) * 1.0f - 1.57f; // 上向きを中心に左右に振る
            Ton.Primitive.DrawSector(new Vector2(cx2, cy2), 100f, lookAngle - 0.5f, lookAngle + 0.5f, Color.Red * 0.3f, 24);
            
            // 扇形 (クールダウン・リング状)
            float fill = (_time * 0.5f) % 1.0f;
            float endA = -1.57f + fill * 6.28f;
            Ton.Primitive.DrawSector(new Vector2(cx2 + 150, cy2), 50f, -1.57f, endA, Color.Cyan * 0.5f, 36, 25f);

            // 破線スプライン (軌跡)
            var dashPoints = new List<Vector2>
            {
                new Vector2(400, 600),
                new Vector2(500, 550),
                new Vector2(600, 650),
                new Vector2(700, 600)
            };
            Ton.Primitive.DrawSplineDashed(dashPoints, 4f, Color.White, 20f, 10f);

            // 破線矢印 (ガイド)
            var arrowPoints = new List<Vector2>
            {
                new Vector2(950, 550),
                new Vector2(850, 500),
                new Vector2(800, 600)
            };
            // 破線間隔を変化させてみる
            float dashLen = 15f + (float)Math.Sin(_time * 5) * 5f;
            Ton.Primitive.DrawArrowDashed(arrowPoints, 4f, Color.Yellow, 20f, dashLen, 10f, Color.Orange, 2f);

            // 5. 追加プリミティブデモ (Triangle, Polygon, Arc, RoundedRect)
            float cx3 = 500;
            float cy3 = 100;

            // 三角形
            Ton.Primitive.DrawTriangle(
                new Vector2(cx3, cy3 - 30),
                new Vector2(cx3 - 30, cy3 + 20),
                new Vector2(cx3 + 30, cy3 + 20),
                Color.Violet
            );

            // 多角形 (五角形)
            List<Vector2> polyVerts = new List<Vector2>();
            for (int i = 0; i < 5; i++)
            {
                float angle = i * MathHelper.TwoPi / 5 - MathHelper.PiOver2;
                polyVerts.Add(new Vector2(cx3 + 100, cy3) + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 40f);
            }
            Ton.Primitive.DrawPolygon(polyVerts, Color.Turquoise);

            // 円弧 (HPゲージ風)
            float hpRatio = (float)(Math.Sin(_time) + 1.0) / 2.0f; // 0.0-1.0
            float startA = -MathHelper.Pi * 0.8f;
            // float endA = MathHelper.Pi * 0.8f; // 重複変数を避けるため名前変更
            float endA_Arc = MathHelper.Pi * 0.8f;
            float currentA = startA + (endA_Arc - startA) * hpRatio;
            
            // 背景（暗い色）
            Ton.Primitive.DrawArc(new Vector2(cx3 + 200, cy3), 40f, startA, endA_Arc, 8f, Color.Gray * 0.5f, 24);
            // 本体（明るい色）
            Ton.Primitive.DrawArc(new Vector2(cx3 + 200, cy3), 40f, startA, currentA, 8f, Color.Lime, 24);

            // 角丸矩形 (回転)
            Ton.Primitive.DrawRoundedRectangle(
                new Vector2(cx3 + 300, cy3),
                new Vector2(80, 50),
                15f,
                Color.HotPink,
                _time, // 回転
                null
            );

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(Ton.Input.GetPressedDuration("A") * 400.0f), 160, 0.6f + (float)Ton.Input.GetPressedDuration("A"));
        }

        public void Terminate()
        {
        }
    }
}
