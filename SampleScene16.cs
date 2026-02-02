using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Mononotonka
{
    /// <summary>
    /// 魔法エフェクト（TonMagicEffect）のデモシーン
    /// </summary>
    public class SampleScene16 : IScene
    {
        private int _selectedLevel = 1;
        private string _lastAction = "";
        private float _cooldown = 0;

        public void Initialize()
        {
            // パーティクル画像を読み込み
            Ton.Gra.LoadTexture("shader/image/fire_particle", "shader/image/fire_particle");
            Ton.Gra.LoadTexture("shader/image/ice_particle", "shader/image/ice_particle");
            Ton.Gra.LoadTexture("shader/image/wind_particle", "shader/image/wind_particle");
            Ton.Gra.LoadTexture("shader/image/earth_particle", "shader/image/earth_particle");
            Ton.Gra.LoadTexture("shader/image/heal_particle", "shader/image/heal_particle");
            Ton.Gra.LoadTexture("shader/image/poison_particle", "shader/image/poison_particle");
            Ton.Gra.LoadTexture("shader/image/light_particle", "shader/image/light_particle");
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _cooldown -= dt;

            // 左右でレベル選択
            if(Ton.Input.IsJustPressed("Right"))
            {
                _selectedLevel++;
                if (_selectedLevel > 4)
                {
                    _selectedLevel = 1;
                }
            }
            if (Ton.Input.IsJustPressed("Left"))
            {
                _selectedLevel--;
                if (_selectedLevel < 1)
                {
                    _selectedLevel = 4;
                }
            }

            float centerX = Ton.Game.VirtualWidth / 2f;
            float centerY = Ton.Game.VirtualHeight / 2f + 200;

            // Bボタンでファイア発動
            if (_cooldown <= 0 && Ton.Input.IsJustPressed("B"))
            {
                Ton.Magic.Fire(centerX, centerY, _selectedLevel);
                
                string[] names = { "", "Fire", "Fira", "Firaga", "Firaja" };
                _lastAction = $"{names[_selectedLevel]} (Level {_selectedLevel})";
                _cooldown = 0.5f;
            }

            // Xボタンでブリザド発動
            if (_cooldown <= 0 && Ton.Input.IsJustPressed("X"))
            {
                Ton.Magic.Ice(centerX, centerY, _selectedLevel);
                
                string[] names = { "", "Blizzard", "Blizzara", "Blizzaga", "Blizzaja" };
                _lastAction = $"{names[_selectedLevel]} (Level {_selectedLevel})";
                _cooldown = 0.5f;
            }

            // Yボタンでエアロ発動
            if (_cooldown <= 0 && Ton.Input.IsJustPressed("Y"))
            {
                Ton.Magic.Wind(centerX, centerY, _selectedLevel);
                
                string[] names = { "", "Aero", "Aerora", "Aeroga", "Aeroja" };
                _lastAction = $"{names[_selectedLevel]} (Level {_selectedLevel})";
                _cooldown = 0.5f;
            }

            // Lボタンでクエイク発動
            if (_cooldown <= 0 && Ton.Input.IsJustPressed("L"))
            {
                Ton.Magic.Earth(centerX, centerY, _selectedLevel);
                
                string[] names = { "", "Quake", "Quakera", "Quakega", "Quakeja" };
                _lastAction = $"{names[_selectedLevel]} (Level {_selectedLevel})";
                _cooldown = 0.5f;
            }

            // Rボタンでケアル発動
            if (_cooldown <= 0 && Ton.Input.IsJustPressed("R"))
            {
                Ton.Magic.Heal(centerX, centerY, _selectedLevel);
                
                string[] names = { "", "Cure", "Cura", "Curaga", "Curaja" };
                _lastAction = $"{names[_selectedLevel]} (Level {_selectedLevel})";
                _cooldown = 0.5f;
            }

            // 上キーでポイズン発動
            if (_cooldown <= 0 && Ton.Input.IsJustPressed("Up"))
            {
                Ton.Magic.Poison(centerX, centerY, _selectedLevel);
                
                string[] names = { "", "Poison", "Poisora", "Poisoga", "Poisoja" };
                _lastAction = $"{names[_selectedLevel]} (Level {_selectedLevel})";
                _cooldown = 0.5f;
            }

            // 下キーでホーリー発動
            if (_cooldown <= 0 && Ton.Input.IsJustPressed("Down"))
            {
                Ton.Magic.Light(centerX, centerY, _selectedLevel);
                
                string[] names = { "", "Holy", "Holyra", "Holyga", "Holyja" };
                _lastAction = $"{names[_selectedLevel]} (Level {_selectedLevel})";
                _cooldown = 0.5f;
            }

            // Aボタン長押しで戻る
            if (Ton.Input.GetPressedDuration("A") > 1.0f)
            {
                Ton.Scene.Change(new SampleScene01(), 0.5f, 0.2f, Color.OrangeRed);
            }
        }

        public void Draw()
        {
            // 背景
            int nColor = (int)(Math.Sin(Ton.Game.GetTotalGameTime().TotalSeconds * 2.0) * 127.0) + 127;
            Ton.Gra.Clear(new Color(nColor, nColor, nColor));

            // タイトル
            Ton.Gra.DrawText("Magic Effect Demo", 20, 20, Color.White, 0.8f);
            
            // 操作説明
            Ton.Gra.DrawText("B:Fire X:Ice Y:Wind L:Earth R:Cure", 20, 60, Color.Gray, 0.5f);
            Ton.Gra.DrawText("Up:Poison Down:Holy  Left/Right:Level", 20, 80, Color.Gray, 0.5f);
            
            // 選択中のレベル表示
            string[] fireNames = { "", "Fire", "Fira", "Firaga", "Firaja" };
            string[] iceNames = { "", "Blizzard", "Blizzara", "Blizzaga", "Blizzaja" };
            string[] windNames = { "", "Aero", "Aerora", "Aeroga", "Aeroja" };
            string[] earthNames = { "", "Quake", "Quakera", "Quakega", "Quakeja" };
            string[] healNames = { "", "Cure", "Cura", "Curaga", "Curaja" };
            string[] poisonNames = { "", "Poison", "Poisora", "Poisoga", "Poisoja" };
            string[] lightNames = { "", "Holy", "Holyra", "Holyga", "Holyja" };
            Color[] fireColors = { Color.White, Color.Orange, Color.OrangeRed, Color.Red, Color.DarkRed };
            Color[] iceColors = { Color.White, Color.LightBlue, Color.DeepSkyBlue, Color.Blue, Color.DarkBlue };
            Color[] windColors = { Color.White, Color.LightGreen, Color.LimeGreen, Color.Green, Color.DarkGreen };
            Color[] earthColors = { Color.White, Color.SandyBrown, Color.Peru, Color.SaddleBrown, Color.Maroon };
            Color[] healColors = { Color.White, Color.PaleGreen, Color.LawnGreen, Color.MediumSeaGreen, Color.SeaGreen };
            Color[] poisonColors = { Color.White, new Color(200, 100, 255), new Color(180, 80, 220), new Color(150, 60, 200), new Color(120, 40, 180) };
            Color[] lightColors = { Color.White, Color.LightGoldenrodYellow, Color.Gold, Color.Goldenrod, Color.DarkGoldenrod };
            
            for (int i = 1; i <= 4; i++)
            {
                string marker = i == _selectedLevel ? "> " : "  ";
                Color fc = i == _selectedLevel ? fireColors[i] : Color.Gray;
                Color ic = i == _selectedLevel ? iceColors[i] : Color.Gray;
                Color wc = i == _selectedLevel ? windColors[i] : Color.Gray;
                Color ec = i == _selectedLevel ? earthColors[i] : Color.Gray;
                Color hc = i == _selectedLevel ? healColors[i] : Color.Gray;
                Color pc = i == _selectedLevel ? poisonColors[i] : Color.Gray;
                Color lc = i == _selectedLevel ? lightColors[i] : Color.Gray;
                Ton.Gra.DrawText($"{marker}{i}: {fireNames[i]}", 20, 110 + (i - 1) * 30, fc, 0.6f);
                Ton.Gra.DrawText($"{iceNames[i]}", 180, 110 + (i - 1) * 30, ic, 0.6f);
                Ton.Gra.DrawText($"{windNames[i]}", 300, 110 + (i - 1) * 30, wc, 0.6f);
                Ton.Gra.DrawText($"{earthNames[i]}", 400, 110 + (i - 1) * 30, ec, 0.6f);
                Ton.Gra.DrawText($"{healNames[i]}", 520, 110 + (i - 1) * 30, hc, 0.6f);
                Ton.Gra.DrawText($"{poisonNames[i]}", 620, 110 + (i - 1) * 30, pc, 0.6f);
                Ton.Gra.DrawText($"{lightNames[i]}", 780, 110 + (i - 1) * 30, lc, 0.6f);
            }

            // 最後に発動した魔法
            if (!string.IsNullOrEmpty(_lastAction))
            {
                Ton.Gra.DrawText($"Cast: {_lastAction}", 20, 260, Color.Yellow, 0.6f);
            }

            // アクティブエフェクト数
            Ton.Gra.DrawText($"Active Effects: {Ton.Magic.ActiveCount}", 20, Ton.Game.VirtualHeight - 40, Color.Gray, 0.5f);

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the A button (Next Scene)", 700 - (int)(Ton.Input.GetPressedDuration("A") * 400.0f), 160, 0.6f + (float)Ton.Input.GetPressedDuration("A"));

        }

        public void Terminate()
        {
        }
    }
}
