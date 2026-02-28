using Microsoft.Xna.Framework;
using Mononotonka;

namespace Mononotonka
{
    /// <summary>
    /// TonConfigMenuの動作確認に特化したテスト用シーンです。
    /// </summary>
    public class SampleScene08 : IScene
    {
        private string _statusMessage = "Ready.";
        private int _sePlayCount = 0;
        private float _holdRButton = 0.0f;

        public void Initialize()
        {
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // TonConfigMenuの確認で使うBGM/SEをロードする
            Ton.Sound.LoadBGM("sample_assets/sound/bgm/tutorial", "tutorial");
            Ton.Sound.LoadSound("sample_assets/sound/se/coin", "coin");

            Ton.Sound.PlayBGM("tutorial");
            _statusMessage = "BGM started.";
            Ton.Log.Info("Scene " + this.GetType().Name + " Initialized.");
        }

        public void Terminate()
        {
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminating.");

            Ton.Sound.UnloadAll();

            Ton.Log.Info("Scene " + this.GetType().Name + " Terminated.");
        }

        public void Update(GameTime gameTime)
        {
            // ConfigMenuを開く
            if (Ton.Input.IsJustPressed("B"))
            {
                Ton.ConfigMenu.Open();
                return;
            }

            // R長押しで次シーンへ
            if (Ton.Input.IsPressed("R"))
            {
                _holdRButton += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_holdRButton >= 1.0f)
                {
                    Ton.Scene.Change(new SampleScene09(), 0.5f, 0.5f, Color.Red);
                }
            }
            else
            {
                _holdRButton = 0.0f;
            }

            // SE再生テスト
            if (Ton.Input.IsJustPressed("X"))
            {
                Ton.Sound.PlaySE("coin");
                _sePlayCount++;
                _statusMessage = "SE played.";
            }

            // BGM再生/停止テスト
            if (Ton.Input.IsJustPressed("Y"))
            {
                if (Ton.Sound.IsBGMPlaying())
                {
                    Ton.Sound.StopBGM(0.2f);
                    _statusMessage = "BGM stopped.";
                }
                else
                {
                    Ton.Sound.PlayBGM("tutorial", 0.2f, 1.0f);
                    _statusMessage = "BGM resumed.";
                }
            }

            // 手動ミュートAPI検証
            if (Ton.Input.IsJustPressed("A"))
            {
                Ton.Sound.SetSEMuted(!Ton.Sound.IsSEMuted());
                _statusMessage = "SE manual mute toggled.";
            }
            if (Ton.Input.IsJustPressed("L"))
            {
                Ton.Sound.SetBGMMuted(!Ton.Sound.IsBGMMuted());
                _statusMessage = "BGM manual mute toggled.";
            }
        }

        public void Draw()
        {
            Ton.Gra.Clear(Color.DarkSlateBlue);

            int y = 40;
            Ton.Gra.DrawText("Eight Scene: TonConfigMenu Test", 40, y, 0.8f);
            y += 40;

            Ton.Gra.DrawText($"Status: {_statusMessage}", 40, y, Color.Yellow, 0.65f);
            y += 30;

            var activityState = Ton.Game.GetWindowActivityState();
            Ton.Gra.DrawText($"Window State: {GetWindowStateLabel(activityState)}", 40, y, Color.Cyan, 0.65f);
            y += 30;
            Ton.Gra.DrawText($"Master Volume: {(int)(Ton.Sound.GetMasterVolume() * 100)}%", 40, y, Color.White, 0.65f);
            y += 30;
            Ton.Gra.DrawText($"Mute In Background: {(Ton.Sound.GetMuteWhenInactive() ? "ON" : "OFF")}", 40, y, Color.White, 0.65f);
            y += 30;
            Ton.Gra.DrawText($"BGM Playing: {(Ton.Sound.IsBGMPlaying() ? "YES" : "NO")}", 40, y, Color.White, 0.65f);
            y += 30;
            Ton.Gra.DrawText($"BGM Manual Mute: {(Ton.Sound.IsBGMMuted() ? "ON" : "OFF")}", 40, y, Color.White, 0.65f);
            y += 30;
            Ton.Gra.DrawText($"SE Manual Mute: {(Ton.Sound.IsSEMuted() ? "ON" : "OFF")}", 40, y, Color.White, 0.65f);
            y += 30;
            Ton.Gra.DrawText($"SE Played Count: {_sePlayCount}", 40, y, Color.White, 0.65f);

            Ton.Gra.DrawText("--- How To Test ---", 40, 360, Color.Cyan, 0.7f);
            Ton.Gra.DrawText("[B] Open Config Menu", 40, 400, 0.65f);
            Ton.Gra.DrawText("[X] Play SE (coin)   [Y] Toggle BGM Play/Stop", 40, 430, 0.65f);
            Ton.Gra.DrawText("[A] Toggle SE Manual Mute   [L] Toggle BGM Manual Mute", 40, 460, 0.65f);
            Ton.Gra.DrawText("Set 'Mute In Background' ON in Config, then Alt+Tab to verify mute.", 40, 500, 0.65f);

            Ton.Gra.DrawText("Hold the R button (Next Scene)", 700 - (int)(_holdRButton * 400.0f), 620, 0.6f + (_holdRButton));
        }

        /// <summary>
        /// ウィンドウ状態enumを画面表示用文字列に変換します。
        /// </summary>
        /// <param name="state">ウィンドウ状態</param>
        /// <returns>表示用文字列</returns>
        private string GetWindowStateLabel(TonWindowActivityState state)
        {
            switch (state)
            {
                case TonWindowActivityState.Active:
                    return "Active";
                case TonWindowActivityState.JustActivated:
                    return "JustActivated";
                case TonWindowActivityState.Inactive:
                    return "Inactive";
                case TonWindowActivityState.JustDeactivated:
                    return "JustDeactivated";
                default:
                    return "Unknown";
            }
        }
    }
}
