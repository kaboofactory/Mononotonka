using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Mononotonka
{
    /// <summary>
    /// GameMainクラスは、ゲームプログラムの本体です。
    /// ここでゲームの初期化、更新、描画のループ処理を管理します。
    /// </summary>
    public class GameMain : Game
    {
        private GraphicsDeviceManager _graphics;

        /// <summary>
        /// コンストラクタです。
        /// グラフィックス機能の準備や、コンテンツ（素材）の保存場所を設定します。
        /// </summary>
        public GameMain()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true; // マウスカーソルを表示するように設定します
        }

        /// <summary>
        /// Initializeは、ゲーム起動時に一度だけ呼ばれる初期化処理です。
        /// </summary>
        protected override void Initialize()
        {
            // Mononotonkaライブラリのシステムを初期化します
            Ton.Instance.Initialize(this, _graphics);

            // ウィンドウタイトルを設定します
            Ton.Game.SetWindowTitle("Mononotonka Sample Program");

            // 画面サイズなどの初期設定を行います
            Ton.Game.SetVirtualResolution(1280, 720); // ゲーム内の仮想解像度

            // ウィンドウサイズを設定します(設定ファイルにサイズが保存されている場合はTonConfigMenuが優先して読み込みます)
            Ton.Game.SetWindowSize(1280, 720);

            // アンチエイリアスオフ
            Ton.Gra.SetAntiAliasing(false);

            // ウィンドウサイズ変更を許可
            Ton.Game.SetResizable(true);

            // 最初のシーンへ移動します
            //Ton.Scene.Change(new SampleScene01());
            Ton.Scene.Change(new SampleScene08());

            base.Initialize();
        }

        /// <summary>
        /// LoadContentは、画像や音などのデータを読み込むためのメソッドです。
        /// </summary>
        protected override void LoadContent()
        {
            // グラフィックスデバイスの最大テクスチャサイズを取得して、ログに出力します。(実生成してチェックするので)
            Ton.Gra.GetMaxTextureSize();

            // 各シーンに入る前に事前に読み込んでおきたいデータがあればここに記述します。
            // 例: Ton.Gra.LoadTexture("image/hero", "hero");

            // フォントのロード(MGDB Editorに登録してビルドしたフォントリソースをここで読み込みます)
            Ton.Gra.LoadFont("font/default", "default");
            Ton.Gra.LoadFont("font/hanazome", "hanazome");
            Ton.Gra.LoadFont("font/rounded", "rounded");
            Ton.Gra.LoadFont("font/tanuki", "tanuki");
            Ton.Gra.LoadFont("font/tegaki", "tegaki");

            // デバッグ: フォントテクスチャを書き出す(フォントテクスチャを確認したい場合に使用)
            /*
            Ton.Gra.DebugSaveFontTexture("mononotonka_font_default.png", "default");
            Ton.Gra.DebugSaveFontTexture("mononotonka_font_hanazome.png", "hanazome");
            Ton.Gra.DebugSaveFontTexture("mononotonka_font_rounded.png", "rounded");
            Ton.Gra.DebugSaveFontTexture("mononotonka_font_tanuki.png", "tanuki");
            Ton.Gra.DebugSaveFontTexture("mononotonka_font_tegaki.png", "tegaki");
            */

            // 画像のロード(永続的にキャッシュに乗せるリソースはここでisPermanent=trueにしてください)
            // Content Pipelineでビルドされたリソースをロードするため、拡張子は不要です
            Ton.Gra.LoadTexture("sample_assets/image/landscape", "landscape");
            Ton.Gra.LoadTexture("sample_assets/image/fighter", "fighter");
            Ton.Gra.LoadTexture("sample_assets/image/cat_animation", "cat_animation");
            Ton.Gra.LoadTexture("sample_assets/image/coin_animation", "coin_animation");
            Ton.Gra.LoadTexture("sample_assets/image/9-patch", "9-patch");
            Ton.Gra.LoadTexture("sample_assets/image/9-patch-dark", "9-patch-dark");
            Ton.Gra.LoadTexture("sample_assets/image/controller", "controller");
            Ton.Gra.LoadTexture("sample_assets/image/chara_animation", "chara_animation");
            Ton.Gra.LoadTexture("sample_assets/image/heart", "heart");
            Ton.Gra.LoadTexture("sample_assets/image/icon", "icon");
            Ton.Gra.LoadTexture("sample_assets/image/item", "item");
            Ton.Gra.LoadTexture("sample_assets/image/finger", "finger");
            Ton.Gra.LoadTexture("sample_assets/image/scroll_up", "scroll_up");
            Ton.Gra.LoadTexture("sample_assets/image/scroll_down", "scroll_down");
            Ton.Gra.LoadTexture("sample_assets/image/neko_bg", "neko_bg");
        }

        /// <summary>
        /// Updateは、毎フレーム（1秒間に60回など）呼び出され、ゲームの計算や状態更新を行うメソッドです。
        /// </summary>
        /// <param name="gameTime">前回の更新からの経過時間などの情報</param>
        protected override void Update(GameTime gameTime)
        {
            // この関数は基本的に変更不要です。シーンのUpdateメソッドにて更新を行ってください。

            // ESCキーが押されたら、ゲームを終了します
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            // Mononotonkaシステムの更新処理を実行します
            Ton.Instance.Update(gameTime);

            // MonoGameの基本的な更新処理を実行します
            base.Update(gameTime);
        }

        /// <summary>
        /// Drawは、毎フレーム呼び出され、画面への描画を行うメソッドです。
        /// </summary>
        /// <param name="gameTime">前回の描画からの経過時間などの情報</param>
        protected override void Draw(GameTime gameTime)
        {
            // この関数は基本的に変更不要です。シーンのDrawメソッドにて描画を行ってください。

            // Mononotonkaの描画処理を実行します。
            // 画面のクリア（背景を黒にする処理）や、描画の開始・終了処理もこの中で自動的に行われます。
            Ton.Instance.Draw(gameTime);

            // MonoGameの基本的な描画処理を実行します
            base.Draw(gameTime);
        }
    }
}
