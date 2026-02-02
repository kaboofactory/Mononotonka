using System;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// シーンインターフェース。
    /// すべてのシーンクラスはこのインターフェースを実装する必要があります。
    /// </summary>
    public interface IScene
    {
        void Initialize();
        void Update(GameTime gameTime);
        void Draw();
        void Terminate();
    }

    /// <summary>
    /// シーン管理クラス。
    /// シーンの遷移（フェードイン・アウト）と実行中のシーンの更新・描画を管理します。
    /// </summary>
    public class TonScene
    {
        private IScene _currentScene;
        private IScene _nextScene;
        
        // 遷移用パラメータ
        private bool _isTransitioning = false;
        private float _fadeTimer = 0f;
        private float _fadeDurationOut = 1.0f;
        private float _fadeDurationIn = 1.0f;
        private Color _fadeColor = Color.Black;
        private bool _fadingOut = false; // True=暗転中(Fade Out), False=明転中(Fade In)

        /// <summary>
        /// 初期化処理。
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// 終了処理。現在のシーンを終了させます。
        /// </summary>
        public void Terminate()
        {
            _currentScene?.Terminate();
        }

        /// <summary>
        /// シーンを変更します。
        /// </summary>
        /// <param name="nextScene">次のシーンインスタンス</param>
        /// <param name="durationOut">フェードアウト（暗転）にかかる時間（秒）</param>
        /// <param name="durationIn">フェードイン（明転）にかかる時間（秒）。マイナス値を指定するとdurationOutと同じになります。</param>
        /// <param name="fadeColor">フェード色</param>
        public void Change(IScene nextScene, float durationOut = 0.0f, float durationIn = -1.0f, Color fadeColor = default)
        {
            if (_isTransitioning) return; // 二重呼び出し防止

            if (fadeColor == default) fadeColor = Color.Black;
            _fadeColor = fadeColor;
            _fadeDurationOut = durationOut;
            _fadeDurationIn = (durationIn < 0) ? durationOut : durationIn; // durationInが省略(マイナス)されたらOutと同じにする
            _nextScene = nextScene;
            
            _isTransitioning = true;
            _fadingOut = true;
            _fadeTimer = 0f;
        }

        /// <summary>
        /// 現在のシーンを取得します。
        /// </summary>
        public IScene GetCurrentScene()
        {
            return _currentScene;
        }

        /// <summary>
        /// シーンの更新を行います。
        /// フェード処理中は遷移ロジックも実行されます。
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isTransitioning)
            {
                _fadeTimer += elapsed;
                
                if (_fadingOut)
                {
                    // フェードアウト中（画面隠蔽中）
                    if (_fadeTimer >= _fadeDurationOut)
                    {
                        // シーン切り替え
                        _currentScene?.Terminate();
                        _currentScene = _nextScene;
                        _nextScene = null;
                        _currentScene?.Initialize();
                        
                        // フェードイン開始
                        _fadingOut = false;
                        _fadeTimer = 0f;
                    }
                }
                else
                {
                    // フェードイン中（画面表示中）
                    if (_fadeTimer >= _fadeDurationIn)
                    {
                        _isTransitioning = false;
                        _fadeTimer = 0f;
                    }
                }
            }

            _currentScene?.Update(gameTime);
        }

        /// <summary>
        /// シーンの描画を行います。
        /// フェード中はオーバーレイ描画を行います。
        /// </summary>
        public void Draw()
        {
            _currentScene?.Draw();

            if (_isTransitioning)
            {
                // フェードオーバーレイ描画
                float alpha = 0f;
                if (_fadingOut)
                {
                    if (_fadeDurationOut > 0)
                        alpha = MathHelper.Clamp(_fadeTimer / _fadeDurationOut, 0f, 1f);
                    else
                        alpha = 1f;
                }
                else
                {
                    if (_fadeDurationIn > 0)
                        alpha = MathHelper.Clamp(1.0f - (_fadeTimer / _fadeDurationIn), 0f, 1f);
                    else
                        alpha = 0f;
                }
                
                if (alpha > 0)
                {
                    // 画面全体を矩形で覆う
                    Ton.Gra.FillRect(0, 0, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight, _fadeColor * alpha);
                }
            }
        }
    }
}
