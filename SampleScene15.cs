using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Mononotonka
{
    public class SampleScene15 : IScene
    {
        private float _time = 0;
        
        // リボン（剣の軌跡）用
        private List<Vector2> _trailPoints = new List<Vector2>();
        private float _slashTimer = 0;
        private const float SLASH_INTERVAL = 1.0f; // 1秒ごとに斬る
        private const float SLASH_DURATION = 0.2f; // 斬撃の持続時間
        private bool _isSlashing = false;
        private Vector2 _slashStartPos = new Vector2(200, 500);
        private Vector2 _slashEndPos = new Vector2(600, 200);

        // リボン（8の字）用
        private List<Vector2> _figure8Points = new List<Vector2>();
        private float _figure8Timer = 0;

        public void Initialize()
        {
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _time += dt;

            // --- 1. Ribbon Animation (Auto Slash) ---
            _slashTimer += dt;
            if (_slashTimer >= SLASH_INTERVAL)
            {
                _slashTimer = 0;
                _isSlashing = true;
                _trailPoints.Clear();
                
                // 斬撃の位置を少しランダムに変える
                var rand = new Random();
                _slashStartPos = new Vector2(200 + rand.Next(-100, 100), 500 + rand.Next(-100, 100));
                _slashEndPos = new Vector2(600 + rand.Next(-100, 100), 200 + rand.Next(-100, 100));
            }

            if (_isSlashing)
            {
                if (_slashTimer > SLASH_DURATION)
                {
                    _isSlashing = false;
                }
                else
                {
                    // 斬撃進行度 (0.0 - 1.0)
                    float t = _slashTimer / SLASH_DURATION;
                    
                    // 円弧を描くように補間 (ベジェ曲線)
                    Vector2 control = new Vector2((_slashStartPos.X + _slashEndPos.X) * 0.5f - 200, (_slashStartPos.Y + _slashEndPos.Y) * 0.5f - 200);
                    
                    // 2次ベジェ P = (1-t)^2 P0 + 2(1-t)t P1 + t^2 P2
                    Vector2 p = Vector2.Zero;
                    float u = 1 - t;
                    p += u * u * _slashStartPos;
                    p += 2 * u * t * control;
                    p += t * t * _slashEndPos;

                    _trailPoints.Add(p);
                }
            }
            
            // 古い点を削除して軌跡が消えていくようにする
            // 斬撃中は少し残し、終わったら急速に消す
            if (_trailPoints.Count > 20 || (!_isSlashing && _trailPoints.Count > 0))
            {
                // 一気に全部消さず、徐々に消す
                 int removeCount = _isSlashing ? 1 : 2;
                 for(int i=0; i<removeCount && _trailPoints.Count > 0; i++) 
                    _trailPoints.RemoveAt(0);
            }

            // --- 1-B. Ribbon Animation (Figure-8 Flutter) ---
            _figure8Timer += dt;
            // レムニスケート（8の字）軌道: x = a * cos(t), y = a * sin(2t) / 2
            // 少し位置をずらして画面右上に
            float t8 = _figure8Timer * 3.0f; // 速度
            float scale8 = 200f;
            Vector2 center8 = new Vector2(1000, 200);
            
            // ヒラヒラ感（ノイズを加える）
            float flutter = (float)Math.Sin(t8 * 10f) * 5f;

            Vector2 pos8 = center8 + new Vector2(
                (float)Math.Cos(t8) * scale8,
                (float)Math.Sin(2 * t8) * scale8 * 0.5f + flutter
            );
            
            _figure8Points.Add(pos8);
            if (_figure8Points.Count > 40)
            {
                _figure8Points.RemoveAt(0);
            }

            // Aボタンで戻る
            if (Ton.Input.GetPressedDuration("A") > 1.0f)
            {
                Ton.Scene.Change(new SampleScene16(), 0.5f, 0.2f, Color.Gold);
            }
        }

        public void Draw()
        {
            Ton.Gra.DrawBackground("landscape");
            Ton.Gra.DrawText("Advanced Effects Demo (Auto)", 20, 20, 0.7f);

            // 1. Ribbon (剣の軌跡)
            if (_trailPoints.Count > 1)
            {
                // 青白く光る軌跡
                // 先端は太く、後ろは細く
                Ton.Primitive.DrawRibbon(
                    _trailPoints,
                    5.0f,  // 始点（尻尾）
                    40.0f, // 終点（先端）
                    Color.Blue * 0.0f,    // 尻尾は透明
                    Color.Cyan            // 先端は発光色
                );
            }
            Ton.Gra.DrawText("Auto Slash Ribbon", 200, 150, 0.5f);

            // 1-B. Figure-8 Ribbon
            if (_figure8Points.Count > 1)
            {
                Ton.Primitive.DrawRibbon(
                    _figure8Points,
                    30f, 0f, 
                    Color.HotPink, Color.Transparent
                );
            }
            Ton.Gra.DrawText("Fluttering Ribbon", 900, 320, 0.5f);

            // 1-C. 固定リボン
            Ton.Primitive.DrawRibbon(
                new List<Vector2> {
                    new Vector2(50, 500),
                    new Vector2(150, 550),
                    new Vector2(250, 520),
                    new Vector2(350, 600),
                    new Vector2(450, 580),
                    new Vector2(550, 650)
                },
                10f, 30f,
                Color.OrangeRed,
                Color.Yellow * 0.5f
            );
            Ton.Gra.DrawText("Fix Ribbon", 200, 680, 0.5f);

            // 2. Bolt (稲妻)
            Vector2 pole1 = new Vector2(100, 400);
            Vector2 pole2 = new Vector2(400, 400);

            Ton.Primitive.DrawCircle(pole1, 10f, Color.Gray);
            Ton.Primitive.DrawCircle(pole2, 10f, Color.Gray);

            // 弱い雷 (チリチリ)
            // 秒間15回形状変化
            Ton.Primitive.DrawBolt(pole1, pole2, 2f, Color.Yellow, 0.3f, 15f);
            Ton.Gra.DrawText("Weak Bolt (15 updates/sec)", 150, 420, 0.5f);

            // 強い雷 (ズバババン)
            Vector2 pole3 = new Vector2(600, 200);
            Vector2 pole4 = new Vector2(800, 500);
            Ton.Primitive.DrawCircle(pole3, 15f, Color.Red);
            Ton.Primitive.DrawCircle(pole4, 15f, Color.Red);
            
            Ton.Primitive.DrawBolt(pole3, pole4, 8f, Color.White, 1.5f, 30f); // 芯（白）
            Ton.Primitive.DrawBolt(pole3, pole4, 15f, Color.LightGoldenrodYellow * 0.5f, 1.5f, 30f); // 外光（紫）
            Ton.Gra.DrawText("Strong Bolt (30 updates/sec)", 650, 520, 0.5f);

            // 3. Focus Lines (集中線)
            // 画面中心に向かって
            // 強度を時間で変化させる
            float intensity = 0.8f + (float)Math.Sin(_time * 5) * 0.2f;
            
            // 秒間12回更新のアニメーション集中線
            Ton.Primitive.DrawFocusLines(
                new Vector2(Ton.Game.VirtualWidth / 2, Ton.Game.VirtualHeight / 2),
                Ton.Game.VirtualWidth * 1.5f, // 画面外から
                intensity,
                Color.White * 0.1f, // 薄く
                12f // 更新頻度
            );

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(Ton.Input.GetPressedDuration("A") * 400.0f), 160, 0.6f + (float)Ton.Input.GetPressedDuration("A"));
        }

        public void Terminate()
        {
        }
    }
}
