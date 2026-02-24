using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Mononotonka
{
    /// <summary>
    /// コンフィグメニュー管理クラス。
    /// ゲーム内設定変更用UI（フルスクリーン切替、音量、メッセージ速度）を提供します。
    /// </summary>
    public class TonConfigMenu
    {
        private bool _isOpen = false;
        private int _selectedIndex = 0;
        private string[] _items = { "Fullscreen", "Resolution", "Master Volume", "Message Speed", "Close" };
        private const string CONFIG_FILENAME = "config.json";
        
        // Resolution List
        private System.Collections.Generic.List<Point> _resolutions;
        private int _resolutionIndex = 0;
        
        // 設定値保持
        // 設定値保持
        private int _msgSpeedIndex = 2; // Default 100
        private int[] _msgSpeeds = { 10, 30, 50, 75, 100, 150, 200 };
        private bool _isDirty = false;

        // 保存用データ
        public class ConfigData
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public bool IsFullScreen { get; set; }
            public float MasterVolume { get; set; }
            public int MsgSpeed { get; set; }
        }

        public void Initialize()
        {
            var data = Ton.Storage.Load<ConfigData>(CONFIG_FILENAME);
            if (data != null)
            {
                // ウィンドウサイズ
                // TonGame.Initializeで既に設定されている場合もあるが、ここで上書き適用
                Ton.Game.SetWindowSize(data.Width, data.Height);
                Ton.Game.ToggleFullScreen(data.IsFullScreen);
                Ton.Sound.SetMasterVolume(data.MasterVolume);
                
                // メッセージ速度
                // 既存リストから近いものを探す
                int closestIndex = 0;
                int minDiff = int.MaxValue;
                for (int i = 0; i < _msgSpeeds.Length; i++)
                {
                    int diff = Math.Abs(_msgSpeeds[i] - data.MsgSpeed);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        closestIndex = i;
                    }
                }
                _msgSpeedIndex = closestIndex;
                Ton.Msg.SetTextSpeed(_msgSpeeds[_msgSpeedIndex]);
                
                Ton.Log.Info("ConfigMenu: Loaded config.json");
                Ton.Log.Info($"[Config] Res: {data.Width}x{data.Height}, Full: {data.IsFullScreen}, Vol: {data.MasterVolume:F2}, MsgSpeed: {data.MsgSpeed}");
            }
        }

        /// <summary>
        /// メニューを開きます。
        /// </summary>
        public void Open()
        {
            Ton.Log.Info("ConfigMenu.Open Called");
            Ton.Input.ConsumeInput(); // 同じフレームで閉じないように入力を消費
            _isOpen = true;
            _selectedIndex = 0;
            
            // 解像度リスト取得
            _resolutions = Ton.Game.GetAvailableResolutions();

            // 現在の解像度に合わせる
            int w = Ton.Game.WindowWidth;
            int h = Ton.Game.WindowHeight;
            _resolutionIndex = _resolutions.FindIndex(pt => pt.X == w && pt.Y == h);
            if (_resolutionIndex < 0) _resolutionIndex = 0;
        }

        /// <summary>
        /// メニューを閉じます。
        /// </summary>
        public void Close()
        {
            Ton.Log.Info("ConfigMenu.Close Called");
            _isOpen = false;

            if (_isDirty)
            {
                // 設定保存
                var data = new ConfigData
                {
                    Width = Ton.Game.WindowWidth,
                    Height = Ton.Game.WindowHeight,
                    IsFullScreen = Ton.Game.IsFullScreen,
                    MasterVolume = (float)Math.Round(Ton.Sound.GetMasterVolume(), 2),
                    MsgSpeed = _msgSpeeds[_msgSpeedIndex]
                };
                Ton.Storage.Save(CONFIG_FILENAME, data);
                _isDirty = false;
                Ton.Log.Info($"ConfigMenu: Saved {CONFIG_FILENAME}");
                Ton.Log.Info($"[Config] Res: {data.Width}x{data.Height}, Full: {data.IsFullScreen}, Vol: {data.MasterVolume:F2}, MsgSpeed: {data.MsgSpeed}");
            }
        }

        /// <summary>
        /// メニューが開いているかどうかを返します。
        /// </summary>
        public bool IsOpen()
        {
            return _isOpen;
        }

        /// <summary>
        /// メニューの更新処理。入力操作を行います。
        /// </summary>
        public void Update()
        {
            // 専用ボタンか、開発者がUpdate内で呼び出す前提
            
            if (!_isOpen) return;

            // カーソル移動
            if (Ton.Input.IsJustPressed("Up"))
            {
                _selectedIndex--;
                if (_selectedIndex < 0) _selectedIndex = _items.Length - 1;
                // フルスクリーン中はResolution項目をスキップ
                if (_selectedIndex == 1 && Ton.Game.IsFullScreen) _selectedIndex--;
            }
            if (Ton.Input.IsJustPressed("Down"))
            {
                _selectedIndex++;
                if (_selectedIndex >= _items.Length) _selectedIndex = 0;
                // フルスクリーン中はResolution項目をスキップ
                if (_selectedIndex == 1 && Ton.Game.IsFullScreen) _selectedIndex++;
            }

            // 設定変更
            if (Ton.Input.IsJustPressed("Left") || Ton.Input.IsJustPressed("Right"))
            {
                int dir = Ton.Input.IsJustPressed("Right") ? 1 : -1;
                ChangeSetting(dir);
            }
            
            // 決定 / キャンセル
            if (Ton.Input.IsJustPressed("A"))
            {
                if (_items[_selectedIndex] == "Close") Close();
                else ChangeSetting(1);
            }
            
            if (Ton.Input.IsJustPressed("B"))
            {
                Ton.Input.ConsumeInput(); // 閉じた直後のシーン側の誤動作を防ぐ
                Close();
            }
        }

        private void ChangeSetting(int dir)
        {
            _isDirty = true; // 変更フラグON

            switch (_selectedIndex)
            {
                case 0: // Fullscreen
                    Ton.Game.ToggleFullScreen(!Ton.Game.IsFullScreen);
                    break;
                case 1: // Resolution
                    // フルスクリーン中は解像度変更を無効化（チカチカ防止）
                    if (Ton.Game.IsFullScreen) break;
                    
                    if (_resolutions != null && _resolutions.Count > 0)
                    {
                        _resolutionIndex += dir;
                        if (_resolutionIndex < 0) _resolutionIndex = _resolutions.Count - 1;
                        if (_resolutionIndex >= _resolutions.Count) _resolutionIndex = 0;
                        
                        Point res = _resolutions[_resolutionIndex];

                        // 解像度変更して、センタリングする
                        Ton.Game.SetWindowSize(res.X, res.Y);
                        Ton.Game.CenterWindow();
                    }
                    break;
                case 2: // Volume
                    float currentVol = Ton.Sound.GetMasterVolume();
                    float step = 0.02f;
                    if (currentVol >= 0.40f - 0.001f)
                    {
                        step = 0.05f;
                    }

                    // Lowering volume from exactly 0.40f should use small step?
                    // Request: "40% or more is 5% steps", "39% or less is 2% steps"
                    // If at 0.40 (40%), going up -> 0.45 (+0.05). Correct.
                    // If at 0.40 (40%), going down -> 0.38 (-0.02).
                    // If at 0.38 (38%), going up -> 0.40 (+0.02). Correct.
                    
                    // Logic:
                    // If going UP (dir > 0):
                    //   If current >= 0.39f, add 0.05f (to reach 0.45 from 0.40).
                    //   Else add 0.02f.
                    // If going DOWN (dir < 0):
                    //   If current > 0.41f, subtract 0.05f.
                    //   Else subtract 0.02f.
                    
                    float nextVol = currentVol;
                    if (dir > 0)
                    {
                        if (currentVol >= 0.40f - 0.001f) step = 0.05f;
                        else step = 0.02f;
                        nextVol += step;
                    }
                    else
                    {
                        if (currentVol > 0.40f + 0.001f) step = 0.05f;
                        else step = 0.02f;
                        nextVol -= step;
                    }

                    // Round to avoid float drift (2 decimal places)
                    nextVol = (float)Math.Round(nextVol, 2);
                    Ton.Sound.SetMasterVolume(MathHelper.Clamp(nextVol, 0f, 1f));
                    break;
                case 3: // Msg Speed
                    _msgSpeedIndex += dir;
                    if (_msgSpeedIndex < 0) _msgSpeedIndex = 0;
                    if (_msgSpeedIndex >= _msgSpeeds.Length) _msgSpeedIndex = _msgSpeeds.Length - 1;
                    
                    // メッセージ速度反映
                    Ton.Msg.SetTextSpeed(_msgSpeeds[_msgSpeedIndex]);
                    Ton.Log.Info($"Config: MsgSpeed set to {_msgSpeeds[_msgSpeedIndex]}");
                    break;
            }
        }

        /// <summary>
        /// コンフィグメニューを描画します。
        /// </summary>
        public void Draw()
        {
            if (!_isOpen) return;

            // オーバーレイ（暗くする）
            int w = Ton.Game.VirtualWidth;
            int h = Ton.Game.VirtualHeight;
            Ton.Gra.FillRect(10, 10, w - 20, h - 20, new Color(0, 0, 0, 180));

            // メニュータイトル
            Ton.Gra.DrawText("Config Menu", 50, 50, 1.0f);

            // メニュー項目描画
            int startY = 150;
            for (int i = 0; i < _items.Length; i++)
            {
                // フルスクリーン中は Resolution をグレー表示
                bool isDisabled = (i == 1 && Ton.Game.IsFullScreen);
                Color c = isDisabled ? Color.Gray : (i == _selectedIndex) ? Color.Yellow : Color.White;
                string label = _items[i];
                string valueText = "";

                // 値の取得
                switch(i)
                {
                    case 0: // Fullscreen
                        valueText = Ton.Game.IsFullScreen ? "ON" : "OFF";
                        break;
                    case 1: // Resolution
                        if (_resolutions != null && _resolutions.Count > 0)
                            valueText = $"{_resolutions[_resolutionIndex].X} x {_resolutions[_resolutionIndex].Y}";
                        break;
                    case 2: // Volume
                        valueText = $"{(int)Math.Round(Ton.Sound.GetMasterVolume() * 100)} %";
                        break;
                    case 3: // Msg Speed
                        valueText = $"{_msgSpeeds[_msgSpeedIndex]} ms/char";
                        break;
                    case 4: // Close
                        // 値なし
                        break;
                }
                
                // 描画(解像度が変わると問題出る可能性あるので左上に描く)
                Ton.Gra.DrawText(label, 50, startY + i * 50, c, 0.6f);
                if (!string.IsNullOrEmpty(valueText))
                {
                    Ton.Gra.DrawText(valueText, 300, startY + (i * 50), c, 0.6f);
                }
            }
        }
    }
}
