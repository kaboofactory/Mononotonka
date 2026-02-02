using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Mononotonka
{
    /// <summary>
    /// 入力管理クラスです。
    /// キーボード、ゲームパッドの状態を取得し、ボタン入力として処理します。
    /// </summary>
    /// <summary>
    /// マウスボタンの定義
    /// </summary>
    public enum MouseButton
    {
        Left,
        Right,
        Middle,
        XButton1,
        XButton2
    }

    public class TonInput
    {
        private KeyboardState _currentKeyboard;
        private KeyboardState _prevKeyboard;
        private GamePadState _currentGamePad;
        private GamePadState _prevGamePad;
        private MouseState _currentMouse;
        private MouseState _prevMouse;

        private float _vibrationTimer = 0f;

        private Dictionary<string, List<Keys>> _keyMap;
        private Dictionary<string, List<Buttons>> _buttonMap;
        
        // ボタン押下時間の計測用
        private Dictionary<string, double> _pressedDuration = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _registeredButtonNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private bool _isInputConsumed = false;

        /// <summary>
        /// コンストラクタ。デフォルトのキーマッピングを設定します。
        /// </summary>
        public TonInput()
        {
            _keyMap = new Dictionary<string, List<Keys>>(StringComparer.OrdinalIgnoreCase);
            _buttonMap = new Dictionary<string, List<Buttons>>(StringComparer.OrdinalIgnoreCase);
            
            // デフォルトマッピング
            // 矢印キー=十字キー, Z=A, X=B, C=X, A=Y
            RegisterButton("Up", Keys.Up, Buttons.DPadUp);
            RegisterButton("Down", Keys.Down, Buttons.DPadDown);
            RegisterButton("Left", Keys.Left, Buttons.DPadLeft);
            RegisterButton("Right", Keys.Right, Buttons.DPadRight);
            
            RegisterButton("A", Keys.Z, Buttons.A);
            RegisterButton("B", Keys.X, Buttons.B);
            RegisterButton("X", Keys.C, Buttons.X);
            RegisterButton("Y", Keys.A, Buttons.Y);

            RegisterButton("R", Keys.W, Buttons.RightShoulder);
            RegisterButton("L", Keys.Q, Buttons.LeftShoulder);

            RegisterButton("Start", Keys.Enter, Buttons.Start);
            RegisterButton("Select", Keys.Space, Buttons.Back);
        }

        /// <summary>
        /// 初期化処理。外部ファイルからキーコンフィグを読み込みます。
        /// </summary>
        public void Initialize()
        {
            // input.configの読み込み
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "input.config");
            if (System.IO.File.Exists(path))
            {
                var lines = System.IO.File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string name = parts[0].Trim();
                        string[] bindings = parts[1].Split(',');
                        foreach (var bind in bindings)
                        {
                            string b = bind.Trim();
                            if (Enum.TryParse<Keys>(b, out Keys k)) RegisterButton(name, k, (Buttons)(-1));
                            else if (Enum.TryParse<Buttons>(b, out Buttons btn)) RegisterButton(name, Keys.None, btn);
                        }
                    }
                }
                Ton.Log.Info($"Loaded Input Config: {path}");
            }
            else
            {
                Ton.Log.Info($"Input Config not found: {path} (Using defaults)");
            }
            
            // 登録されたボタン情報のログ出力
            foreach (var name in _registeredButtonNames)
            {
                string keys = _keyMap.ContainsKey(name) ? string.Join(", ", _keyMap[name]) : "None";
                string buttons = _buttonMap.ContainsKey(name) ? string.Join(", ", _buttonMap[name]) : "None";
                Ton.Log.Info($"[Input] Registered: {name} (Keys: {keys}, Buttons: {buttons})");
            }

            // 初期ゲームパッド状態ログ
            var gp = GamePad.GetState(PlayerIndex.One);
            var cap = GamePad.GetCapabilities(PlayerIndex.One);
            Ton.Log.Info($"Initial GamePad Status: {(gp.IsConnected ? "Connected" : "Disconnected")}");
            if (gp.IsConnected)
            {
                Ton.Log.Info($"GamePad Name: {cap.DisplayName}");
            }
        }

        /// <summary>
        /// 仮想ボタンに入力を割り当てます。
        /// </summary>
        /// <param name="name">仮想ボタン名</param>
        /// <param name="key">キーボードのキー</param>
        /// <param name="btn">ゲームパッドのボタン</param>
        public void RegisterButton(string name, Keys key, Buttons btn)
        {
            if (!_keyMap.ContainsKey(name)) _keyMap[name] = new List<Keys>();
            if (key != Keys.None) _keyMap[name].Add(key);

            if (!_buttonMap.ContainsKey(name)) _buttonMap[name] = new List<Buttons>();
            if ((int)btn != -1) _buttonMap[name].Add(btn);

            // 登録済みボタンリストに追加
            if (!_registeredButtonNames.Contains(name))
            {
                _registeredButtonNames.Add(name);
                _pressedDuration[name] = 0.0;
            }
        }

        /// <summary>
        /// 入力状態の更新を行います。毎フレーム呼び出してください。
        /// </summary>
        /// <param name="gameTime">時間情報</param>
        public void Update(GameTime gameTime)
        {
            Update(); // 状態更新
            
            // 押下時間の更新
            double elapsed = gameTime.ElapsedGameTime.TotalSeconds;
            foreach (var name in _registeredButtonNames)
            {
                if (IsPressed(name))
                {
                    _pressedDuration[name] += elapsed;
                }
                else
                {
                    _pressedDuration[name] = 0.0;
                }
            }

            UpdateVibration((float)elapsed);
        }

        /// <summary>
        /// 内部的な状態更新処理です。
        /// </summary>
        public void Update()
        {
            _isInputConsumed = false; // フレーム冒頭でリセット

            _prevKeyboard = _currentKeyboard;
            _currentKeyboard = Keyboard.GetState();

            _prevGamePad = _currentGamePad;
            _currentGamePad = GamePad.GetState(PlayerIndex.One);

            _prevMouse = _currentMouse;
            _currentMouse = Mouse.GetState();

            
            // 接続状態変化ログ
            if (_prevGamePad.IsConnected && !_currentGamePad.IsConnected)
            {
                Ton.Log.Warning("GamePad Disconnected (PlayerIndex.One)");
            }
            else if (!_prevGamePad.IsConnected && _currentGamePad.IsConnected)
            {
                Ton.Log.Info("GamePad Connected (PlayerIndex.One)");
            }
        }

        /// <summary>
        /// 現在のフレームの入力を消費し、以降の入力判定を全てfalseにします。
        /// </summary>
        public void ConsumeInput()
        {
            _isInputConsumed = true;
        }

        /// <summary>
        /// 指定したボタンが押されているか判定します。
        /// </summary>
        public bool IsPressed(string buttonName)
        {
            if (_isInputConsumed) return false;

            if (_keyMap.ContainsKey(buttonName))
            {
                foreach (var k in _keyMap[buttonName])
                    if (_currentKeyboard.IsKeyDown(k)) return true;
            }
            if (_buttonMap.ContainsKey(buttonName))
            {
                foreach (var b in _buttonMap[buttonName])
                    if (_currentGamePad.IsButtonDown(b)) return true;
            }
            return false;
        }

        /// <summary>
        /// 指定したボタンが今押された瞬間か判定します。
        /// </summary>
        public bool IsJustPressed(string buttonName)
        {
             if (_isInputConsumed) return false;

             if (_keyMap.ContainsKey(buttonName))
            {
                foreach (var k in _keyMap[buttonName])
                    if (_currentKeyboard.IsKeyDown(k) && _prevKeyboard.IsKeyUp(k)) return true;
            }
            if (_buttonMap.ContainsKey(buttonName))
            {
                foreach (var b in _buttonMap[buttonName])
                    if (_currentGamePad.IsButtonDown(b) && _prevGamePad.IsButtonUp(b)) return true;
            }
            return false;
        }

        /// <summary>
        /// 指定したボタンが今離された瞬間か判定します。
        /// </summary>
        public bool IsJustReleased(string buttonName)
        {
             if (_isInputConsumed) return false;

             if (_keyMap.ContainsKey(buttonName))
            {
                foreach (var k in _keyMap[buttonName])
                    if (_currentKeyboard.IsKeyUp(k) && _prevKeyboard.IsKeyDown(k)) return true;
            }
            if (_buttonMap.ContainsKey(buttonName))
            {
                foreach (var b in _buttonMap[buttonName])
                    if (_currentGamePad.IsButtonUp(b) && _prevGamePad.IsButtonDown(b)) return true;
            }
            return false;
        }

        /// <summary>
        /// 指定したボタンが押され続けている時間（秒）を取得します。
        /// 押されていない場合は 0.0 を返します。
        /// </summary>
        public double GetPressedDuration(string buttonName)
        {
            if (_isInputConsumed) return 0.0;
            if (_pressedDuration.ContainsKey(buttonName))
            {
                return _pressedDuration[buttonName];
            }
            return 0.0;
        }

        /// <summary>
        /// 指定したボタンの押下時間を強制的に0にリセットします。
        /// 長押しイベントの完了後などに使用します。
        /// </summary>
        public void ClearPressedDuration(string buttonName)
        {
             if (_pressedDuration.ContainsKey(buttonName))
             {
                 _pressedDuration[buttonName] = 0.0;
             }
        }

        /// <summary>
        /// 方向入力ベクトルを取得します（キーボードの矢印キー、ゲームパッドのスティック）。
        /// </summary>
        public Vector2 GetVector()
        {
            if (_isInputConsumed) return Vector2.Zero;

            Vector2 vec = Vector2.Zero;
            if (IsPressed("Up")) vec.Y -= 1;
            if (IsPressed("Down")) vec.Y += 1;
            if (IsPressed("Left")) vec.X -= 1;
            if (IsPressed("Right")) vec.X += 1;

            // ゲームパッドのアナログ入力
            vec += _currentGamePad.ThumbSticks.Left;
            
            if (vec.Length() > 1) vec.Normalize();
            return vec;
        }

        /// <summary>
        /// 仮想解像度上でのマウス位置を取得します。
        /// </summary>
        public Vector2 GetMousePosition()
        {
            var mouseState = Mouse.GetState();
            return Ton.Game.ConvertWindowToVirtual(new Vector2(mouseState.X, mouseState.Y));
        }

        /// <summary>
        /// ゲームパッドを振動させます。
        /// </summary>
        /// <param name="seconds">振動時間(秒)</param>
        /// <param name="motorLeft">左モーター強度(0.0-1.0)</param>
        /// <param name="motorRight">右モーター強度(0.0-1.0)</param>
        public void Vibrate(float seconds, float motor)
        {
            Vibrate(seconds, motor, motor);
        }
        public void Vibrate(float seconds, float motorLeft, float motorRight)
        {
            GamePad.SetVibration(PlayerIndex.One, motorLeft, motorRight);
            _vibrationTimer = seconds;
        }
        
        /// <summary>
        /// 振動の更新処理を行います。タイマーを減算し、0になったら停止します。
        /// </summary>
        /// <param name="elapsed">経過時間(秒)</param>
        private void UpdateVibration(float elapsed)
        {
            if (_vibrationTimer > 0)
            {
                _vibrationTimer -= elapsed;
                if (_vibrationTimer <= 0)
                {
                    // 振動停止
                    GamePad.SetVibration(PlayerIndex.One, 0, 0);
                }
            }
        }

        /// <summary>
        /// マウスステートから指定されたボタンの状態を取得するヘルパーメソッドです。
        /// </summary>
        /// <param name="state">マウスステート</param>
        /// <param name="button">対象のボタン</param>
        /// <returns>ボタンの状態(Pressed/Released)</returns>
        private ButtonState GetMouseButtonState(MouseState state, MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left: return state.LeftButton;
                case MouseButton.Right: return state.RightButton;
                case MouseButton.Middle: return state.MiddleButton;
                case MouseButton.XButton1: return state.XButton1;
                case MouseButton.XButton2: return state.XButton2;
                default: return ButtonState.Released;
            }
        }

        /// <summary>
        /// 指定したマウスボタンが押されているか判定します。
        /// </summary>
        public bool IsMousePressed(MouseButton button)
        {
            if (_isInputConsumed) return false;
            return GetMouseButtonState(_currentMouse, button) == ButtonState.Pressed;
        }

        /// <summary>
        /// 指定したマウスボタンが今押された瞬間か判定します。
        /// </summary>
        public bool IsMouseJustPressed(MouseButton button)
        {
            if (_isInputConsumed) return false;
            return GetMouseButtonState(_currentMouse, button) == ButtonState.Pressed &&
                   GetMouseButtonState(_prevMouse, button) == ButtonState.Released;
        }

        /// <summary>
        /// 指定したマウスボタンが今離された瞬間か判定します。
        /// </summary>
        public bool IsMouseJustReleased(MouseButton button)
        {
            if (_isInputConsumed) return false;
            return GetMouseButtonState(_currentMouse, button) == ButtonState.Released &&
                   GetMouseButtonState(_prevMouse, button) == ButtonState.Pressed;
        }
    }
}
