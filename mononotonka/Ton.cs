using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mononotonka
{
    /// <summary>
    /// Mononotonkaラッパーのコアクラスです（シングルトン）。
    /// ゲーム内のあらゆる機能へのアクセスポイントとなります。
    /// </summary>
    public class Ton
    {
        private static Ton _instance;
        /// <summary>
        /// Tonクラスの唯一のインスタンスを取得します。
        /// </summary>
        public static Ton Instance => _instance ??= new Ton();

        // Static Shortcuts
        public static TonLog Log => Instance.log;
        public static TonGame Game => Instance.game;
        public static TonInput Input => Instance.input;
        public static TonGraphics Gra => Instance.gra;
        public static TonSound Sound => Instance.sound;
        public static TonMessage Msg => Instance.msg;
        public static TonCharacter Character => Instance.character;
        public static TonConfigMenu ConfigMenu => Instance.configmenu;
        public static TonScene Scene => Instance.scene;
        public static TonMath Math => Instance.math;
        public static TonStorage Storage => Instance.storage;
        public static TonParticle Particle => Instance.particle;
        public static TonSaveLoadMenu SaveLoadMenu => Instance.saveload;
        public static TonPrimitive Primitive => Instance.primitive;
        /// <summary>魔法エフェクトシステム</summary>
        public static TonMagicEffect Magic => Instance.magic;
        /// <summary>ゲームデータ（ステータス・フラグ等）</summary>
        public static TonGameData Data => Instance.gamedata;

        // サブシステム群
        /// <summary>ログ出力管理</summary>
        public TonLog log { get; private set; }
        /// <summary>ゲームシステム管理（ウィンドウサイズ、FPSなど）</summary>
        public TonGame game { get; private set; }
        /// <summary>入力管理（キーボード、ゲームパッド）</summary>
        public TonInput input { get; private set; }
        /// <summary>グラフィックス管理（描画、テクスチャ）</summary>
        public TonGraphics gra { get; private set; }
        /// <summary>サウンド管理（BGM、SE）</summary>
        public TonSound sound { get; private set; }
        /// <summary>メッセージウィンドウ管理（テキスト表示）</summary>
        public TonMessage msg { get; private set; }
        /// <summary>キャラクター管理</summary>
        public TonCharacter character { get; private set; }
        /// <summary>設定メニュー管理</summary>
        public TonConfigMenu configmenu { get; private set; }
        /// <summary>セーブロードメニュー管理</summary>
        public TonSaveLoadMenu saveload { get; private set; }
        /// <summary>ゲームデータ管理</summary>
        public TonGameData gamedata { get; internal set; }
        /// <summary>シーン管理</summary>
        public TonScene scene { get; private set; }
        /// <summary>
        /// 数学・乱数ユーティリティ
        /// </summary>
        public TonMath math { get; private set; }
        /// <summary>
        /// ストレージ管理（セーブ・ロード）
        /// </summary>
        public TonStorage storage { get; private set; }
        /// <summary>パーティクルシステム</summary>
        public TonParticle particle { get; private set; }
        /// <summary>プリミティブ描画（幾何学図形）</summary>
        public TonPrimitive primitive { get; private set; }
        /// <summary>魔法エフェクト</summary>
        public TonMagicEffect magic { get; private set; }

        private Ton()
        {
            // 各サブクラスを生成します
            // 注意: InitializeメソッドでGameやGraphicsDeviceへの参照が渡されるまで、一部の機能は完全に初期化されません
            log = new TonLog();
            game = new TonGame();
            input = new TonInput();
            gra = new TonGraphics();
            sound = new TonSound();
            msg = new TonMessage();
            character = new TonCharacter();
            configmenu = new TonConfigMenu();
            saveload = new TonSaveLoadMenu();
            gamedata = new TonGameData();
            scene = new TonScene();
            math = new TonMath();
            storage = new TonStorage();
            particle = new TonParticle();
            primitive = new TonPrimitive();
            magic = new TonMagicEffect();
        }

        /// <summary>
        /// Mononotonkaシステムの初期化を行います。
        /// </summary>
        /// <param name="game">MonoGameのGameインスタンス</param>
        /// <param name="graphics">GraphicsDeviceManagerのインスタンス</param>
        public void Initialize(Game game, GraphicsDeviceManager graphics)
        {
            this.game.Initialize(game, graphics);
            this.gra.Initialize(game, graphics);
            this.input.Initialize();
            this.sound.Initialize(game.Services);
            this.msg.Initialize();
            this.scene.Initialize();
            this.primitive.Initialize(game, graphics);
            this.configmenu.Initialize(); // 設定読み込み
            
            // 起動ログを出力
            log.Info("Mononotonka Initialized.");
            
            // ウィンドウを中央に配置
            this.game.CenterWindow();
        }

        /// <summary>
        /// システム全体の更新処理を行います。毎フレーム呼び出してください。
        /// </summary>
        /// <param name="gameTime">前回の更新からの経過時間などの情報</param>
        public void Update(GameTime gameTime)
        {
            // メニューが開いているかどうか
            bool isMenuOpen = configmenu.IsOpen() || saveload.IsOpen();

            game.Update(gameTime);
            input.Update(gameTime);
            sound.Update(gameTime); // 音は止めない

            if (!isMenuOpen)
            {
                // メニューが開いていない時のみ進行
                gra.Update(gameTime);
                scene.Update(gameTime);
                character.Update(gameTime);
                particle.Update(gameTime);
                magic.Update(gameTime);
                msg.Update(gameTime);
            }

            configmenu.Update();
            saveload.Update();
        }

        /// <summary>
        /// システム全体の描画処理を行います。毎フレーム呼び出してください。
        /// </summary>
        /// <param name="gameTime">前回の描画からの経過時間などの情報</param>
        public void Draw(GameTime gameTime)
        {
            game.Draw(gameTime);
            gra.Begin();
            gra.Clear(Color.Black); // 毎フレームのリセット
            
            // シーンの描画（タイルマップなどはここで描画されることを想定）
            scene.Draw();
            
            // キャラクターの描画
            character.Draw();

            // パーティクルの描画（キャラクターより手前に表示）
            particle.Draw();

            // メッセージウィンドウの描画
            msg.Draw();

            // 設定メニューの描画
            configmenu.Draw();
            
            // セーブロードメニューの描画（設定メニューより手前か奥かは仕様次第だが、一般的に同じ階層か手前）
            saveload.Draw();

            
            gra.End();
        }

        /// <summary>
        /// システムの終了処理を行います。
        /// </summary>
        public void Terminate()
        {
            log.Info("Mononotonka Terminating...");
            scene.Terminate();
            gra.Terminate(); // テクスチャの解放
            sound.Terminate(); // 音楽の停止・解放
            // その他、必要なクリーンアップ処理

            log.Close(); // 最後にログファイルを閉じる
        }
    }
}
