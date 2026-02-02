using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mononotonka;

namespace Mononotonka
{
    /// <summary>
    /// TonMenuManager + Submenu + Callback 動作確認用シーン
    /// </summary>
    public class SampleScene06 : IScene
    {
        // メニュー画面管理クラス
        private TonMenuManager _menuManager;

        // Yボタン押下時間
        float fHoldYButton = 0.0f;

        // UIステータス
        private string _logText = "";
        private string _helpText = "";

        // MPステータス
        private int _mp = 14;

        public void Initialize()
        {
            // 初期化処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Initializing.");

            // SEロード
            Ton.Sound.LoadSound("sample_assets/sound/se/buzzer", "buzzer");
            Ton.Sound.LoadSound("sample_assets/sound/se/cursor", "cursor");

            // TonMenuManager 初期化（メニュー画面管理クラス）
            _menuManager = new TonMenuManager();

            // メインメニューを作成して開く
            // Pushすることでスタックの一番上に積まれ、操作・描画対象となる
            var mainMenu = CreateMainMenu();
            _menuManager.Push(mainMenu);

            // 初期化処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Initialized.");
        }

        public void Terminate()
        {
            // 終了処理開始
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminating.");

            // SEアンロード  
            Ton.Sound.UnloadAll();

            // シーン終了時にメニューを全て破棄
            _menuManager.Clear();

            // 終了処理終了
            Ton.Log.Info("Scene " + this.GetType().Name + " Terminated.");
        }

        public void Update(GameTime gameTime)
        {
            // Yボタン押下時間更新
            if (Ton.Input.IsPressed("Y"))
            {
                fHoldYButton += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (fHoldYButton >= 1.0f)
                {
                    // Yボタンを1秒以上押していたら次のシーンへ移動(フェードアウト・フェードイン時間を指定可能)
                    Ton.Scene.Change(new SampleScene07(), 0.5f, 0.5f, Color.ForestGreen);
                }
            }
            else
            {
                fHoldYButton = 0.0f;
            }

            // メニューの入力制御などはマネージャーに一任する
            // スタックトップのメニューのみが操作を受け付ける
            _menuManager.Update();

            // もし全メニューが閉じられたら（キャンセル連打などでスタックが空になった場合）
            if (!_menuManager.IsMenuOpen)
            {
                // 例えば前のシーンに戻るなど
                // Ton.Scene.Change(new FifthScene());
                // ここではデモ用にもう一度メインメニューを開き直す
                _logText = "全メニューが閉じられました。リセットします。";
                _menuManager.Push(CreateMainMenu());
            }
        }

        public void Draw()
        {
            Ton.Gra.Clear(Color.DarkSlateGray);

            // デバッグ情報表示
            Ton.Gra.DrawText("Six Scene: TonMenuManager Test", 20, 10, Color.White, 0.7f);
            Ton.Gra.DrawText($"MP: {_mp}", 600, 10, Color.Cyan);

            // メニュー描画 (重ね合わせ対応)
            // 引数のコールバック内で、各メニューごとのウィンドウ背景描画処理を定義できる。
            // これにより SixScene 側で自由にウィンドウのデザイン（9-patch等）を実装可能。
            _menuManager.Draw((menu) => 
            {
                // ウィンドウを9-patch画像で描画
                // メニューの矩形より少し大きめに描画して枠を表現
                if (menu.IsActive)
                {
                    // アクティブメニューの描画
                    Ton.Gra.FillRoundedRect("9-patch", menu.WindowRect.X - 16, menu.WindowRect.Y - 16, menu.WindowRect.Width + 32, menu.WindowRect.Height + 32, 16, 16);
                }
                else
                {
                    // 非アクティブメニューは少し暗く表示
                    Ton.Gra.FillRoundedRect("9-patch-dark", menu.WindowRect.X - 16, menu.WindowRect.Y - 16, menu.WindowRect.Width + 32, menu.WindowRect.Height + 32, 16, 16);
                }
            });

            // 次のシーンへ
            Ton.Gra.DrawText("Hold the Y button (Next Scene)", 700 - (int)(fHoldYButton * 400.0f), 70, 0.6f + (fHoldYButton));

            // ログ表示
            Ton.Gra.DrawText(_logText, 20, 360, Color.Yellow);

            // ヘルプ表示 (下部)
            // こちらも9-patchでウィンドウ描画
            Ton.Gra.FillRoundedRect("9-patch", 0, 430, Ton.Game.VirtualWidth, Ton.Game.VirtualHeight - 430, 16, 16);
            Ton.Gra.DrawText(_helpText, 20, 450, Color.White, 0.8f);
        }

        // --- メニュー生成ファクトリー ---

        /// <summary>
        /// メインメニューを生成して返します
        /// </summary>
        private TonMenu CreateMainMenu()
        {
            // メインメニュー作成 (位置とサイズを指定)
            var menu = new TonMenu(new Rectangle(50, 50, 240, 200), 1, 4, 200, 50, false);

            // メニュー外観設定
            menu.SetContentScale(0.8f);

            // カーソル画像設定
            menu.SetTextOffset(35); // カーソルサイズ分、テキスト位置を少し右にずらす
            menu.SetCursorIcon("finger", new Vector2(35, 0)); // 指カーソル設定 (少し右にずらす)

            // ライフサイクルイベント: メニューが表示されたり、他のメニューから戻ってきた時に呼ばれる
            menu.OnEnter = () =>
            {
                _helpText = "メインコマンドを選択してください";
            };
            menu.OnResume = () =>
            {
                _helpText = "メインコマンドを選択してくださいぴょ～～～～ん"; // 戻ってきた時も更新
            };

            // カーソル移動時のSE
            menu.OnSelectionChanged = (item) => Ton.Sound.PlaySE("cursor");

            // ITEMS 追加

            // 1. たたかう (単純なテキスト表示アクション)
            menu.AddItem(CreateTextItem("たたかう", (itm) =>
            {
                _logText = "敵に攻撃した！";
                Ton.Sound.PlaySE("buzzer");
            }));

            // 2. まほう (サブメニューを開くアクション)
            menu.AddItem(CreateTextItem("まほう", (itm) => 
            {
                // 効果音
                Ton.Sound.PlaySE("cursor");

                // サブメニューを作成
                var magicMenu = CreateMagicMenu();
                
                // サブメニューをPushして開く。
                // Push時にコールバックを指定することで、そのメニューが閉じた後の処理(結果受け取り)を記述できる
                _menuManager.Push(magicMenu, OnMagicFinished); 
            }));

            // 3. アイテム
            menu.AddItem(CreateTextItem("アイテム", (itm) => 
            {
                // サブメニューを作成
                var itemMenu = CreateItemMenu();
                _menuManager.Push(itemMenu, (res) => {
                    if (res != null)
                    {
                        // アイテム画面から結果を受け取った
                        _logText = $"{res} を使った！";

                        // 効果音
                        Ton.Sound.PlaySE("cursor");
                    }
                    else
                    {
                        _logText = "アイテム選択をキャンセル";
                    }
                });
            }));

            // 4. にげる
            menu.AddItem(CreateTextItem("にげる", (itm) => {
                _logText = "逃げられなかった！";
                Ton.Sound.PlaySE("buzzer");
            }));

            return menu;
        }

        /// <summary>
        /// 魔法メニューが閉じた後に呼ばれるコールバックメソッド
        /// </summary>
        private void OnMagicFinished(object result)
        {
            if (result == null) 
            {
                _logText = "魔法選択をキャンセルしました";
            }
            else 
            {
                // 選択された魔法名が返ってくる想定
                string magicName = (string)result;
                _logText = $"{magicName} を唱えた！ 残りMP: {_mp}";
            }
        }

        /// <summary>
        /// 魔法選択用サブメニューを生成して返します
        /// </summary>
        private TonMenu CreateMagicMenu()
        {
            // 少しずらして表示（重なり確認用）
            var menu = new TonMenu(new Rectangle(100, 80, 400, 150), 1, 3, 400, 50, false);
            menu.SetContentScale(0.8f);
            menu.SetLoopable(true); // カーソルループ有効

            menu.OnEnter = () => _helpText = "使用する魔法を選んでください";

            // カーソル移動時のSE
            menu.OnSelectionChanged = (item) => Ton.Sound.PlaySE("cursor");

            // 魔法リスト追加
            AddMagicItem(menu, "ファイア", 10);
            AddMagicItem(menu, "ブリザド", 12);
            AddMagicItem(menu, "サンダー", 15);

            return menu;
        }

        /// <summary>
        /// アイテムメニューの生成
        /// 横5列、縦10行相当のデータを用意し、スクロールテストを行う
        /// </summary>
        private TonMenu CreateItemMenu()
        {
            // 表示領域: 920x200 (広げる)
            // 列数: 3, 行数(表示): 4
            // アイテムサイズ: 幅300, 高さ50
            var menu = new TonMenu(new Rectangle(240, 120, 920, 200), 3, 4, 300, 50, true);
            menu.SetContentScale(0.5f);
            menu.SetTextOffset(0); // パネルレイアウト内のテキストがずれないように0にする
            menu.SetLoopable(false);
            menu.SetCursorIcon("finger", new Vector2(5, 0));

            // 背景描画などはManagerのDrawコールバックで行われる

            // スクロールインジケータのカスタム描画
            menu.OnPostDraw = (m) =>
            {
                int cx = m.WindowRect.Center.X;
                int vy = (int)(Ton.Game.TotalGameTime.TotalSeconds * 40.0) % 15;

                if (m.CanScrollUp)
                {
                    // 上矢印 (ウィンドウ上辺中央)
                    Ton.Gra.Draw("scroll_up", cx - (Ton.Gra.GetTextureWidth("scroll_up") / 2), m.WindowRect.Y - Ton.Gra.GetTextureHeight("scroll_up") - 15 - vy);
                }
                
                if (m.CanScrollDown)
                {
                    // 下矢印 (ウィンドウ下辺中央)
                    Ton.Gra.Draw("scroll_down", cx - (Ton.Gra.GetTextureWidth("scroll_down") / 2), m.WindowRect.Y + m.WindowRect.Height + 15 + vy);
                }
            };

            menu.OnEnter = () =>
            {
                _helpText = "アイテムを選んでください（空白も選択可）";
            };
            menu.OnSelectionChanged = (item) => Ton.Sound.PlaySE("cursor");

            // データ生成 (50個 + α)
            string[] dummyNames = { "ポーション", "ハイポーション", "エーテル", "エリクサー", "解毒剤", "気付け薬", "フェニ尾", "テント", "コテージ" };
            Random rand = new Random();

            for (int i = 0; i < 50; i++)
            {
                // 適当に空白を作る（例えば 10% の確率、あるいは特定の場所）
                if (i == 7 || i == 12 || i == 25 || i == 44) 
                {
                    // 空白アイテム（中身のないアイテムを追加して、そこを選択可能にする）
                    // 完全に空のアイテムを追加
                    var blankItem = new TonMenuItem();
                    // 何も描画しないが、カーソルは合う
                    menu.AddItem(blankItem);
                    continue;
                }

                string name = dummyNames[rand.Next(dummyNames.Length)];
                int count = rand.Next(1, 99);
                AddItemItem(menu, name, count);
            }

            return menu;
        }

        // アイテム項目追加ヘルパー
        private void AddItemItem(TonMenu menu, string name, int count)
        {
            var item = new TonMenuItem();
            item.Tag = name; // 結果返却用に名前を保持

            // 横レイアウト: アイコン | 名前 | 個数
            var layout = new TonMenuPanel(TonMenuPanel.LayoutType.Horizontal);
            
            // 1. アイコン 
            layout.AddChild(new TonMenuIcon("item"){ Scale = 0.8f }, 0.2f); // 20%
            
            // 2. 名前 (広めにとる)
            layout.AddChild(new TonMenuText(name){ Alignment = ElementAlignment.Left }, 0.6f); // 60%
            
            // 3. 個数
            layout.AddChild(new TonMenuText($"x{count}"){ Alignment = ElementAlignment.Right, Color = Color.Yellow }, 0.2f); // 20%

            item.SetLayout(layout);

            item.OnDecided = (itm) => {
                Ton.Sound.PlaySE("ok");
                _menuManager.Pop(itm.Tag);
            };

            menu.AddItem(item);
        }

        // 魔法項目追加ヘルパー
        private void AddMagicItem(TonMenu menu, string name, int cost)
        {
            // メニューアイテムを作成
            var item = new TonMenuItem();

            // 水平レイアウトパネルを使用して「魔法名 + ハイフン + 消費MP」を並べる
            var layout = new TonMenuPanel(TonMenuPanel.LayoutType.Horizontal);

            // 魔法名 + ハイフン + 消費MPを左右に配置
            layout.AddChild(new TonMenuText(name), 0.4f); // 左側40%
            layout.AddChild(new TonMenuText("-"){ Color = Color.Gray, Alignment = ElementAlignment.Center }, 0.2f); // 中央20%
            layout.AddChild(new TonMenuText($"{cost} MP"){ Color = Color.Cyan, Alignment = ElementAlignment.Right }, 0.4f); // 右側40%

            // メニューアイテムにレイアウトを設定
            item.SetLayout(layout);

            // 決定時の動作
            item.OnDecided = (i) => {
                if (_mp >= cost)
                {
                    // MP足りてるなら魔法使用
                    _mp -= cost;

                    // 効果音
                    Ton.Sound.PlaySE("cursor");
                    
                    // Pop(result) を呼ぶことで、このメニューを閉じつつ呼び出し元(OnMagicFinished)に結果を渡す
                    _menuManager.Pop(name);
                }
                else
                {
                    // MP足りない場合はメッセージ表示のみ
                    _logText = "MPが足りない！";
                    
                    // 効果音
                    Ton.Sound.PlaySE("buzzer");
                }
            };

            // メニューにメニューアイテムを追加
            menu.AddItem(item);
        }

        // テキストのみのアイテム作成ヘルパー
        private TonMenuItem CreateTextItem(string text, Action<TonMenuItem> action)
        {
            // メニューアイテムを作成
            var item = new TonMenuItem();

            // 空のレイアウトパネルを作成してテキストを追加
            var layout = new TonMenuPanel(TonMenuPanel.LayoutType.Free);

            // レイアウトパネルにテキスト要素を追加
            layout.AddChild(new TonMenuText(text));

            // メニューアイテムにレイアウトを設定
            item.SetLayout(layout);
            
            // 決定時の動作を設定
            item.OnDecided = action;

            return item;
        }
    }
}
