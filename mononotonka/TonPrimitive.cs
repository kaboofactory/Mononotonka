using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Mononotonka
{
    /// <summary>
    /// プリミティブ（幾何学図形）の描画を行うクラスです。
    /// DrawUserPrimitivesを使用するため、SpriteBatchとは別の描画パスになります。
    /// </summary>
    public class TonPrimitive
    {
        private Game _game;
        private GraphicsDeviceManager _graphics;
        private BasicEffect _basicEffect;

        public void Initialize(Game game, GraphicsDeviceManager graphics)
        {
            _game = game;
            _graphics = graphics;

            _basicEffect = new BasicEffect(game.GraphicsDevice);
            _basicEffect.VertexColorEnabled = true;
            _basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
                0, Ton.Game.VirtualWidth,
                Ton.Game.VirtualHeight, 0,
                0, 1
            );
        }

        /// <summary>
        /// 描画に使用するBasicEffectの射影行列を更新します。
        /// <para>
        /// 画面サイズ（仮想解像度）に合わせて正射影行列を設定します。
        /// 描画開始前に毎回呼び出すことで、画面リサイズ等に対応します。
        /// </para>
        /// </summary>
        private void UpdateProjection()
        {
            if (_basicEffect != null)
            {
                _basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
                    0, Ton.Game.VirtualWidth,
                    Ton.Game.VirtualHeight, 0,
                    0, 1
                );
            }
        }

        /// <summary>
        /// 複数の点を通るスプライン曲線を描画します。
        /// <para>
        /// キャットムル＝ロムスプライン (Catmull-Rom Spline) 補間を用いて、
        /// 指定された点を滑らかに通る曲線を生成し、描画します。
        /// </para>
        /// </summary>
        /// <param name="points">曲線が通過する制御点のリスト。</param>
        /// <param name="thickness">線の太さ。</param>
        /// <param name="color">線の色。</param>
        public void DrawSpline(List<Vector2> points, float thickness, Color color)
        {
            if (points == null || points.Count < 2) return;

            // 補間された点リストを生成
            // GenerateSmoothCurve内部でCatmull-Rom補間を行い、滑らかな点列を計算します。
            List<Vector2> smoothPoints = GenerateSmoothCurve(points, 20); // 1区間あたり20分割して滑らかにする

            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();
                
                // 本体の線を描画
                DrawPolyLine(smoothPoints, thickness, color);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 複数の点を通る矢印を描画します。
        /// <para>
        /// 終点に矢印の頭（ヘッド）が付いたスプライン曲線を描画します。
        /// </para>
        /// </summary>
        /// <param name="points">通過点のリスト。</param>
        /// <param name="thickness">線の太さ。</param>
        /// <param name="color">色。</param>
        /// <param name="headSize">矢印の先端のサイズ。</param>
        /// <param name="borderColor">縁取りの色（null指定時は縁取りなし）。</param>
        /// <param name="borderThickness">縁取りの太さ。</param>
        public void DrawArrow(List<Vector2> points, float thickness, Color color, float headSize = 20f, Color? borderColor = null, float borderThickness = 2f)
        {
            if (points == null || points.Count < 2) return;

            // まずスプライン曲線として補間点リストを生成
            List<Vector2> smoothPoints = GenerateSmoothCurve(points, 20); // 20分割

            // 矢印の向きと位置を計算 (パスの最後の2点から方向ベクトルを求める)
            var last = smoothPoints[smoothPoints.Count - 1];
            var prev = smoothPoints[smoothPoints.Count - 2];
            var dir = last - prev;
            
            // ベクトルの長さが0（同じ点）の場合はX軸方向をデフォルトとする
            if (dir.LengthSquared() > 0) dir.Normalize();
            else dir = Vector2.UnitX;

            // ボディ（線）のパスを生成（矢印のヘッド部分と重ならないように短くする）
            // 矢印の形状によるが、headSizeの8割程度戻った位置まで線を引くときれい繋がる
            float trimLength = headSize * 0.8f; 
            
            // ボディ用パスのトリミング（後ろから指定距離だけカット）
            List<Vector2> bodyPoints = TrimPathEnd(smoothPoints, trimLength);

            // SpriteBatch中断
            Ton.Gra.SuspendBatch();

            try
            {
                UpdateProjection();

                // 縁取り描画
                if (borderColor != null)
                {
                    // 線(始点側のみ延長)
                    List<Vector2> borderBodyPoints = ExtendPathStart(bodyPoints, borderThickness);

                    DrawPolyLine(borderBodyPoints, thickness + borderThickness * 2, borderColor.Value);
                    
                    // 先端（縁取りが先端まで見えるように、位置を少し前方にずらす）
                    // 矢印の鋭さに応じてずらす量は変わるが、ここでは簡易的に borderThickness * 2 程度ずらす
                    Vector2 borderTip = last + dir * (borderThickness * 2.0f);
                    
                    // サイズも大きくして、根本（後ろ側）にも縁が出るようにする
                    // 前に 2.0倍、後ろに 1.0倍 分伸ばすイメージで、合計 +3.0倍
                    float borderHeadSize = headSize + borderThickness * 3.0f;

                    DrawArrowHead(borderTip, dir, borderHeadSize, borderColor.Value);
                }

                // 本体描画
                DrawPolyLine(bodyPoints, thickness, color);
                DrawArrowHead(last, dir, headSize, color);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// キャットムル＝ロムスプライン曲線上の特定位置の座標と向きを取得します。
        /// <para>
        /// オブジェクトを曲線に沿って動かしたい場合などに使用します。
        /// </para>
        /// </summary>
        /// <param name="points">通過点のリスト。</param>
        /// <param name="t">
        /// 位置パラメータ (0.0f ～ points.Count - 1.0f)。
        /// 整数部が区間インデックス、小数部が区間内の進行度を表します。
        /// </param>
        /// <param name="position">計算された座標。</param>
        /// <param name="radians">計算された向き（ラジアン）。</param>
        public void GetSplineInfo(List<Vector2> points, float t, out Vector2 position, out float radians)
        {
            position = Vector2.Zero;
            radians = 0f;

            if (points == null || points.Count < 2) return;

            // tの値を有効範囲内にクランプ（制限）する
            float maxT = points.Count - 1;
            if (t < 0) t = 0;
            if (t > maxT) t = maxT;

            // セグメントのインデックス(i)と、パラメトリック変数(u)を計算
            // i番目の点とi+1番目の点の間を、u (0.0～1.0) で補間する
            int i = (int)t;
            if (i >= points.Count - 1)
            {
                // 終点の場合の特例処理（最後の区間の終端とする）
                i = points.Count - 2;
                t = points.Count - 1; 
            }
            float u = t - i;

            // Catmull-Rom補間に必要な4点を取得 (P0, P1, P2, P3)
            // P1とP2の間を補間するが、曲率計算のために前後のP0, P3が必要
            // 端点の場合は隣の点を複製して代用する
            Vector2 p0 = (i > 0) ? points[i - 1] : points[0];
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];
            Vector2 p3 = (i + 1 < points.Count - 1) ? points[i + 2] : points[points.Count - 1];

            // 座標計算 (Catmull-Rom公式)
            position = Vector2.CatmullRom(p0, p1, p2, p3, u);

            // 向き計算（接線ベクトルを求める）
            // 解析的に微分するのも手だが、簡易的に「少し先の点」との差分を使って求める
            // u + delta が 1.0 を超える場合は、逆に「少し前の点」との差分を使う
            
            float delta = 0.01f;
            Vector2 nextPos;
            
            if (u + delta <= 1.0f)
            {
                nextPos = Vector2.CatmullRom(p0, p1, p2, p3, u + delta);
                Vector2 dir = nextPos - position;
                // ベクトルの長さが極端に短い場合（同じ点など）は角度更新しない
                if (dir.LengthSquared() > 0.000001f)
                {
                    radians = (float)Math.Atan2(dir.Y, dir.X);
                }
            }
            else
            {
                // 終端付近は少し前の点を見る
                Vector2 prevPos = Vector2.CatmullRom(p0, p1, p2, p3, u - delta);
                Vector2 dir = position - prevPos;
                if (dir.LengthSquared() > 0.000001f)
                {
                    radians = (float)Math.Atan2(dir.Y, dir.X);
                }
            }
        }

        private List<Vector2> TrimPathEnd(List<Vector2> points, float trimDistance)
        {
            if (points == null || points.Count < 2) return new List<Vector2>();
            if (trimDistance <= 0) return new List<Vector2>(points);

            List<Vector2> result = new List<Vector2>();
            float accumulatedDist = 0;
            
            // 後ろから遡って距離を計算
            int cutIndex = -1;
            Vector2 cutPos = Vector2.Zero;

            for (int i = points.Count - 1; i > 0; i--)
            {
                float dist = Vector2.Distance(points[i], points[i - 1]);
                if (accumulatedDist + dist >= trimDistance)
                {
                    // このセグメント内でカット
                    float remaining = trimDistance - accumulatedDist;
                    // points[i] から points[i-1] 方向に remaining だけ進んだ地点
                    Vector2 dir = points[i - 1] - points[i];
                    dir.Normalize();
                    cutPos = points[i] + dir * remaining;
                    cutIndex = i - 1; // i-1まではそのまま含める
                    break;
                }
                accumulatedDist += dist;
            }

            if (cutIndex == -1)
            {
                // 全部消えてしまう場合（短すぎる）
                // 始点だけ返すか、空を返す
                return new List<Vector2>(); 
            }

            // 先頭からcutIndexまでコピー
            for (int i = 0; i <= cutIndex; i++)
            {
                result.Add(points[i]);
            }
            // カット地点を追加
            result.Add(cutPos);

            return result;
        }

        private List<Vector2> ExtendPathStart(List<Vector2> points, float length)
        {
            if (points == null || points.Count < 2) return points;
            List<Vector2> newPoints = new List<Vector2>(points);
            
            Vector2 dir = points[1] - points[0];
            if (dir.LengthSquared() > 0)
            {
                dir.Normalize();
                newPoints[0] = points[0] - dir * length;
            }
            return newPoints;
        }

        private List<Vector2> ExtendPathEnd(List<Vector2> points, float length)
        {
            if (points == null || points.Count < 2) return points;
            List<Vector2> newPoints = new List<Vector2>(points);
            
            int last = points.Count - 1;
            Vector2 dir = points[last] - points[last - 1];
            if (dir.LengthSquared() > 0)
            {
                dir.Normalize();
                newPoints[last] = points[last] + dir * length;
            }
            return newPoints;
        }

        /// <summary>
        /// スムーズな曲線を生成するためのヘルパーメソッド。
        /// <para>
        /// キャットムル＝ロムスプライン補間を用いて、元の点列の間を補完した新しい点列を生成します。
        /// </para>
        /// </summary>
        /// <param name="points">元の点列。</param>
        /// <param name="stepsPerSegment">1区間あたりの分割数（高いほど滑らかになるが、計算コストが増える）。</param>
        private List<Vector2> GenerateSmoothCurve(List<Vector2> points, int stepsPerSegment)
        {
            // 点が少ない場合はそのまま返す
            if (points.Count < 3) return new List<Vector2>(points);

            List<Vector2> result = new List<Vector2>();

            // 制御点の準備
            // Catmull-Rom補間には「前後の点」が必要なため、
            // 始点の手前に「始点のコピー」、終点の後に「終点のコピー」を追加して、
            // 端の点までスムーズに描画できるようにする。
            var controlPoints = new List<Vector2>(points.Count + 2);
            controlPoints.Add(points[0]); // P0 = P1 (Start duplicate)
            controlPoints.AddRange(points);
            controlPoints.Add(points[points.Count - 1]); // Pn+1 = Pn (End duplicate)

            // 各セグメントを補間
            // controlPointsには端点が増えているのでループ回数に注意
            for (int i = 0; i < controlPoints.Count - 3; i++)
            {
                Vector2 p0 = controlPoints[i];     // 前の点
                Vector2 p1 = controlPoints[i + 1]; // 現在の始点
                Vector2 p2 = controlPoints[i + 2]; // 現在の終点
                Vector2 p3 = controlPoints[i + 3]; // 次の点

                // 最初のセグメントの最初の点は必ず追加
                if (i == 0)
                {
                    result.Add(p1);
                }

                // 分割数だけループして補間点を生成
                for (int j = 1; j <= stepsPerSegment; j++)
                {
                    float t = j / (float)stepsPerSegment;
                    Vector2 pos = Vector2.CatmullRom(p0, p1, p2, p3, t);
                    result.Add(pos);
                }
            }

            return result;
        }

        /// <summary>
        /// 円（または楕円）を描画します。
        /// </summary>
        /// <param name="center">中心座標。</param>
        /// <param name="radius">半径（楕円の場合はradiusXとして扱われます）。</param>
        /// <param name="color">色。</param>
        /// <param name="segments">円の滑らかさ（分割数）。デフォルトは36。</param>
        public void DrawCircle(Vector2 center, float radius, Color color, int segments = 36)
        {
            DrawCircle(center, radius, radius, color, segments);
        }

        /// <summary>
        /// 円（または楕円）を描画します。
        /// </summary>
        /// <param name="center">中心座標。</param>
        /// <param name="radiusX">X方向の半径（横幅の半分）。</param>
        /// <param name="radiusY">Y方向の半径（縦幅の半分）。</param>
        /// <param name="color">色。</param>
        /// <param name="segments">円の滑らかさ（分割数）。デフォルトは36。</param>
        public void DrawCircle(Vector2 center, float radiusX, float radiusY, Color color, int segments = 36)
        {
            if (segments < 3) segments = 3;

            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();

                // 本体
                var verts = CreateCircleVertices(center, radiusX, radiusY, segments, color);
                DrawUserPrimitives(verts, PrimitiveType.TriangleList);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 円または楕円の頂点データを生成します。
        /// 中心点と、外周の点を結ぶ三角形のリスト (TriangleList) を作成します。
        /// </summary>
        private VertexPositionColor[] CreateCircleVertices(Vector2 center, float rx, float ry, int segments, Color color)
        {
            // TriangleListで描画するため、中心点と外周2点を結ぶ三角形を並べる
            // 頂点数: segments * 3
            VertexPositionColor[] vertices = new VertexPositionColor[segments * 3];
            
            float angleStep = MathHelper.TwoPi / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                // 楕円の方程式: x = rx * cos(θ), y = ry * sin(θ)
                Vector2 p1 = center + new Vector2((float)Math.Cos(angle1) * rx, (float)Math.Sin(angle1) * ry);
                Vector2 p2 = center + new Vector2((float)Math.Cos(angle2) * rx, (float)Math.Sin(angle2) * ry);

                // 頂点順序: 中心 -> 点1 -> 点2
                // カリングが無効(None)であれば時計回り・反時計回りは気にする必要はないが
                // 一般的には反時計回り(CCW)が表面。
                vertices[i * 3 + 0] = new VertexPositionColor(new Vector3(center, 0), color);
                vertices[i * 3 + 1] = new VertexPositionColor(new Vector3(p1, 0), color);
                vertices[i * 3 + 2] = new VertexPositionColor(new Vector3(p2, 0), color);
            }
            return vertices;
        }

        /// <summary>
        /// 矩形を描画します（回転対応）。
        /// </summary>
        /// <param name="position">座標。</param>
        /// <param name="size">幅と高さ。</param>
        /// <param name="color">色。</param>
        /// <param name="rotation">回転角度（ラジアン）。</param>
        /// <param name="origin">回転中心（nullなら中心(w/2, h/2)）。</param>
        public void DrawRectangle(Vector2 position, Vector2 size, Color color, float rotation = 0f, Vector2? origin = null)
        {
            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();

                Vector2 realOrigin = origin ?? (size / 2f);

                // 本体
                var verts = CreateRotatedRectpy(position, size, rotation, realOrigin, color);
                DrawUserPrimitives(verts, PrimitiveType.TriangleStrip);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 回転矩形の頂点データを生成します。
        /// TriangleStrip形式（Z字順序）で4頂点を返します。
        /// </summary>
        private VertexPositionColor[] CreateRotatedRectpy(Vector2 pos, Vector2 size, float rot, Vector2 origin, Color color)
        {
            // 回転行列の作成
            // 1. 原点あわせ (-origin)
            // 2. 回転 (RotationZ)
            // 3. 元の位置へ移動 (+pos)
            Matrix mat = Matrix.CreateTranslation(-origin.X, -origin.Y, 0) *
                         Matrix.CreateRotationZ(rot) *
                         Matrix.CreateTranslation(pos.X, pos.Y, 0);

            // 4隅の座標を変換
            Vector2 tl = Vector2.Transform(Vector2.Zero, mat);
            Vector2 tr = Vector2.Transform(new Vector2(size.X, 0), mat);
            Vector2 bl = Vector2.Transform(new Vector2(0, size.Y), mat);
            Vector2 br = Vector2.Transform(size, mat);

            // TriangleStrip順序: ToList -> TopRight -> BottomLeft -> BottomRight
            // 0:TL 1:TR 2:BL 3:BR
            // トライアングル構成: (0,1,2) と (2,1,3)
            return new VertexPositionColor[]
            {
                new VertexPositionColor(new Vector3(tl, 0), color),
                new VertexPositionColor(new Vector3(tr, 0), color),
                new VertexPositionColor(new Vector3(bl, 0), color),
                new VertexPositionColor(new Vector3(br, 0), color)
            };
        }

        /// <summary>
        /// 複数の点をつなぐ太線を描画します。
        /// <para>
        /// TriangleStripを用いて、線に幅を持たせたポリゴンを描画します。
        /// マイター結合（角の処理）は簡易的なものになります。
        /// </para>
        /// </summary>
        private void DrawPolyLine(List<Vector2> points, float thickness, Color color)
        {
            if (points.Count < 2) return;

            int count = points.Count;
            // 頂点数 = 点の数 * 2 (左右)
            VertexPositionColor[] vertices = new VertexPositionColor[count * 2];
            float halfThick = thickness * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector2 current = points[i];
                Vector2 normal;

                // 各点における「法線（進行方向に対して垂直なベクトル）」を計算する
                if (i == 0)
                {
                    // 始点: 次の点への方向から法線を計算
                    Vector2 dir = points[1] - current;
                    if (dir.LengthSquared() > 0) dir.Normalize();
                    normal = new Vector2(-dir.Y, dir.X); // 90度回転
                }
                else if (i == count - 1)
                {
                    // 終点: 前の点からの方向から法線を計算
                    Vector2 dir = current - points[i - 1];
                    if (dir.LengthSquared() > 0) dir.Normalize();
                    normal = new Vector2(-dir.Y, dir.X);
                }
                else
                {
                    // 中間点: 前後の点への方向の平均（角度の二等分線に近い）をとることで、
                    // 角が滑らかに繋がるようにする
                    Vector2 dir1 = current - points[i - 1];
                    if (dir1.LengthSquared() > 0) dir1.Normalize();

                    Vector2 dir2 = points[i + 1] - current;
                    if (dir2.LengthSquared() > 0) dir2.Normalize();

                    // 平均接線
                    Vector2 tangent = dir1 + dir2;
                    if (tangent.LengthSquared() == 0) tangent = dir2; // 完全に折り返している場合など
                    else tangent.Normalize();

                    // 接線の垂直方向を法線とする
                    normal = new Vector2(-tangent.Y, tangent.X);

                    // TODO: 鋭角の場合に太さが細くならないように、マイター結合の補正を入れるとさらに良くなる
                    // 現在は固定幅での拡張
                }

                // 中心点から法線方向に +/- halfThick ずらして、左右の頂点を生成
                Vector2 pLeft = current + normal * halfThick;
                Vector2 pRight = current - normal * halfThick;

                // Strip用の頂点配列にセット (Zig-Zag順: 0, 1, 2, 3...)
                // index偶数: Left, index奇数: Right
                vertices[i * 2] = new VertexPositionColor(new Vector3(pLeft, 0), color);
                vertices[i * 2 + 1] = new VertexPositionColor(new Vector3(pRight, 0), color);
            }

            // プリミティブ描画 (TriangleStrip)
            DrawUserPrimitives(vertices, PrimitiveType.TriangleStrip);
        }


        /// <summary>
        /// 三角形を描画（塗りつぶし）します。
        /// </summary>
        /// <param name="p1">頂点1</param>
        /// <param name="p2">頂点2</param>
        /// <param name="p3">頂点3</param>
        /// <param name="color">色</param>
        public void DrawTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color color)
        {
            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();
                
                VertexPositionColor[] vertices = new VertexPositionColor[]
                {
                    new VertexPositionColor(new Vector3(p1, 0), color),
                    new VertexPositionColor(new Vector3(p2, 0), color),
                    new VertexPositionColor(new Vector3(p3, 0), color)
                };

                DrawUserPrimitives(vertices, PrimitiveType.TriangleList);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 凸多角形を描画（塗りつぶし）します。
        /// <para>
        /// 頂点リストの順番通りに結んで描画します。凸形状であることを前提としています（TriangleFan的に描画）。
        /// </para>
        /// </summary>
        /// <param name="vertices">頂点リスト（3つ以上）</param>
        /// <param name="color">色</param>
        public void DrawPolygon(List<Vector2> vertices, Color color)
        {
            if (vertices == null || vertices.Count < 3) return;

            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();

                // 中心点（平均）を求めて、そこから各辺へ三角形を作る方式が、
                // 凸多角形なら安定して描画できる
                Vector2 center = Vector2.Zero;
                foreach (var v in vertices) center += v;
                center /= vertices.Count;

                int count = vertices.Count;
                VertexPositionColor[] vpc = new VertexPositionColor[count * 3];

                for (int i = 0; i < count; i++)
                {
                    Vector2 p1 = vertices[i];
                    Vector2 p2 = vertices[(i + 1) % count]; // 次の点（最後は最初に戻る）

                    vpc[i * 3 + 0] = new VertexPositionColor(new Vector3(center, 0), color);
                    vpc[i * 3 + 1] = new VertexPositionColor(new Vector3(p1, 0), color);
                    vpc[i * 3 + 2] = new VertexPositionColor(new Vector3(p2, 0), color);
                }

                DrawUserPrimitives(vpc, PrimitiveType.TriangleList);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 円弧（線）を描画します。
        /// </summary>
        /// <param name="center">中心</param>
        /// <param name="radius">半径</param>
        /// <param name="startAngle">開始角度（ラジアン）</param>
        /// <param name="endAngle">終了角度（ラジアン）</param>
        /// <param name="thickness">線の太さ</param>
        /// <param name="color">色</param>
        /// <param name="segments">分割数</param>
        public void DrawArc(Vector2 center, float radius, float startAngle, float endAngle, float thickness, Color color, int segments = 24)
        {
            if (segments < 2) segments = 2;

            List<Vector2> points = new List<Vector2>();
            float totalAngle = endAngle - startAngle;
            float step = totalAngle / segments;

            for (int i = 0; i <= segments; i++)
            {
                float angle = startAngle + step * i;
                Vector2 p = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                points.Add(p);
            }

            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();
                DrawPolyLine(points, thickness, color);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 角丸矩形を描画（塗りつぶし）します。
        /// </summary>
        /// <param name="position">矩形の中心座標、またはOrigin基準の座標</param>
        /// <param name="size">幅と高さ</param>
        /// <param name="cornerRadius">角の丸み（半径）</param>
        /// <param name="color">色</param>
        /// <param name="rotation">回転角度（ラジアン）</param>
        /// <param name="origin">回転中心（nullなら矩形の中心）</param>
        public void DrawRoundedRectangle(Vector2 position, Vector2 size, float cornerRadius, Color color, float rotation = 0f, Vector2? origin = null)
        {
            // コーナー半径が大きすぎる場合の補正
            float minDim = Math.Min(size.X, size.Y);
            if (cornerRadius * 2 > minDim) cornerRadius = minDim / 2f;
            if (cornerRadius < 0) cornerRadius = 0;

            // 回転などの行列を作成
            Vector2 realOrigin = origin ?? (size / 2f);
            
            // 原点あわせ -> 回転 -> 元の位置へ
            Matrix transform = Matrix.CreateTranslation(-realOrigin.X, -realOrigin.Y, 0) *
                               Matrix.CreateRotationZ(rotation) *
                               Matrix.CreateTranslation(position.X, position.Y, 0);

            // 形状生成
            // 中心点から描画する TriangleFan 形式で頂点を作るのが汎用的
            // 図形内部の点（ここでは矩形中心 (w/2, h/2)）を基準にする
            Vector2 centerLocal = size / 2f;

            List<VertexPositionColor> vertices = new List<VertexPositionColor>();

            // 1. 中心点
            Vector2 transformedCenter = Vector2.Transform(centerLocal, transform);
            vertices.Add(new VertexPositionColor(new Vector3(transformedCenter, 0), color));

            // 2. 周囲の点を作成（右上、右下、左下、左上の順にコーナーを回る）
            // コーナーの分割数
            int cornerSegments = 8;

            float w = size.X;
            float h = size.Y;
            float r = cornerRadius;

            // 右上 (Top-Right)
            GenerateCornerVertices(vertices, new Vector2(w - r, r), r, -MathHelper.PiOver2, 0, cornerSegments, transform, color);

            // 右下 (Bottom-Right) : 0 -> PI/2
            GenerateCornerVertices(vertices, new Vector2(w - r, h - r), r, 0, MathHelper.PiOver2, cornerSegments, transform, color);

            // 左下 (Bottom-Left) : PI/2 -> PI
            GenerateCornerVertices(vertices, new Vector2(r, h - r), r, MathHelper.PiOver2, MathHelper.Pi, cornerSegments, transform, color);

            // 左上 (Top-Left) : PI -> 3PI/2 (or -PI/2)
            GenerateCornerVertices(vertices, new Vector2(r, r), r, MathHelper.Pi, MathHelper.Pi * 1.5f, cornerSegments, transform, color);

            // 始点に戻る（閉じる）
            vertices.Add(vertices[1]); 

            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();
                
                // TriangleFan -> TriangleList 変換
                int triCount = vertices.Count - 2;
                VertexPositionColor[] triList = new VertexPositionColor[triCount * 3];
                
                for(int i=0; i<triCount; i++)
                {
                    triList[i*3+0] = vertices[0];     // Center
                    triList[i*3+1] = vertices[i+1];   // Current
                    triList[i*3+2] = vertices[i+2];   // Next
                }
                
                DrawUserPrimitives(triList, PrimitiveType.TriangleList);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        private void GenerateCornerVertices(List<VertexPositionColor> vertices, Vector2 center, float r, float startAngle, float endAngle, int segments, Matrix transform, Color color)
        {
            float step = (endAngle - startAngle) / segments;
            for (int i = 0; i <= segments; i++)
            {
                float angle = startAngle + step * i;
                Vector2 p = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * r;
                
                // 変換
                Vector2 transP = Vector2.Transform(p, transform);
                vertices.Add(new VertexPositionColor(new Vector3(transP, 0), color));
            }
        }

        /// <summary>
        /// 矢印の先端（三角形）を描画します。
        /// <para>
        /// 指定された位置と向きに基づいて、二等辺三角形の頂点を計算し描画します。
        /// </para>
        /// </summary>
        /// <param name="pos">先端の座標。</param>
        /// <param name="direction">矢印の向き（正規化されたベクトル）。</param>
        /// <param name="size">矢印の大きさ（高さ）。</param>
        /// <param name="color">色。</param>
        private void DrawArrowHead(Vector2 pos, Vector2 direction, float size, Color color)
        {
            // 方向ベクトルから角度を計算する（Atan2は -PI ～ PI を返す）
            // 実は今回は角度を使わずにベクトル演算だけで頂点を求める方式を採用しているため、
            // この変数は使っていないが、回転行列を使う場合は必要になる。
            // float angle = (float)Math.Atan2(direction.Y, direction.X);
            
            // 三角形の頂点計算
            // 先端が pos
            // 後ろの2点（左後方、右後方）を計算して三角形を作る
            
            Vector2 tip = pos; // 先端（Tip）
            
            // 根本（Back）の位置: 進行方向の逆に size 分だけ戻った場所
            Vector2 back = pos - direction * size;
            
            // 矢印の幅を決めるための法線ベクトル
            // direction(x, y) に対して (-y, x) は90度回転した法線
            // size * 0.6f は矢印の開き具合（アスペクト比）の係数
            Vector2 normal = new Vector2(-direction.Y, direction.X) * (size * 0.6f); 
            
            // 根本から左右に広げて、三角形の底辺の2点を求める
            Vector2 left = back + normal;
            Vector2 right = back - normal;

            // 頂点配列の作成
            VertexPositionColor[] vertices = new VertexPositionColor[3];
            vertices[0] = new VertexPositionColor(new Vector3(tip, 0), color);
            vertices[1] = new VertexPositionColor(new Vector3(right, 0), color); 
            vertices[2] = new VertexPositionColor(new Vector3(left, 0), color);
            // カリング（裏面非描画）が無効(None)なら頂点の順番はどちらでも表示される

            DrawUserPrimitives(vertices, PrimitiveType.TriangleList);
        }

        /// <summary>
        /// スプライン曲線の点と、指定された高さ（bottomY）の間を塗りつぶします。
        /// <para>
        /// 水面や地形の断面のような表現に使用できます。
        /// 上側（曲線）と下側（底辺）で異なる色を指定でき、グラデーションになります。
        /// </para>
        /// </summary>
        /// <param name="points">曲線が通過する点のリスト。</param>
        /// <param name="bottomY">底辺のY座標（固定）。</param>
        /// <param name="topColor">上側（曲線側）の色。半透明も可。</param>
        /// <param name="bottomColor">下側（底辺側）の色。半透明も可。</param>
        /// <param name="isAdditive">trueの場合、加算合成（光るような表現）で描画します。</param>
        public void DrawSplineArea(List<Vector2> points, float bottomY, Color topColor, Color bottomColor, bool isAdditive = false)
        {
            if (points == null || points.Count < 2) return;

            // 補間された点リストを生成
            List<Vector2> smoothPoints = GenerateSmoothCurve(points, 20);

            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();

                int count = smoothPoints.Count;
                // TriangleStripで「上」「下」「上」「下」とジグザグに結ぶため、頂点数は点の2倍
                VertexPositionColor[] vertices = new VertexPositionColor[count * 2];

                for (int i = 0; i < count; i++)
                {
                    Vector2 p = smoothPoints[i];
                    
                    // 頂点0: 曲線上の点 (topColor)
                    vertices[i * 2] = new VertexPositionColor(new Vector3(p, 0), topColor);
                    
                    // 頂点1: 底辺上の点 (bottomColor)
                    // X座標は曲線と同じで、Y座標だけbottomYに固定することで、
                    // 真下に伸びる帯状のメッシュを作る
                    vertices[i * 2 + 1] = new VertexPositionColor(new Vector3(p.X, bottomY, 0), bottomColor);
                }

                // ブレンドステートの決定
                // Additive: 色を加算する（光の表現など）
                // AlphaBlend: 通常の半透明合成
                BlendState blend = isAdditive ? BlendState.Additive : BlendState.AlphaBlend;

                // 描画実行
                DrawUserPrimitives(vertices, PrimitiveType.TriangleStrip, blend);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 扇形を描画します。内径を指定してドーナツ型にすることも可能です。
        /// </summary>
        /// <param name="center">中心座標。</param>
        /// <param name="radius">外径（外側の半径）。</param>
        /// <param name="startAngle">開始角度（ラジアン）。右方向が0、時計回り。</param>
        /// <param name="endAngle">終了角度（ラジアン）。</param>
        /// <param name="color">色。</param>
        /// <param name="segments">円弧の分割数。</param>
        /// <param name="innerRadius">内径（内側の半径）。0より大きい値を指定するとドーナツ型になります。</param>
        public void DrawSector(Vector2 center, float radius, float startAngle, float endAngle, Color color, int segments = 24, float innerRadius = 0f)
        {
            if (segments < 3) segments = 3;

            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();

                // 本体
                var verts = CreateSectorVertices(center, radius, innerRadius, startAngle, endAngle, segments, color);
                DrawUserPrimitives(verts, PrimitiveType.TriangleList);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 扇形の頂点データを生成するヘルパーメソッド。
        /// </summary>
        private VertexPositionColor[] CreateSectorVertices(Vector2 center, float outerRadius, float innerRadius, float startAngle, float endAngle, int segments, Color color)
        {
            // 全体の角度差分（扇形の開き具合）
            float sweep = endAngle - startAngle;
            float angleStep = sweep / segments;
            
            // 内径があるかどうかで処理を分ける
            bool isRing = innerRadius > 0.01f;
            
            // リング状なら四角形（三角形2つ）× セグメント数
            // 通常の扇形なら三角形1つ × セグメント数
            int vertsPerSegment = isRing ? 6 : 3;

            VertexPositionColor[] vertices = new VertexPositionColor[segments * vertsPerSegment];

            for (int i = 0; i < segments; i++)
            {
                // 現在のセグメントの開始角度と終了角度
                float a1 = startAngle + i * angleStep;
                float a2 = startAngle + (i + 1) * angleStep;

                // 外周の2点
                Vector2 p1_out = center + new Vector2((float)Math.Cos(a1) * outerRadius, (float)Math.Sin(a1) * outerRadius);
                Vector2 p2_out = center + new Vector2((float)Math.Cos(a2) * outerRadius, (float)Math.Sin(a2) * outerRadius);

                if (isRing)
                {
                    // 内周の2点
                    Vector2 p1_in = center + new Vector2((float)Math.Cos(a1) * innerRadius, (float)Math.Sin(a1) * innerRadius);
                    Vector2 p2_in = center + new Vector2((float)Math.Cos(a2) * innerRadius, (float)Math.Sin(a2) * innerRadius);

                    // 四角形を2つの三角形で表現
                    // 1つ目: Inner1 -> Outer1 -> Outer2
                    vertices[i * 6 + 0] = new VertexPositionColor(new Vector3(p1_in, 0), color);
                    vertices[i * 6 + 1] = new VertexPositionColor(new Vector3(p1_out, 0), color);
                    vertices[i * 6 + 2] = new VertexPositionColor(new Vector3(p2_out, 0), color);

                    // 2つ目: Inner1 -> Outer2 -> Inner2
                    vertices[i * 6 + 3] = new VertexPositionColor(new Vector3(p1_in, 0), color);
                    vertices[i * 6 + 4] = new VertexPositionColor(new Vector3(p2_out, 0), color);
                    vertices[i * 6 + 5] = new VertexPositionColor(new Vector3(p2_in, 0), color);
                }
                else
                {
                    // 通常の扇形（中心点を使う）
                    // 中心 -> Outer1 -> Outer2
                    vertices[i * 3 + 0] = new VertexPositionColor(new Vector3(center, 0), color);
                    vertices[i * 3 + 1] = new VertexPositionColor(new Vector3(p1_out, 0), color);
                    vertices[i * 3 + 2] = new VertexPositionColor(new Vector3(p2_out, 0), color);
                }
            }

            return vertices;
        }

        /// <summary>
        /// 破線スプラインを描画します。
        /// </summary>
        /// <param name="points">通過点のリスト。</param>
        /// <param name="thickness">線の太さ。</param>
        /// <param name="color">色。</param>
        /// <param name="dashLength">線の部分の長さ。</param>
        /// <param name="gapLength">空白部分の長さ。</param>
        public void DrawSplineDashed(List<Vector2> points, float thickness, Color color, float dashLength = 10f, float gapLength = 10f)
        {
            if (points == null || points.Count < 2) return;

            // スムーズな曲線を取得
            List<Vector2> smoothPoints = GenerateSmoothCurve(points, 20);
            
            // 破線化処理
            // 連続した点群を、dashLengthとgapLengthに基づいて「複数の短い線のリスト（dashes）」に分割します
            List<List<Vector2>> dashes = CreateDashes(smoothPoints, dashLength, gapLength);

            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();

                foreach (var dash in dashes)
                {
                    // 点が2つ未満の線分は描画できないためスキップ
                    if (dash.Count < 2) continue;
                    
                    // 本体描画
                    DrawPolyLine(dash, thickness, color);
                }
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 破線矢印を描画します。
        /// <para>
        /// ボディ部分を破線にし、先端に通常の矢印ヘッドを描画します。
        /// </para>
        /// </summary>
        /// <param name="points">通過点のリスト。</param>
        /// <param name="thickness">線の太さ。</param>
        /// <param name="color">色。</param>
        /// <param name="headSize">矢印の先端サイズ。</param>
        /// <param name="dashLength">破線の線部分の長さ。</param>
        /// <param name="gapLength">破線の空白部分の長さ。</param>
        /// <param name="borderColor">縁取りの色。</param>
        /// <param name="borderThickness">縁取りの太さ。</param>
        public void DrawArrowDashed(List<Vector2> points, float thickness, Color color, float headSize = 20f, float dashLength = 10f, float gapLength = 10f, Color? borderColor = null, float borderThickness = 2f)
        {
            if (points == null || points.Count < 2) return;

            List<Vector2> smoothPoints = GenerateSmoothCurve(points, 20);

            // 矢印情報の計算
            var last = smoothPoints[smoothPoints.Count - 1];
            var prev = smoothPoints[smoothPoints.Count - 2];
            var dir = last - prev;
            if (dir.LengthSquared() > 0) dir.Normalize();

            // ボディのトリミング（矢印部分は空ける）
            float trimLength = headSize * 0.8f; 
            List<Vector2> bodyPoints = TrimPathEnd(smoothPoints, trimLength);

            // ボディ部分を破線リストに変換
            List<List<Vector2>> dashes = CreateDashes(bodyPoints, dashLength, gapLength);

            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();

                // ボディ描画（各ダッシュ線分を描画）
                foreach (var dash in dashes)
                {
                    if (dash.Count < 2) continue;

                    if (borderColor != null)
                    {
                        List<Vector2> borderDash = ExtendPathStart(dash, borderThickness);
                        borderDash = ExtendPathEnd(borderDash, borderThickness);
                        DrawPolyLine(borderDash, thickness + borderThickness * 2, borderColor.Value);
                    }
                    DrawPolyLine(dash, thickness, color);
                }

                // ヘッド描画（ここはDrawArrowと同じロジックで通常描画）
                if (borderColor != null)
                {
                    Vector2 borderTip = last + dir * (borderThickness * 2.0f);
                    float borderHeadSize = headSize + borderThickness * 3.0f;
                    DrawArrowHead(borderTip, dir, borderHeadSize, borderColor.Value);
                }
                DrawArrowHead(last, dir, headSize, color);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 点列を走査して、指定された長さ（dash）と間隔（gap）で分割したリストのリストを生成します。
        /// </summary>
        private List<List<Vector2>> CreateDashes(List<Vector2> points, float dashLen, float gapLen)
        {
            List<List<Vector2>> dashes = new List<List<Vector2>>();
            if (points == null || points.Count < 2) return dashes;
            if (dashLen <= 0) 
            {
                // Dash長さ0以下の場合は一本線として返す
                dashes.Add(points);
                return dashes;
            }

            List<Vector2> currentDash = new List<Vector2>();
            currentDash.Add(points[0]);
            
            float currentDist = 0f;       // 現在のセグメント（Dash or Gap）での進行距離
            bool isDashing = true;        // 現在Dash（描画する線）を処理中かどうか
            float segmentTarget = dashLen; // 現在のセグメントの目標長さ

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p0 = points[i];
                Vector2 p1 = points[i + 1];
                float dist = Vector2.Distance(p0, p1);

                float processed = 0f; // この点区間で処理した距離
                while (processed < dist)
                {
                    float remainingSeg = dist - processed;     // 点区間の残り距離
                    float need = segmentTarget - currentDist;  // セグメント完了に必要な距離

                    if (remainingSeg >= need)
                    {
                        // この点区間の途中でセグメント（Dash or Gap）が終了する場合
                        // 線形補間(Lerp)で区切り位置を計算
                        Vector2 mid = p0 + (p1 - p0) * ((processed + need) / dist);
                        
                        if (isDashing)
                        {
                            // Dash終了: ここまでを1つの線リストとして保存
                            currentDash.Add(mid);
                            dashes.Add(new List<Vector2>(currentDash));
                            
                            // 次のDashのためにリセット
                            currentDash.Clear();
                            segmentTarget = gapLen; // 次はGap
                            isDashing = false;
                        }
                        else
                        {
                            // Gap終了: ここから新しいDash開始
                            currentDash.Add(mid); // 新しいDashの始点
                            segmentTarget = dashLen; // 次はDash
                            isDashing = true;
                        }
                        
                        currentDist = 0;
                        processed += need;
                    }
                    else
                    {
                        // 次の点まで到達したが、セグメントはまだ終わらない場合
                        if (isDashing)
                        {
                            currentDash.Add(p1);
                        }
                        currentDist += remainingSeg;
                        processed = dist; // 次の点へ (loop脱出)
                    }
                }
            }

            // ループ終了後、最後がDash中で終わっていたらそれも追加する
            if (isDashing && currentDash.Count > 1)
            {
                dashes.Add(currentDash);
            }

            return dashes;
        }

        /// <summary>
        /// 頂点配列を受け取り、GraphicsDeviceを通じてプリミティブを描画するための内部メソッド。
        /// </summary>
        /// <param name="vertices">頂点データの配列。</param>
        /// <param name="type">プリミティブの種類（TriangleList, TriangleStripなど）。</param>
        /// <param name="blendState">使用するブレンドステート（省略時は現在の状態を維持）。</param>
        private void DrawUserPrimitives(VertexPositionColor[] vertices, PrimitiveType type, BlendState blendState = null)
        {
            if (vertices.Length == 0) return;

            // SpriteBatchが走っている可能性があるため、ステート管理が必要
            // BasicEffectを使う場合は個別にApplyする必要がある
            
            // カリングを無効化（矢印の先端とボディで巻き方向が異なる可能性があるため）
            // 両面描画することで、頂点順序のミスやカメラの向きによる非表示を防ぐ
            var rasterizerState = new RasterizerState { CullMode = CullMode.None };
            _game.GraphicsDevice.RasterizerState = rasterizerState;

            // BlendStateの保存と適用
            BlendState prevBlendState = _game.GraphicsDevice.BlendState;
            if (blendState != null)
            {
                _game.GraphicsDevice.BlendState = blendState;
            }

            foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                
                // プリミティブ数（描画する三角形や線の数）の計算
                int primitiveCount = 0;
                if (type == PrimitiveType.TriangleList) primitiveCount = vertices.Length / 3;
                else if (type == PrimitiveType.TriangleStrip) primitiveCount = vertices.Length - 2;
                else if (type == PrimitiveType.LineList) primitiveCount = vertices.Length / 2;

                if (primitiveCount > 0)
                {
                    _game.GraphicsDevice.DrawUserPrimitives(type, vertices, 0, primitiveCount);
                }
            }

            // BlendStateを戻す（他の描画に影響を与えないため）
            if (blendState != null)
            {
                _game.GraphicsDevice.BlendState = prevBlendState;
            }
        }


        /// <summary>
        /// リボン（軌跡）を描画します。
        /// <para>
        /// 始点から終点にかけて太さと色が変化する帯を描画します。
        /// 剣の軌跡やミサイルの煙などに適しています。
        /// </para>
        /// </summary>
        /// <param name="points">頂点のリスト。</param>
        /// <param name="startWidth">始点（リストの先頭）での太さ。</param>
        /// <param name="endWidth">終点（リストの末尾）での太さ。</param>
        /// <param name="startColor">始点の色。</param>
        /// <param name="endColor">終点の色。</param>
        public void DrawRibbon(List<Vector2> points, float startWidth, float endWidth, Color startColor, Color endColor)
        {
            if (points == null || points.Count < 2) return;

            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();

                int count = points.Count;
                VertexPositionColor[] vertices = new VertexPositionColor[count * 2];

                // 全長を計算（正規化された進行度 T を求めるため）
                float totalLength = 0;
                float[] distances = new float[count];
                distances[0] = 0;
                for (int i = 1; i < count; i++)
                {
                    float d = Vector2.Distance(points[i], points[i - 1]);
                    totalLength += d;
                    distances[i] = totalLength;
                }

                if (totalLength <= 0) return; // 全点が同じ場所にある場合など

                for (int i = 0; i < count; i++)
                {
                    Vector2 current = points[i];
                    Vector2 normal;

                    // 法線計算 (DrawPolyLineと同様、ただし角の処理は簡易的)
                    if (i == 0)
                    {
                        Vector2 dir = points[1] - current;
                        if (dir.LengthSquared() > 0) dir.Normalize();
                        normal = new Vector2(-dir.Y, dir.X);
                    }
                    else if (i == count - 1)
                    {
                        Vector2 dir = current - points[i - 1];
                        if (dir.LengthSquared() > 0) dir.Normalize();
                        normal = new Vector2(-dir.Y, dir.X);
                    }
                    else
                    {
                        Vector2 dir1 = current - points[i - 1];
                        if (dir1.LengthSquared() > 0) dir1.Normalize();
                        Vector2 dir2 = points[i + 1] - current;
                        if (dir2.LengthSquared() > 0) dir2.Normalize();
                        
                        Vector2 tangent = dir1 + dir2;
                        if (tangent.LengthSquared() == 0) tangent = dir2;
                        else tangent.Normalize();
                        normal = new Vector2(-tangent.Y, tangent.X);
                    }

                    // 進行度 (0.0 ～ 1.0)
                    float t = distances[i] / totalLength;

                    // 現在の太さと色を補間
                    float currentWidth = MathHelper.Lerp(startWidth, endWidth, t);
                    Color currentColor = Color.Lerp(startColor, endColor, t);

                    float halfWidth = currentWidth * 0.5f;

                    Vector2 pLeft = current + normal * halfWidth;
                    Vector2 pRight = current - normal * halfWidth;

                    vertices[i * 2] = new VertexPositionColor(new Vector3(pLeft, 0), currentColor);
                    vertices[i * 2 + 1] = new VertexPositionColor(new Vector3(pRight, 0), currentColor);
                }

                DrawUserPrimitives(vertices, PrimitiveType.TriangleStrip);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 稲妻を描画します。
        /// <para>
        /// 2点間をランダムなジグザグ線で結びます。
        /// </para>
        /// </summary>
        /// <param name="start">始点。</param>
        /// <param name="end">終点。</param>
        /// <param name="thickness">線の太さ。</param>
        /// <param name="color">色。</param>
        /// <param name="difficulty">激しさ（揺れ幅）。1.0が標準的。</param>
        /// <param name="updatesPerSecond">秒間の形状更新回数。0またはマイナスで更新なし（位置依存のみ）。</param>
        public void DrawBolt(Vector2 start, Vector2 end, float thickness, Color color, float difficulty, float updatesPerSecond = 60f)
        {
            // シードの生成
            // 時間経過による変化 + 場所による違い
            int timeSeed = 0;
            if (updatesPerSecond > 0)
            {
                double time = Ton.Game.TotalGameTime.TotalSeconds;
                timeSeed = (int)(time * updatesPerSecond);
            }
            
            // 座標ハッシュを混ぜて、場所ごとに違う形にする
            // 座標が変わると形が変わってしまうが、稲妻なので許容範囲とする
            // (固定したい場合は別途ID管理が必要になるため、今回は簡易実装とする)
            int posSeed = start.GetHashCode() ^ end.GetHashCode();
            int finalSeed = timeSeed ^ posSeed;

            List<Vector2> points = GenerateBoltPoints(start, end, difficulty, finalSeed);
            
            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();
                DrawPolyLine(points, thickness, color);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }

        /// <summary>
        /// 稲妻の頂点生成処理（再帰分割法）。
        /// </summary>
        private List<Vector2> GenerateBoltPoints(Vector2 start, Vector2 end, float difficulty, int seed)
        {
            var results = new List<Vector2>();
            results.Add(start);
            
            var rand = new Random(seed);
            
            // 再帰的に分割していく
            // difficulty が大きいほど大きくずれる
            // 距離に応じて分割数を決めるなどの調整も可能だが、ここでは固定段数か距離依存にする
            
            float dist = Vector2.Distance(start, end);
            // ある程度細かくなるまで分割
            // 例えば 10px 以下になるまで、とかだと細かすぎるので、
            // ステップ数固定等で実装する。ここではリストに入れて処理する非再帰で書く
            
            var segments = new LinkedList<Vector2>();
            segments.AddLast(start);
            segments.AddLast(end);

            float displacement = dist * 0.25f * difficulty; // 初回のずれ幅

            // 5回分割（2^5 = 32分割）あれば十分稲妻に見える
            for (int i = 0; i < 5; i++)
            {
                var node = segments.First;
                while (node != null && node.Next != null)
                {
                    Vector2 p1 = node.Value;
                    Vector2 p2 = node.Next.Value;
                    
                    Vector2 mid = (p1 + p2) * 0.5f;
                    
                    // 法線方向へのランダムなずれ
                    Vector2 dir = p2 - p1;
                    Vector2 normal = new Vector2(-dir.Y, dir.X);
                    if (normal.LengthSquared() > 0) normal.Normalize();
                    
                    float offset = (float)(rand.NextDouble() - 0.5) * displacement;
                    mid += normal * offset;

                    // ノード挿入
                    segments.AddAfter(node, mid);
                    
                    // 次のセグメントへ（今追加したmidの次は処理しないので2つ進む）
                    node = node.Next.Next;
                }
                displacement *= 0.5f; // 分割が進むごとにずれ幅を減らす
            }

            return new List<Vector2>(segments);
        }

        /// <summary>
        /// 集中線（Focus Lines）を描画します。
        /// <para>
        /// 画面外から中心（または指定点）に向かう鋭い三角形をランダムに描画します。
        /// </para>
        /// </summary>
        /// <param name="center">集中線の中心点。</param>
        /// <param name="outerRadius">描画開始半径（画面外まで届くように大きめに設定）。</param>
        /// <param name="intensity">線の密度や太さの係数（0.0～）。</param>
        /// <param name="color">色。</param>
        /// <param name="updatesPerSecond">秒間の形状更新回数。0で更新なし。</param>
        public void DrawFocusLines(Vector2 center, float outerRadius, float intensity, Color color, float updatesPerSecond = 0f)
        {
            Ton.Gra.SuspendBatch();
            try
            {
                UpdateProjection();

                // シード生成
                int timeSeed = 0;
                if (updatesPerSecond > 0)
                {
                    double time = Ton.Game.TotalGameTime.TotalSeconds;
                    timeSeed = (int)(time * updatesPerSecond);
                }
                // 中心座標もシードに混ぜる
                int seed = timeSeed ^ center.GetHashCode();

                var rand = new Random(seed);
                int lineCount = (int)(40 * intensity); // 線の本数
                if (lineCount < 5) lineCount = 5;

                VertexPositionColor[] vertices = new VertexPositionColor[lineCount * 3];

                for (int i = 0; i < lineCount; i++)
                {
                    // ランダムな角度
                    float angle = (float)(rand.NextDouble() * MathHelper.TwoPi);
                    
                    // 線の太さ（先端の角度幅）
                    float widthAngle = (float)(rand.NextDouble() * 0.05f * intensity);
                    
                    // 線の長さ（中心からの距離）
                    // 中心ギリギリまで来る線もあれば、遠くで終わる線もある
                    // innerRadius をランダムに決める
                    float innerRadius = (float)(rand.NextDouble() * outerRadius * 0.5f);

                    // 3頂点を計算
                    // p1: 中心側（鋭い先端）
                    // p2, p3: 外側（太い底辺）
                    
                    Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                    
                    Vector2 p1 = center + dir * innerRadius;
                    
                    Vector2 outerCenter = center + dir * outerRadius;
                    Vector2 sideDir = new Vector2(-dir.Y, dir.X); // 垂直
                    
                    // 外側の幅（距離に応じて太くする＋ランダム）
                    float outerWidth = (outerRadius * widthAngle) + (float)rand.NextDouble() * 20f * intensity;

                    Vector2 p2 = outerCenter + sideDir * (outerWidth * 0.5f);
                    Vector2 p3 = outerCenter - sideDir * (outerWidth * 0.5f);

                    vertices[i * 3 + 0] = new VertexPositionColor(new Vector3(p1, 0), color);
                    vertices[i * 3 + 1] = new VertexPositionColor(new Vector3(p2, 0), color);
                    vertices[i * 3 + 2] = new VertexPositionColor(new Vector3(p3, 0), color);
                }

                DrawUserPrimitives(vertices, PrimitiveType.TriangleList);
            }
            finally
            {
                Ton.Gra.ResumeBatch();
            }
        }
    }
}
