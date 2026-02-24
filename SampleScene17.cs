using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// 座標逆変換API（TryGetTexturePointFromDrawEx）のテストシーンです。
    /// 回転する四角形テクスチャ上を左クリックしている間、レインボー色の小円を描画します。
    /// </summary>
    public class SampleScene17 : IScene
    {
        /// <summary>
        /// テクスチャ上に描いた点情報です。
        /// </summary>
        private struct PaintDot
        {
            /// <summary>テクスチャ内ローカル座標です。</summary>
            public Vector2 LocalPoint;
            /// <summary>描画色です。</summary>
            public Color Color;
            /// <summary>半径です。</summary>
            public float Radius;
        }

        private const string GENERATED_TEXTURE_NAME = "scene17_generated_square";
        private const int GENERATED_TEXTURE_SIZE = 512;
        private const int CURSOR_SOURCE_X = 0;
        private const int CURSOR_SOURCE_Y = 320;
        private const int CURSOR_SIZE = 64;
        private const float DOT_RADIUS = 3.5f;
        private const int MAX_DOT_COUNT = 3000;
        private const float BASE_DRAW_SCALE = 0.72f;
        private const float SCALE_AMPLITUDE = 0.18f;
        private const float SCALE_SPEED = 2.4f;

        private readonly List<PaintDot> _paintDots = new List<PaintDot>();
        private readonly TonDrawParamEx _drawParam = new TonDrawParamEx();

        private string _textureName = GENERATED_TEXTURE_NAME;
        private int _sourceX = 0;
        private int _sourceY = 0;
        private int _sourceWidth = GENERATED_TEXTURE_SIZE;
        private int _sourceHeight = GENERATED_TEXTURE_SIZE;
        private Vector2 _drawCenter = Vector2.Zero;
        private float _rainbowPhase = 0.0f;
        private bool _isMouseOnTexture = false;
        private bool _isTextureGenerated = false;
        private Point _lastTexturePoint = Point.Zero;

        /// <summary>
        /// シーン開始時の初期化を行います。
        /// </summary>
        public void Initialize()
        {
            _textureName = GENERATED_TEXTURE_NAME;
            _sourceWidth = GENERATED_TEXTURE_SIZE;
            _sourceHeight = GENERATED_TEXTURE_SIZE;
            _isTextureGenerated = false;

            // 再入時に同名ターゲットが残っている場合に備えて一度解放してから作り直します。
            Ton.Gra.ReleaseRenderTarget(_textureName);
            Ton.Gra.CreateRenderTarget(_textureName, GENERATED_TEXTURE_SIZE, GENERATED_TEXTURE_SIZE);

            _drawCenter = new Vector2(Ton.Game.VirtualWidth * 0.5f, Ton.Game.VirtualHeight * 0.52f);

            _drawParam.ScaleX = BASE_DRAW_SCALE;
            _drawParam.ScaleY = BASE_DRAW_SCALE;
            _drawParam.Angle = 0.0f;
            _drawParam.Alpha = 1.0f;
            _drawParam.Color = Color.White;
            _drawParam.FlipH = false;
            _drawParam.FlipV = false;
        }

        /// <summary>
        /// 毎フレームの更新処理を行います。
        /// </summary>
        /// <param name="gameTime">経過時間情報です。</param>
        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // テクスチャを常時回転させて、逆変換の追従を確認します。
            _drawParam.Angle += elapsed * 1.7f;
            if (_drawParam.Angle > MathHelper.TwoPi)
            {
                _drawParam.Angle -= MathHelper.TwoPi;
            }

            // 時間経過に応じて、sin波で等倍拡大縮小させます。
            float pulse = MathF.Sin((float)Ton.Game.TotalGameTime.TotalSeconds * SCALE_SPEED);
            float scale = BASE_DRAW_SCALE + (pulse * SCALE_AMPLITUDE);
            _drawParam.ScaleX = scale;
            _drawParam.ScaleY = scale;

            Vector2 mousePosition = Ton.Input.GetMousePosition();
            _isMouseOnTexture = Ton.Gra.TryGetTexturePointFromDrawEx(
                mousePosition,
                _drawCenter.X,
                _drawCenter.Y,
                _sourceX,
                _sourceY,
                _sourceWidth,
                _sourceHeight,
                _drawParam,
                out _lastTexturePoint);

            // 左クリック中かつテクスチャ上を指している間だけ、点を追加します。
            if (_isMouseOnTexture && Ton.Input.IsMousePressed(MouseButton.Left))
            {
                Vector2 localPoint = new Vector2(
                    (_lastTexturePoint.X - _sourceX) + 0.5f,
                    (_lastTexturePoint.Y - _sourceY) + 0.5f);

                AddPaintDot(localPoint);
            }

            // Bボタンで描画結果をクリアします。
            if (Ton.Input.IsJustPressed("B"))
            {
                _paintDots.Clear();
            }

            // Aボタン長押しでシーン先頭へ戻ります。
            if (Ton.Input.GetPressedDuration("A") > 1.0f)
            {
                Ton.Scene.Change(new SampleScene01(), 0.5f, 0.2f, Color.DeepSkyBlue);
            }
        }

        /// <summary>
        /// 毎フレームの描画処理を行います。
        /// </summary>
        public void Draw()
        {
            EnsureGeneratedTexture();
            Ton.Gra.DrawBackground("landscape");

            Ton.Gra.DrawText("SampleScene17: Reverse Mapping Test", 20, 20, Color.White, 0.7f);
            Ton.Gra.DrawText("Hold Left Click on rotating square to paint.", 20, 55, Color.Gainsboro, 0.5f);
            Ton.Gra.DrawText("B: Clear  Hold A: Back to Scene01  (Generated 512x512)", 20, 80, Color.Gainsboro, 0.5f);

            Ton.Gra.DrawEx(_textureName, _drawCenter.X, _drawCenter.Y, _sourceX, _sourceY, _sourceWidth, _sourceHeight, _drawParam);

            // テクスチャローカル座標で保持した点を、毎フレーム現在の回転状態へ再投影して描画します。
            for (int i = 0; i < _paintDots.Count; i++)
            {
                PaintDot dot = _paintDots[i];
                Vector2 drawPos = ConvertLocalToVirtual(dot.LocalPoint);
                Ton.Primitive.DrawCircle(drawPos, dot.Radius, dot.Color, 14);
            }

            if (_isMouseOnTexture)
            {
                string posText = $"Tex: {_lastTexturePoint.X}, {_lastTexturePoint.Y}";
                Ton.Gra.DrawText(posText, 20, 110, Color.Yellow, 0.55f);
            }
            else
            {
                Ton.Gra.DrawText("Tex: out of area", 20, 110, Color.LightGray, 0.55f);
            }

            Vector2 mousePosition = Ton.Input.GetMousePosition();
            Ton.Gra.Draw("coin_animation", (int)(mousePosition.X - 31.0f), (int)(mousePosition.Y - 32.0f), CURSOR_SOURCE_X, CURSOR_SOURCE_Y, CURSOR_SIZE, CURSOR_SIZE);

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(Ton.Input.GetPressedDuration("A") * 400.0f), 160, 0.6f + (float)Ton.Input.GetPressedDuration("A"));

        }

        /// <summary>
        /// シーン終了時の後始末を行います。
        /// </summary>
        public void Terminate()
        {
            _paintDots.Clear();
            Ton.Gra.ReleaseRenderTarget(_textureName);
        }

        /// <summary>
        /// 初回描画時に、書き込み対象となる512x512の四角形テクスチャを自己生成します。
        /// </summary>
        private void EnsureGeneratedTexture()
        {
            if (_isTextureGenerated)
            {
                return;
            }

            Ton.Gra.SetRenderTarget(_textureName);
            Ton.Gra.Clear(new Color(12, 14, 24));

            // 縦方向グラデーション
            for (int y = 0; y < GENERATED_TEXTURE_SIZE; y++)
            {
                float t = y / (GENERATED_TEXTURE_SIZE - 1.0f);
                byte r = (byte)(18 + (40 * t));
                byte g = (byte)(24 + (100 * t));
                byte b = (byte)(48 + (130 * t));
                Ton.Gra.FillRect(0, y, GENERATED_TEXTURE_SIZE, 1, new Color(r, g, b));
            }

            // チェッカーを薄く重ねて質感を出す
            const int cell = 32;
            for (int y = 0; y < GENERATED_TEXTURE_SIZE; y += cell)
            {
                for (int x = 0; x < GENERATED_TEXTURE_SIZE; x += cell)
                {
                    bool odd = (((x / cell) + (y / cell)) % 2) == 0;
                    Ton.Gra.FillRect(x, y, cell, cell, odd ? new Color(255, 255, 255, 18) : new Color(0, 0, 0, 16));
                }
            }

            // 放射ライン
            Vector2 center = new Vector2(GENERATED_TEXTURE_SIZE * 0.5f, GENERATED_TEXTURE_SIZE * 0.5f);
            for (int i = 0; i < 48; i++)
            {
                float angle = MathHelper.TwoPi * i / 48.0f;
                float inner = 56.0f + ((i % 3) * 6.0f);
                float outer = 248.0f;
                Vector2 p1 = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * inner;
                Vector2 p2 = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * outer;
                Ton.Gra.DrawLine(p1, p2, new Color(255, 255, 255, 28), 1.0f);
            }

            // 枠と対角線
            Ton.Gra.DrawRect(0, 0, GENERATED_TEXTURE_SIZE, GENERATED_TEXTURE_SIZE, new Color(240, 240, 255, 180), 3);
            Ton.Gra.DrawRect(12, 12, GENERATED_TEXTURE_SIZE - 24, GENERATED_TEXTURE_SIZE - 24, new Color(180, 220, 255, 120), 2);
            Ton.Gra.DrawLine(0, 0, GENERATED_TEXTURE_SIZE - 1, GENERATED_TEXTURE_SIZE - 1, new Color(255, 255, 255, 38), 1.0f);
            Ton.Gra.DrawLine(0, GENERATED_TEXTURE_SIZE - 1, GENERATED_TEXTURE_SIZE - 1, 0, new Color(255, 255, 255, 38), 1.0f);

            Ton.Gra.SetRenderTarget();
            _isTextureGenerated = true;
        }

        /// <summary>
        /// テクスチャ内ローカル座標の点を追加します。
        /// </summary>
        /// <param name="localPoint">テクスチャ内ローカル座標です。</param>
        private void AddPaintDot(Vector2 localPoint)
        {
            PaintDot dot = new PaintDot
            {
                LocalPoint = localPoint,
                Color = CreateRainbowColor(_rainbowPhase),
                Radius = DOT_RADIUS
            };

            _paintDots.Add(dot);
            _rainbowPhase += 0.19f;

            if (_paintDots.Count > MAX_DOT_COUNT)
            {
                _paintDots.RemoveAt(0);
            }
        }

        /// <summary>
        /// 位相差を使ってレインボーカラーを生成します。
        /// </summary>
        /// <param name="phase">色相位相です。</param>
        /// <returns>生成された色です。</returns>
        private Color CreateRainbowColor(float phase)
        {
            float r = (MathF.Sin(phase) + 1.0f) * 0.5f;
            float g = (MathF.Sin(phase + (MathHelper.TwoPi / 3.0f)) + 1.0f) * 0.5f;
            float b = (MathF.Sin(phase + (MathHelper.TwoPi * 2.0f / 3.0f)) + 1.0f) * 0.5f;
            return new Color(r, g, b, 1.0f);
        }

        /// <summary>
        /// テクスチャローカル座標を現在の描画座標（仮想画面座標）へ変換します。
        /// </summary>
        /// <param name="localPoint">テクスチャローカル座標です。</param>
        /// <returns>変換後の仮想画面座標です。</returns>
        private Vector2 ConvertLocalToVirtual(Vector2 localPoint)
        {
            float scaleX = _drawParam.ScaleX * (_drawParam.FlipH ? -1.0f : 1.0f);
            float scaleY = _drawParam.ScaleY * (_drawParam.FlipV ? -1.0f : 1.0f);

            Vector2 centered = localPoint - new Vector2(_sourceWidth * 0.5f, _sourceHeight * 0.5f);
            Vector2 scaled = new Vector2(centered.X * scaleX, centered.Y * scaleY);

            float cos = MathF.Cos(_drawParam.Angle);
            float sin = MathF.Sin(_drawParam.Angle);
            Vector2 rotated = new Vector2(
                scaled.X * cos - scaled.Y * sin,
                scaled.X * sin + scaled.Y * cos);

            return _drawCenter + rotated;
        }
    }
}
