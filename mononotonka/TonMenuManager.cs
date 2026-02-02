using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// メニューのスタック（重なり）と遷移を管理するクラス。
    /// サブメニューやポップアップウィンドウの実装を補助します。
    /// </summary>
    public class TonMenuManager
    {
        private class MenuContext
        {
            public TonMenu Menu;
            public Action<object> OnResult;
        }

        private Stack<MenuContext> _menuStack = new Stack<MenuContext>();

        /// <summary>
        /// 現在開いているメニューがあるかどうか
        /// </summary>
        public bool IsMenuOpen => _menuStack.Count > 0;

        /// <summary>
        /// 新しいメニューを開きます。
        /// </summary>
        /// <param name="menu">開くメニュー</param>
        /// <param name="onResult">メニューが閉じたときに呼ばれるコールバック（結果を受け取る）</param>
        public void Push(TonMenu menu, Action<object> onResult = null)
        {
            // 現在のトップメニューがあれば Pause
            if (_menuStack.Count > 0)
            {
                var current = _menuStack.Peek();
                current.Menu.IsActive = false; // アクティブフラグオフ
                current.Menu.OnPause?.Invoke();
            }

            // 新しいメニューを作成して Push
            var context = new MenuContext
            {
                Menu = menu,
                OnResult = onResult
            };
            _menuStack.Push(context);

            // 初期化
            menu.IsActive = true;
            menu.OnEnter?.Invoke();
        }

        /// <summary>
        /// 現在のメニューを閉じ、前のメニューに戻ります。
        /// </summary>
        /// <param name="result">親メニューに渡す結果データ（キャンセル時はnull推奨）</param>
        public void Pop(object result = null)
        {
            if (_menuStack.Count == 0) return;

            // 閉じるメニューの処理
            var closingContext = _menuStack.Pop();
            closingContext.Menu.IsActive = false;
            closingContext.Menu.OnExit?.Invoke();

            // コールバック呼び出し
            closingContext.OnResult?.Invoke(result);

            // メニューがまだ残っていれば、新しいトップを再開
            if (_menuStack.Count > 0)
            {
                var current = _menuStack.Peek();
                current.Menu.IsActive = true;
                current.Menu.OnResume?.Invoke();
            }
        }

        /// <summary>
        /// すべてのメニューを閉じます。
        /// </summary>
        public void Clear()
        {
            // スタックが空になるまでPopし続ける
            // （ただしコールバック地獄になる可能性があるので、イベントだけ呼んでクリアするほうが安全かも）
            // ここでは簡易的に Stack をクリアし、OnExit だけ呼ぶ形にする
            
            while (_menuStack.Count > 0)
            {
                var context = _menuStack.Pop();
                context.Menu.IsActive = false;
                context.Menu.OnExit?.Invoke();
                // コールバックは呼ばない（強制終了のため）
            }
        }

        private string _inputOk = "A";
        private string _inputCancel = "B";

        /// <summary>
        /// 操作に使用するボタン割り当てを設定します。
        /// </summary>
        /// <param name="ok">決定操作のボタン名</param>
        /// <param name="cancel">キャンセル操作のボタン名</param>
        public void SetInputButtons(string ok, string cancel)
        {
            _inputOk = ok;
            _inputCancel = cancel;
        }

        /// <summary>
        /// 入力更新処理。スタックのトップにあるメニューのみ制御します。
        /// </summary>
        public void Update()
        {
            if (_menuStack.Count == 0) return;

            var current = _menuStack.Peek();
            var menu = current.Menu;

            // 入力中継
            if (Ton.Input.IsJustPressed("UP")) menu.Control(TonMenu.InputType.Up);
            if (Ton.Input.IsJustPressed("DOWN")) menu.Control(TonMenu.InputType.Down);
            if (Ton.Input.IsJustPressed("LEFT")) menu.Control(TonMenu.InputType.Left);
            if (Ton.Input.IsJustPressed("RIGHT")) menu.Control(TonMenu.InputType.Right);
            
            if (Ton.Input.IsJustPressed(_inputOk)) menu.Control(TonMenu.InputType.OK);
            if (Ton.Input.IsJustPressed(_inputCancel)) menu.Control(TonMenu.InputType.Cancel);

            // 自動戻り処理
            // TonMenu自体がキャンセル状態になったら、Manager側でPopする
            if (menu.IsCancelled)
            {
                Pop(null); // キャンセルとしてPop
            }
        }

        /// <summary>
        /// 描画処理。スタックの下（奥）にあるメニューから順に描画します。
        /// </summary>
        /// <param name="backgroundDrawer">メニューごとの背景描画を行うコールバック(Optional)</param>
        public void Draw(Action<TonMenu> backgroundDrawer = null)
        {
            if (_menuStack.Count == 0) return;

            // Stackは列挙すると 上->下 の順になるため、描画順としては逆（下->上）にしたい
            // 配列にして逆順で回す
            var menus = _menuStack.ToArray();
            for (int i = menus.Length - 1; i >= 0; i--)
            {
                var m = menus[i].Menu;
                // 背景描画コールバック
                backgroundDrawer?.Invoke(m);
                // メニュー内容描画
                m.Draw();
            }
        }
    }
}
