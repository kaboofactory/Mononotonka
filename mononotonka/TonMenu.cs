using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mononotonka
{
    /// <summary>
    /// RPG風のメニューシステムクラス。
    /// グリッドレイアウト、スクロール、柔軟なアイテム表示（テキスト、アイコン、複合レイアウト）をサポートします。
    /// </summary>
    public class TonMenu
    {
        /// <summary>
        /// メニュー操作のための入力タイプ定義。
        /// 物理的なキー入力（矢印キー、ボタン）を抽象化して扱います。
        /// </summary>
        public enum InputType
        {
            /// <summary>上方向への移動</summary>
            Up,
            /// <summary>下方向への移動</summary>
            Down,
            /// <summary>左方向への移動</summary>
            Left,
            /// <summary>右方向への移動</summary>
            Right,
            /// <summary>決定動作</summary>
            OK,
            /// <summary>キャンセル動作（戻る）</summary>
            Cancel
        }

        // --- プロパティ ---

        /// <summary>
        /// メニュー項目のリスト。
        /// </summary>
        public List<TonMenuItem> Items { get; private set; } = new List<TonMenuItem>();
        
        /// <summary>
        /// 現在のカーソル位置 (Itemsリスト内でのインデックス)。
        /// 0 から Items.Count - 1 までの値、または空白選択許可時はそれ以上の値をとる場合があります。
        /// </summary>
        public int CursorIndex { get; private set; } = 0;

        /// <summary>
        /// スクロール制御用のオフセット（表示開始行）。
        /// アイテム数が表示可能行数を超える場合、ここを起点に描画します。
        /// </summary>
        public int ScrollOffset { get; private set; } = 0;

        // 表示領域設定
        
        /// <summary>メニュー全体の描画領域（スクリーン座標）。</summary>
        public Rectangle WindowRect { get; private set; }
        
        /// <summary>グリッドの列数（横に並べる数）。</summary>
        public int ColCount { get; private set; }
        
        /// <summary>グリッドの行数（縦に並べる数）。これは「一度に表示される行数」です。</summary>
        public int RowCount { get; private set; }
        
        /// <summary>各アイテムの幅（ピクセル）。</summary>
        public int ItemWidth { get; private set; }
        
        /// <summary>各アイテムの高さ（ピクセル）。</summary>
        public int ItemHeight { get; private set; }
        
        /// <summary>
        /// アイテムが存在しない空白部分にもカーソル移動を許可するかどうか。
        /// trueの場合、グリッドのマス目全てに移動可能になります（最後の行の余白など）。
        /// </summary>
        public bool AllowBlankSelect { get; private set; }
        
        /// <summary>複数選択モードを許可するかどうか（現在は未実装または予約プロパティ）。</summary>
        public bool AllowMultiSelect { get; private set; }

        /// <summary>メニューがアクティブ（操作可能・ハイライト表示）かどうか。</summary>
        public bool IsActive { get; set; } = true;

        // スタイル設定

        /// <summary>デフォルトで使用するフォントID（TonGraphicsに登録されたもの）。nullならシステムのデフォルト。</summary>
        public string DefaultFontId { get; private set; } = null;
        
        /// <summary>通常時のテキスト色。</summary>
        public Color DefaultTextColor { get; private set; } = Color.White;
        
        /// <summary>無効化（Enabled=false）されたアイテムのテキスト色。</summary>
        public Color DisabledTextColor { get; private set; } = Color.Gray;
        
        /// <summary>選択中のアイテムの背景色。</summary>
        public Color SelectedCursorColor { get; private set; } = Color.FromNonPremultiplied(255, 255, 200, 100); 
        
        /// <summary>カーソル枠（アウトライン）の色。</summary>
        public Color CursorColor { get; private set; } = Color.White; 

        /// <summary>コンテンツ（テキスト・アイコン）の描画スケール倍率（デフォルトは1.0）。縮小・拡大表示に使用。</summary>
        public float ContentScale { get; private set; } = 1.0f;
        
        // カーソル挙動

        /// <summary>テキスト描画時のX座標オフセット（パディング）。デフォルトは10。</summary>
        public int TextOffset { get; private set; } = 10;

        /// <summary>カーソルアイコン画像名。</summary>
        public string CursorIcon { get; private set; } = null;

        /// <summary>カーソルアイコンの表示位置オフセット。</summary>
        public Vector2 CursorIconOffset { get; private set; } = Vector2.Zero;

        /// <summary>カーソル移動時に端から反対側へループするかどうか。</summary>
        public bool IsLoop { get; private set; } = false;

        // 状態フラグ

        /// <summary>このフレームで決定操作が行われたかどうか。</summary>
        public bool IsDecided { get; private set; }
        
        /// <summary>このフレームでキャンセル操作が行われたかどうか。</summary>
        public bool IsCancelled { get; private set; }

        // ライフサイクルイベント
        
        /// <summary>メニューが表示された（アクティブになった）瞬間に呼ばれるアクション。</summary>
        public Action OnEnter;
        
        /// <summary>メニューが閉じる（非アクティブになる）瞬間に呼ばれるアクション。</summary>
        public Action OnExit;
        
        /// <summary>一時停止（サブメニューが開くなどして操作権を手放す）時に呼ばれるアクション。</summary>
        public Action OnPause;
        
        /// <summary>再開（サブメニューから戻ってきた時など）時に呼ばれるアクション。</summary>
        public Action OnResume;

        /// <summary>
        /// 描画完了後に呼ばれるアクション。
        /// スクロールバーやカスタムな装飾を描画するために使用します。
        /// </summary>
        public Action<TonMenu> OnPostDraw;

        /// <summary>カーソル位置が変更された時に呼ばれるアクション。引数は選択されたアイテム。</summary>
        public Action<TonMenuItem> OnSelectionChanged;

        // --- 初期化 ---

        /// <summary>
        /// TonMenuのコンストラクタ。
        /// </summary>
        /// <param name="rect">メニューの表示領域（X, Y, Width, Height）</param>
        /// <param name="column">列数（横のアイテム数）</param>
        /// <param name="row">行数（縦のアイテム数=表示行数）</param>
        /// <param name="width">1アイテムあたりの幅</param>
        /// <param name="height">1アイテムあたりの高さ</param>
        /// <param name="bAllowBlankSelect">アイテムがない場所へのカーソル移動を許可するか</param>
        /// <param name="bAllowMultiSelect">複数選択を許可するか（現在は未使用）</param>
        public TonMenu(Rectangle rect, int column, int row, int width, int height, bool bAllowBlankSelect, bool bAllowMultiSelect = false)
        {
            WindowRect = rect;
            ColCount = column;
            RowCount = row;
            ItemWidth = width;
            ItemHeight = height;
            AllowBlankSelect = bAllowBlankSelect;
            AllowMultiSelect = bAllowMultiSelect;
        }

        // --- メソッド ---

        /// <summary>
        /// テキスト描画時のオフセットを設定します。
        /// </summary>
        /// <param name="offset">オフセット値(px)</param>
        public void SetTextOffset(int offset)
        {
            TextOffset = offset;
        }

        /// <summary>
        /// コンテンツ（テキスト・アイコン）の描画スケールを設定します。
        /// </summary>
        /// <param name="scale">スケール値 (1.0 = 等倍)</param>
        public void SetContentScale(float scale)
        {
            ContentScale = scale;
        }

        /// <summary>
        /// カーソルアイコンを設定します。
        /// </summary>
        /// <param name="iconName">アイコン画像名（Ton.Gra.LoadTextureでロードしたもの）</param>
        /// <param name="offset">表示位置オフセット（省略時はZero）</param>
        public void SetCursorIcon(string iconName, Vector2? offset = null)
        {
            CursorIcon = iconName;
            CursorIconOffset = offset ?? Vector2.Zero;
        }

        /// <summary>
        /// フォントを設定します。
        /// </summary>
        /// <param name="fontId">フォントID</param>
        public void SetFont(string fontId)
        {
            DefaultFontId = fontId;
        }

        /// <summary>
        /// テキストカラーを設定します。
        /// </summary>
        /// <param name="defaultColor">通常時の色</param>
        /// <param name="disabledColor">無効時の色（省略時はデフォルト）</param>
        public void SetTextColor(Color defaultColor, Color? disabledColor = null)
        {
            DefaultTextColor = defaultColor;
            if (disabledColor.HasValue) DisabledTextColor = disabledColor.Value;
        }

        /// <summary>
        /// カーソル（背景・枠）の色を設定します。
        /// </summary>
        /// <param name="selectedColor">選択時の背景色</param>
        /// <param name="frameColor">枠の色（省略時は白）</param>
        public void SetCursorColor(Color selectedColor, Color? frameColor = null)
        {
            SelectedCursorColor = selectedColor;
            if (frameColor.HasValue) CursorColor = frameColor.Value;
        }

        /// <summary>
        /// カーソルループ挙動を設定します。
        /// </summary>
        /// <param name="isLoop">ループするかどうか</param>
        public void SetLoopable(bool isLoop)
        {
            IsLoop = isLoop;
        }

        /// <summary>
        /// メニューにアイテムを追加します。
        /// 追加時にアイテムのParentMenuプロパティが自動的に設定されます。
        /// </summary>
        /// <param name="item">追加する項目</param>
        public void AddItem(TonMenuItem item)
        {
            Items.Add(item);
            item.ParentMenu = this;
        }

        /// <summary>
        /// メニューの内容をクリアし、カーソル位置やスクロールをリセットします。
        /// </summary>
        public void Clear()
        {
            Items.Clear();
            CursorIndex = 0;
            ScrollOffset = 0;
            IsDecided = false;
            IsCancelled = false;
        }

        /// <summary>
        /// 現在カーソルが合っているアイテムを取得します。
        /// 空白選択許可モードで、アイテムがない場所を選択している場合はnullになります。
        /// </summary>
        /// <returns>現在のアイテム、またはnull</returns>
        public TonMenuItem GetCurrentItem()
        {
            if (CursorIndex >= 0 && CursorIndex < Items.Count)
            {
                return Items[CursorIndex];
            }
            return null;
        }

        /// <summary>
        /// 外部からの入力制御を行い、カーソル移動や決定/キャンセル処理を実行します。
        /// </summary>
        /// <param name="type">入力の種類</param>
        public void Control(InputType type)
        {
            IsDecided = false;
            IsCancelled = false;

            // 移動可能な最大インデックスを取得（アイテム数、またはグリッドのマス数）
            int maxIndex = GetMaxIndex();
            if (maxIndex < 0) return; // アイテムが全くない場合は何もしない

            int prevIndex = CursorIndex;
            
            // 現在のグリッド上の位置（行・列）を計算
            int currentRow = CursorIndex / ColCount;
            int currentCol = CursorIndex % ColCount;

            // 全行数（空白含む仮想的な行数）を計算
            // MaxIndexまで含むために必要な行数
            int totalRows = (maxIndex / ColCount) + 1;

            switch (type)
            {
                case InputType.Up:
                    currentRow--;
                    if (currentRow < 0)
                    {
                        // 上端を超えた場合のループ処理
                        if (IsLoop) currentRow = totalRows - 1;
                        else currentRow = 0;
                    }
                    break;

                case InputType.Down:
                    currentRow++;
                    if (currentRow >= totalRows)
                    {
                        // 下端を超えた場合のループ処理
                        if (IsLoop) currentRow = 0;
                        else currentRow = totalRows - 1;
                    }
                    break;

                case InputType.Left:
                    if (ColCount > 1) // 1列の場合は左右移動なし
                    {
                        currentCol--;
                        if (currentCol < 0)
                        {
                            // 左端を超えた場合のループ処理
                            if (IsLoop) currentCol = ColCount - 1;
                            else currentCol = 0;
                        }
                    }
                    break;

                case InputType.Right:
                    if (ColCount > 1) // 1列の場合は左右移動なし
                    {
                        currentCol++;
                        if (currentCol >= ColCount)
                        {
                            // 右端を超えた場合のループ処理
                            if (IsLoop) currentCol = 0;
                            else currentCol = ColCount - 1;
                        }
                    }
                    break;

                case InputType.OK:
                    var item = GetCurrentItem();
                    if (item != null)
                    {
                        // 無効アイテムは選択できない（音を鳴らすなどのFBが必要かも）
                        if (!item.Enabled)
                        {
                            // 無効アイテム選択時
                        }
                        else
                        {
                            // 決定フラグを立て、アイテムごとのコールバックを実行
                            IsDecided = true;
                            item.OnDecided?.Invoke(item); 
                        }
                    }
                    else if (AllowBlankSelect)
                    {
                        // アイテムがない場所（空白）を選択した場合も決定とする
                        IsDecided = true;
                    }
                    break;

                case InputType.Cancel:
                    IsCancelled = true;
                    break;
            }

            // 新しいインデックスを再計算
            int nextIndex = currentRow * ColCount + currentCol;
            
            // 計算したインデックスが最大値を超えている場合の補正
            // 例: 最終行のアイテム数が少なく、右側に空白がある場合など
            if (nextIndex > maxIndex)
            {
                // ここでは単純に MaxIndex（データの末尾）に丸める処理としています。
                // つまり、アイテムがない右下の空間にカーソルが行こうとしたら、最後のアイテムに吸着します。
                 nextIndex = maxIndex;
            }

            CursorIndex = nextIndex;

            // カーソル移動があった場合のイベント発火
            if (prevIndex != CursorIndex)
            {
                var current = GetCurrentItem();
                OnSelectionChanged?.Invoke(current);
                // アイテム個別イベントもあれば呼ぶ
                current?.OnSelectionChanged?.Invoke(current);
                
                // 表示範囲（スクロール）の自動調整
                AdjustScroll();
            }
        }

        /// <summary>
        /// 上スクロールが可能かどうかを取得します。
        /// </summary>
        public bool CanScrollUp => ScrollOffset > 0;

        /// <summary>
        /// 下スクロールが可能かどうかを取得します。
        /// </summary>
        public bool CanScrollDown => (ScrollOffset + RowCount) < GetMaxRow();

        private int GetMaxRow()
        {
             return (int)Math.Ceiling((double)Items.Count / ColCount);
        }

        /// <summary>
        /// カーソル位置に合わせてスクロールオフセットを調整し、カーソルが表示範囲内に入るようにします。
        /// AllowBlankSelectの設定により挙動が変わります。
        /// </summary>
        private int GetMaxIndex()
        {
            if (AllowBlankSelect) 
            {
                // 空白選択許可の場合：
                // アイテムを表示するために必要なグリッドの「全セル数 - 1」まで移動可能とします。
                // つまり、最終行の空白部分にもカーソルが行けるようになります。
                int rows = ToalRowsNeeded();
                if (rows == 0) return -1;
                return (rows * ColCount) - 1;
            }
            // 通常時: アイテムリストの最後尾まで
            return Items.Count - 1;
        }

        /// <summary>
        /// 現在のアイテム数を表示するために必要な行数を計算します。
        /// </summary>
        private int ToalRowsNeeded()
        {
            if (Items.Count == 0 && AllowBlankSelect) return 1; // 空でも場所確保のため1行必要
            if (Items.Count == 0) return 0;
            return (int)Math.Ceiling((double)Items.Count / ColCount);
        }

        /// <summary>
        /// カーソル位置に合わせてスクロールオフセットを調整し、カーソルが表示範囲内に入るようにします。
        /// </summary>
        private void AdjustScroll()
        {
            // カーソルが何行目にあるか
            int currentRow = CursorIndex / ColCount;
            
            // 表示範囲より上に行ったら、上にスクロール
            if (currentRow < ScrollOffset)
            {
                ScrollOffset = currentRow;
            }
            // 表示範囲より下に行ったら、下にスクロール（末尾が表示範囲に入るように）
            else if (currentRow >= ScrollOffset + RowCount)
            {
                ScrollOffset = currentRow - RowCount + 1;
            }
        }

        /// <summary>
        /// メニューの描画処理を行います。通常は毎フレーム呼び出します。
        /// 注: ウィンドウ背景の描画はこのメソッド内では行いません（TonMenuManager等で管理）。
        /// </summary>
        public void Draw()
        {
            // --- アイテム描画 ---

            // 現在表示すべきアイテムの範囲を計算
            int startIndex = ScrollOffset * ColCount;
            int endIndex = startIndex + (ColCount * RowCount);
            
            // 描画ループの終了条件: 表示範囲の終わり、または最大移動可能インデックスまで
            int maxDraw = GetMaxIndex() + 1; 

            for (int i = startIndex; i < endIndex; i++)
            {
                // 最大インデックスを超えたら描画終了
                if (i >= maxDraw) break;

                // インデックスに対応するアイテムを取得（範囲外ならnull）
                TonMenuItem item = (i < Items.Count) ? Items[i] : null;
                
                // 画面上のグリッド位置を計算
                int viewIndex = i - startIndex;
                int r = viewIndex / ColCount; // 表示上の行番号 (0 ~ RowCount-1)
                int c = viewIndex % ColCount; // 列番号

                // 描画座標の算出
                int x = WindowRect.X + c * ItemWidth;
                int y = WindowRect.Y + r * ItemHeight; 

                Rectangle itemRect = new Rectangle(x, y, ItemWidth, ItemHeight);

                // --- カーソルハイライト描画 ---
                if (IsActive && i == CursorIndex)
                {
                    // カーソルアイコン描画
                    if (!string.IsNullOrEmpty(CursorIcon))
                    {
                        var iconTex = Ton.Gra.LoadTexture(CursorIcon, CursorIcon);
                        if (iconTex != null)
                        {
                            // アイテムの左側に配置
                            // Y軸はアイテムの中心に合わせる
                            float cx = itemRect.X - iconTex.Width + CursorIconOffset.X;
                            float cy = itemRect.Y + (itemRect.Height - iconTex.Height) / 2 + CursorIconOffset.Y;
                            
                            Ton.Gra.Draw(CursorIcon, (int)cx, (int)cy);
                        }
                    }
                    else
                    {
                        // カーソルアイコンを使用しない時
                        Ton.Gra.FillRect(itemRect.X, itemRect.Y, itemRect.Width, itemRect.Height, SelectedCursorColor * 0.5f); // 半透明合成
                        Ton.Gra.DrawRect(itemRect.X, itemRect.Y, itemRect.Width, itemRect.Height, CursorColor);
                    }
                }
                else if (!IsActive && i == CursorIndex)
                {
                    // 非アクティブ（サブメニューが開いている時など）の選択位置表示
                    // 少し暗くして現在位置を示す
                    Ton.Gra.FillRect(itemRect.X, itemRect.Y, itemRect.Width, itemRect.Height, SelectedCursorColor * 0.5f);
                }

                // --- アイテム内容の描画 ---
                if (item != null)
                {
                    item.Draw(itemRect);
                }
            }

            // 描画後コールバック
            OnPostDraw?.Invoke(this);
        }
    }

    /// <summary>
    /// メニュー内の1つの項目を表すクラス。
    /// 実際の描画内容は Layout（TonMenuPanelなど）によって定義されます。
    /// </summary>
    public class TonMenuItem
    {
        /// <summary>このアイテムが所属している親メニュー。</summary>
        public TonMenu ParentMenu { get; set; }
        
        /// <summary>ユーザーデータを保持するための汎用タグ。</summary>
        public object Tag { get; set; }
        
        /// <summary>有効/無効フラグ。falseの場合は選択しても決定されず、色もグレーアウトします。</summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>このアイテムが決定された時に呼ばれるコールバック。</summary>
        public Action<TonMenuItem> OnDecided;
        
        /// <summary>このアイテムにカーソルが合った時に呼ばれるコールバック。</summary>
        public Action<TonMenuItem> OnSelectionChanged;

        /// <summary>このアイテムの描画レイアウトを管理するルート要素。</summary>
        private TonMenuPanel _layoutRoot;

        /// <summary>
        /// コンストラクタ。デフォルトで空のフリーレイアウトパネルを持ちます。
        /// </summary>
        public TonMenuItem()
        {
            _layoutRoot = new TonMenuPanel(TonMenuPanel.LayoutType.Free);
        }

        /// <summary>
        /// 描画レイアウトを設定します。
        /// </summary>
        /// <param name="panel">ルートとなるパネル</param>
        public void SetLayout(TonMenuPanel panel)
        {
            _layoutRoot = panel;
        }

        /// <summary>
        /// 指定された領域にアイテムを描画します。
        /// 通常はTonMenuから呼び出されます。
        /// </summary>
        /// <param name="rect">描画領域</param>
        public void Draw(Rectangle rect)
        {
            _layoutRoot.Draw(rect, this);
        }
    }

    // --- Elements ---

    /// <summary>
    /// メニューアイテムを構成する描画要素の基底クラス。
    /// テキスト、アイコン、パネルなどがこれを継承します。
    /// </summary>
    public abstract class TonMenuElement
    {
        /// <summary>
        /// 描画を実行します。
        /// </summary>
        /// <param name="area">このエレメントに割り当てられた描画領域</param>
        /// <param name="item">所有しているアイテム情報（親メニューのスタイル参照用）</param>
        public abstract void Draw(Rectangle area, TonMenuItem item);
    }

    /// <summary>
    /// テキストの配置揃え（左寄せ、中央、右寄せ）。
    /// </summary>
    public enum ElementAlignment { Left, Center, Right }

    /// <summary>
    /// テキストを表示するメニュー要素。
    /// 静的な文字列、または動的に変化する文字列（Func）を表示できます。
    /// </summary>
    public class TonMenuText : TonMenuElement
    {
        private Func<string> _textFunc;
        private string _staticText;
        
        /// <summary>使用するフォントID。指定がない場合は親メニューのデフォルトを使用。</summary>
        public string FontId { get; set; } = null;
        
        /// <summary>テキスト色。指定がない場合は親メニューのデフォルトを使用。</summary>
        public Color? Color { get; set; } = null;
        
        /// <summary>描画スケール。指定がない場合は親メニューのデフォルトを使用。</summary>
        public float? Scale { get; set; } = null;
        
        /// <summary>表示位置の揃え方（デフォルトはLeft）。</summary>
        public ElementAlignment Alignment { get; set; } = ElementAlignment.Left;
        
        /// <summary>描画位置の微調整用オフセット。</summary>
        public Vector2 Offset { get; set; } = Vector2.Zero;

        /// <summary>静的テキストで初期化します。</summary>
        public TonMenuText(string text) { _staticText = text; }
        
        /// <summary>動的テキスト（関数）で初期化します。</summary>
        public TonMenuText(Func<string> textFunc) { _textFunc = textFunc; }

        public override void Draw(Rectangle area, TonMenuItem item)
        {
            // 表示文字列の取得
            string text = _textFunc != null ? _textFunc() : _staticText;
            if (string.IsNullOrEmpty(text)) return;

            var menu = item.ParentMenu;
            string fontId = FontId ?? menu.DefaultFontId;
            float scale = Scale ?? menu.ContentScale;
            
            // 色の決定（無効時、指定色、デフォルト色）
            Color drawColor;
            if (!item.Enabled) drawColor = menu.DisabledTextColor;
            else drawColor = Color ?? menu.DefaultTextColor;

            // 非アクティブ時は暗くする
            if (!menu.IsActive)
            {
                drawColor = new Color(drawColor.R / 2, drawColor.G / 2, drawColor.B / 2, drawColor.A);
            }

            // 文字列サイズの計測
            Vector2 size = Ton.Gra.MeasureString(text, fontId) * scale;
            
            // 基準位置の計算
            float x = area.X + Offset.X;
            float y = area.Y + Offset.Y + (area.Height - size.Y) / 2; // 縦は中央揃えデフォルト

            // テキストオフセットの計算
            int textOffset = menu.TextOffset;

            // 水平位置の調整
            switch (Alignment)
            {
                case ElementAlignment.Left:
                    x = x + textOffset;
                    break;
                case ElementAlignment.Center:
                    x = area.X + (area.Width - size.X) / 2 + Offset.X;
                    break;
                case ElementAlignment.Right:
                    x = area.Right - size.X + Offset.X - textOffset;
                    break;
            }

            Ton.Gra.DrawText(text, (int)x, (int)y, drawColor, scale, fontId);
        }
    }

    /// <summary>
    /// 画像（アイコン）を表示するメニュー要素。
    /// </summary>
    public class TonMenuIcon : TonMenuElement
    {
        private Func<string> _iconFunc;
        private string _staticIcon;
        
        /// <summary>描画位置の微調整用オフセット。</summary>
        public Vector2 Offset { get; set; } = Vector2.Zero;
        
        /// <summary>描画スケール（デフォルト1.0）。</summary>
        public float Scale { get; set; } = 1.0f;

        public TonMenuIcon(string iconName) { _staticIcon = iconName; }
        public TonMenuIcon(Func<string> iconFunc) { _iconFunc = iconFunc; }

        public override void Draw(Rectangle area, TonMenuItem item)
        {
            string iconName = _iconFunc != null ? _iconFunc() : _staticIcon;
            if (string.IsNullOrEmpty(iconName)) return;

            // テクスチャ読み込み（キャッシュ利用）
            var tex = Ton.Gra.LoadTexture(iconName, iconName);
            if (tex == null) return;
            
            // 中央配置デフォルト
            float w = tex.Width * Scale;
            float h = tex.Height * Scale;
            
            float x = area.X + (area.Width - w) / 2 + Offset.X;
            float y = area.Y + (area.Height - h) / 2 + Offset.Y;

            // 無効時は半透明などで表現する場合
            TonDrawParam param = new TonDrawParam();
            if (!item.Enabled) param.Alpha = 0.5f;

            // 非アクティブ時は暗くする
            if (!item.ParentMenu.IsActive)
            {
                param.Color = Color.Gray; // 簡易的にグレー乗算
            }

            Ton.Gra.Draw(iconName, (int)x, (int)y, param);
        }
    }

    /// <summary>
    /// 複数の要素をレイアウトして配置するコンテナ要素。
    /// 垂直・水平・自由配置をサポートします。
    /// </summary>
    public class TonMenuPanel : TonMenuElement
    {
        /// <summary>レイアウトの種類。</summary>
        public enum LayoutType 
        { 
            /// <summary>指定なし（すべて同じ領域に重ね書き、またはOffsetで調整）。</summary>
            Free, 
            /// <summary>垂直方向に並べる。</summary>
            Vertical, 
            /// <summary>水平方向に並べる。</summary>
            Horizontal 
        }
        
        private LayoutType _type;
        // 子要素と、そのサイズ比率（または固定サイズ）を保持するリスト
        private List<(TonMenuElement Element, float Ratio)> _children = new List<(TonMenuElement, float)>();

        public TonMenuPanel(LayoutType type)
        {
            _type = type;
        }

        /// <summary>
        /// 子要素を追加します。
        /// </summary>
        /// <param name="element">追加する要素</param>
        /// <param name="ratioOrSize">
        /// Horizontal/Verticalレイアウトの場合の配分比率（0.0~1.0）。
        /// 合計が1.0になるように設定することを推奨します。
        /// </param>
        public void AddChild(TonMenuElement element, float ratioOrSize = 1.0f)
        {
            _children.Add((element, ratioOrSize));
        }

        public override void Draw(Rectangle area, TonMenuItem item)
        {
            if (_children.Count == 0) return;

            if (_type == LayoutType.Free)
            {
                // 全ての子要素を親領域全体に対して描画（オフセット等で個別調整）
                foreach (var child in _children)
                {
                    child.Element.Draw(area, item);
                }
            }
            else if (_type == LayoutType.Horizontal)
            {
                // 水平分割
                float currentX = area.X;
                foreach (var child in _children)
                {
                    float w = area.Width * child.Ratio;
                    // 幅を比率で分割、高さは最大
                    Rectangle childRect = new Rectangle((int)currentX, area.Y, (int)w, area.Height);
                    child.Element.Draw(childRect, item);
                    currentX += w;
                }
            }
            else if (_type == LayoutType.Vertical)
            {
                // 垂直分割
                float currentY = area.Y;
                foreach (var child in _children)
                {
                    float h = area.Height * child.Ratio;
                    // 高さを比率で分割、幅は最大
                    Rectangle childRect = new Rectangle(area.X, (int)currentY, area.Width, (int)h);
                    child.Element.Draw(childRect, item);
                    currentY += h;
                }
            }
        }
    }
}
