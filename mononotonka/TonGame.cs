using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mononotonka
{
    /// <summary>
    /// 基本的なゲームプロセス管理を行うクラスです。
    /// ウィンドウサイズ、解像度、FPS計測などを担当します。
    /// </summary>
    public class TonGame
    {
        private Game _game;
        private GraphicsDeviceManager _graphics;
        
        /// <summary>仮想解像度の幅</summary>
        public int VirtualWidth { get; private set; } = 1280;
        /// <summary>仮想解像度の高さ</summary>
        public int VirtualHeight { get; private set; } = 720;

        /// <summary>現在のウィンドウ幅</summary>
        public int WindowWidth => _graphics.PreferredBackBufferWidth;
        /// <summary>現在のウィンドウ高さ</summary>
        public int WindowHeight => _graphics.PreferredBackBufferHeight;

        /// <summary>ゲーム開始からの総経過時間</summary>
        public TimeSpan TotalGameTime { get; private set; }
        
        /// <summary>コンテンツマネージャへのアクセス</summary>
        public Microsoft.Xna.Framework.Content.ContentManager Content => _game.Content;

        // FPS計測用
        private int _updateFrameCount = 0;
        private float _updateTimer = 0;
        private float _currentUpdateFps = 0;

        private int _drawFrameCount = 0;
        private float _drawTimer = 0;
        private float _currentDrawFps = 0;

        // レターボックス計算キャッシュ用
        private Rectangle _cachedScreenDestinationRect;
        private bool _isScreenDestinationRectDirty = true;

        /// <summary>現在のフルスクリーン状態を取得します。</summary>
        public bool IsFullScreen => _graphics.IsFullScreen;

        /// <summary>
        /// 現在のUpdate FPS（論理フレームレート）を取得します。
        /// </summary>
        public float UpdateFPS => _currentUpdateFps;

        /// <summary>
        /// 現在のDraw FPS（描画フレームレート）を取得します。
        /// </summary>
        public float DrawFPS => _currentDrawFps;

        /// <summary>
        /// 初期化処理です。
        /// </summary>
        /// <param name="game">Gameインスタンス</param>
        /// <param name="graphics">GraphicsDeviceManagerインスタンス</param>
        public void Initialize(Game game, GraphicsDeviceManager graphics)
        {
            _game = game;
            _graphics = graphics;

            // 起動時解像度ログ
            Ton.Log.Info($"Screen Resolution: {_graphics.PreferredBackBufferWidth}x{_graphics.PreferredBackBufferHeight}, FullScreen: {_graphics.IsFullScreen}");
            
            // サポート解像度の取得とログ
            var resolutions = GetAvailableResolutions();
            string resLog = "";
            foreach(var res in resolutions) resLog += $"{res.X}x{res.Y} ";
            Ton.Log.Info($"Supported Resolutions: {resLog}");

            // ウィンドウサイズ変更イベントの登録
            _game.Window.ClientSizeChanged += OnClientSizeChanged;

            // マウスカーソル初期設定（非表示）
            _game.IsMouseVisible = false;
        }

        /// <summary>
        /// ウィンドウサイズが変更された際のイベントハンドラ
        /// </summary>
        private void OnClientSizeChanged(object sender, EventArgs e)
        {
            if (_graphics.IsFullScreen) return; // フルスクリーン時は無視

            // バックバッファサイズをウィンドウのクライアント領域に合わせる
            if (_graphics.PreferredBackBufferWidth != _game.Window.ClientBounds.Width ||
                _graphics.PreferredBackBufferHeight != _game.Window.ClientBounds.Height)
            {
                _graphics.PreferredBackBufferWidth = _game.Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = _game.Window.ClientBounds.Height;
                _graphics.ApplyChanges();
                
                Ton.Log.Info($"Window Resized: {_graphics.PreferredBackBufferWidth}x{_graphics.PreferredBackBufferHeight}");
                
                // キャッシュを無効化
                _isScreenDestinationRectDirty = true;
            }
        }

        /// <summary>
        /// 毎フレーム呼び出される更新処理です。
        /// </summary>
        /// <param name="gameTime">時間情報</param>
        public void Update(GameTime gameTime)
        {
            // FPS計算
            TotalGameTime = gameTime.TotalGameTime;
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _updateFrameCount++;
            _updateTimer += elapsed;
            if (_updateTimer >= 1.0f)
            {
                _currentUpdateFps = _updateFrameCount / _updateTimer;
                _updateFrameCount = 0;
                _updateTimer = 0;
            }
        }

        /// <summary>
        /// 毎フレーム呼び出される描画処理です（FPS計測用）。
        /// </summary>
        /// <param name="gameTime">時間情報</param>
        public void Draw(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _drawFrameCount++;
            _drawTimer += elapsed;
            if (_drawTimer >= 1.0f)
            {
                _currentDrawFps = _drawFrameCount / _drawTimer;
                _drawFrameCount = 0;
                _drawTimer = 0;
            }
        }

        /// <summary>
        /// 仮想解像度の幅を取得します。
        /// </summary>
        /// <returns>仮想解像度の幅</returns>
        public int GetVirtualWidth()
        {
            return VirtualWidth;
        }

        /// <summary>
        /// 仮想解像度の高さを取得します。
        /// </summary>
        /// <returns>仮想解像度の高さ</returns>
        public int GetVirtualHeight()
        {
            return VirtualHeight;
        }

        /// <summary>
        /// 現在のウィンドウ幅を取得します。
        /// </summary>
        /// <returns>ウィンドウ幅</returns>
        public int GetWindowWidth()
        {
            return WindowWidth;
        }

        /// <summary>
        /// 現在のウィンドウ高さを取得します。
        /// </summary>
        /// <returns>ウィンドウ高さ</returns>
        public int GetWindowHeight()
        {
            return WindowHeight;
        }

        /// <summary>
        /// ゲーム開始からの総経過時間を取得します。
        /// </summary>
        /// <returns>総経過時間</returns>
        public TimeSpan GetTotalGameTime()
        {
            return TotalGameTime;
        }

        /// <summary>
        /// 現在のフルスクリーン状態を取得します。
        /// </summary>
        /// <returns>true: フルスクリーン, false: ウィンドウ</returns>
        public bool GetIsFullScreen()
        {
            return IsFullScreen;
        }

        /// <summary>
        /// 現在のUpdate FPS（論理フレームレート）を取得します。
        /// </summary>
        /// <returns>Update FPS</returns>
        public float GetUpdateFPS()
        {
            return UpdateFPS;
        }

        /// <summary>
        /// 現在のDraw FPS（描画フレームレート）を取得します。
        /// </summary>
        /// <returns>Draw FPS</returns>
        public float GetDrawFPS()
        {
            return DrawFPS;
        }

        /// <summary>
        /// 利用可能な解像度リストを取得します。
        /// </summary>
        public System.Collections.Generic.List<Point> GetAvailableResolutions()
        {
             var list = new System.Collections.Generic.List<Point>();
             foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
             {
                 // 重複除外 (リフレッシュレート違いなどを無視)
                 if (list.FindIndex(p => p.X == mode.Width && p.Y == mode.Height) == -1)
                 {
                     // 極端に小さい解像度は除外してもよいが、一旦すべて含める
                     if (mode.Width >= 800 && mode.Height >= 600) // 最低限のフィルタ
                        list.Add(new Point(mode.Width, mode.Height));
                 }
             }
             // ソート
             list.Sort((a, b) => {
                 if (a.X != b.X) return a.X.CompareTo(b.X);
                 return a.Y.CompareTo(b.Y);
             });
             return list;
        }

        /// <summary>
        /// アプリケーションのウィンドウサイズを設定します。
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        public void SetWindowSize(int width, int height)
        {
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();
            _isScreenDestinationRectDirty = true;
        }

        /// <summary>
        /// ウィンドウを画面中央に移動します。
        /// フルスクリーン時は何もしません。
        /// </summary>
        public void CenterWindow()
        {
            if (IsFullScreen) return;

            var screen = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            int x = (screen.Width - WindowWidth) / 2;
            int y = (screen.Height - WindowHeight) / 2;
            
            _game.Window.Position = new Point(x, y);
        }

        /// <summary>
        /// ウィンドウのリサイズ（および最大化ボタン）を許可するか設定します。
        /// </summary>
        /// <param name="enable">trueで許可、falseで固定</param>
        public void SetResizable(bool enable)
        {
            _game.Window.AllowUserResizing = enable;
        }

        /// <summary>
        /// ゲーム内の仮想解像度を設定します。
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        public void SetVirtualResolution(int width, int height)
        {
            VirtualWidth = width;
            VirtualHeight = height;
            _isScreenDestinationRectDirty = true;
        }

        /// <summary>
        /// フルスクリーンモードの切り替えを行います。
        /// </summary>
        /// <param name="isFullscreen">trueでフルスクリーン、falseでウィンドウ</param>
        public void ToggleFullScreen(bool isFullscreen)
        {
            if (_graphics.IsFullScreen != isFullscreen)
            {
                _graphics.IsFullScreen = isFullscreen;
                _graphics.ApplyChanges();
                _isScreenDestinationRectDirty = true;
            }
        }
        
        /// <summary>
        /// ウィンドウのタイトルバーのテキストを設定します。
        /// </summary>
        /// <param name="title">タイトル文字列</param>
        public void SetWindowTitle(string title)
        {
            _game.Window.Title = title;
        }

        /// <summary>
        /// マウスカーソルの表示・非表示を設定します。
        /// </summary>
        /// <param name="visible">trueで表示、falseで非表示</param>
        public void SetMouseVisible(bool visible)
        {
            _game.IsMouseVisible = visible;
        }

        /// <summary>
        /// ゲームを終了します。
        /// </summary>
        public void Exit()
        {
            _game.Exit();
        }
        
        /// <summary>
        /// 仮想解像度を実際のウィンドウに描画する際の描画領域（レターボックス計算済み）を取得します。
        /// </summary>
        /// <returns>描画先矩形</returns>
        public Rectangle GetScreenDestinationRect()
        {
            if (_isScreenDestinationRectDirty)
            {
                float screenW = _graphics.PreferredBackBufferWidth;
                float screenH = _graphics.PreferredBackBufferHeight;
                float virtualW = VirtualWidth;
                float virtualH = VirtualHeight;

                // アスペクト比を維持してスケーリング計算
                float scaleX = screenW / virtualW;
                float scaleY = screenH / virtualH;
                float scale = Math.Min(scaleX, scaleY);

                int finalW = (int)(virtualW * scale);
                int finalH = (int)(virtualH * scale);
                int x = (int)((screenW - finalW) / 2);
                int y = (int)((screenH - finalH) / 2);

                _cachedScreenDestinationRect = new Rectangle(x, y, finalW, finalH);
                _isScreenDestinationRectDirty = false;
            }

            return _cachedScreenDestinationRect;
        }

        /// <summary>
        /// ウィンドウ上の座標を仮想解像度上の座標に変換します（マウス入力用など）。
        /// </summary>
        /// <param name="windowPos">ウィンドウ上の座標</param>
        /// <returns>仮想解像度上の座標</returns>
        public Vector2 ConvertWindowToVirtual(Vector2 windowPos)
        {
            Rectangle dest = GetScreenDestinationRect();
            // 描画領域の左上からのオフセット
            float x = windowPos.X - dest.X;
            float y = windowPos.Y - dest.Y;
            // スケールで割って仮想座標へ
            float scale = (float)dest.Width / VirtualWidth;
            return new Vector2(x / scale, y / scale);
        }
    }
}
