using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.IO;

namespace Mononotonka
{
    /// <summary>
    /// セーブ・ロード画面の表示モード定義。
    /// </summary>
    public enum TonSaveLoadMode
    {
        /// <summary>セーブ機能のみ表示</summary>
        SaveOnly,
        /// <summary>ロード機能のみ表示</summary>
        LoadOnly,
        /// <summary>両方表示（初期選択：セーブ）</summary>
        BothDefaultSave,
        /// <summary>両方表示（初期選択：ロード）</summary>
        BothDefaultLoad
    }

    /// <summary>
    /// セーブ・ロードメニューのUIとロジックを管理するクラス。
    /// Ton.Data (TonGameData) と直接連携してセーブ・ロードを行います。
    /// </summary>
    public class TonSaveLoadMenu
    {
        // メニューが開いているかどうかのフラグ
        private bool _isOpen = false;
        
        // モード設定
        private TonSaveLoadMode _configMode; // Open時に指定されたモード設定
        private bool _isSaveMode; // 現在の内部状態（true = セーブモード, false = ロードモード）

        // スロット管理
        // -1: オートセーブ（ロードモードでのみアクセス可能）
        // 0-3: 通常のスロット
        private const int MAX_SLOTS = 4; // 通常スロットの最大数
        private int _selectedSlotIndex = 0; // 選択中のスロットインデックス（-1 ～ 3）
        
        // UI状態
        private bool _isShowDialog = false; // 確認ダイアログを表示中かどうか
        private bool _dialogResult = false; // ダイアログの選択結果（true = YES, false = NO）
        private string _dialogMessage = ""; // ダイアログに表示するメッセージ
        
        // フィードバックメッセージ（「保存しました」など）
        private string _feedbackMessage = "";
        private int _feedbackTimer = 0; // メッセージ表示タイマー（フレーム数）

        /// <summary>
        /// ロード完了時に呼び出されるコールバック。
        /// シーン遷移などの事後処理をここに記述してください。
        /// </summary>
        public Action OnLoaded { get; set; }

        /// <summary>
        /// 指定されたモード設定でメニューを開きます。
        /// </summary>
        /// <param name="mode">表示モード（SaveOnly, LoadOnly, Both...）</param>
        public void Open(TonSaveLoadMode mode)
        {
            Ton.Log.Info($"TonSaveLoadMenu.Open Mode: {mode}");
            Ton.Input.ConsumeInput(); // 誤操作防止のため入力を消費
            
            _isOpen = true;
            _configMode = mode;
            _isShowDialog = false;
            _feedbackMessage = "";

            // モード設定に基づいて初期状態（セーブ画面かロード画面か）を決定
            switch (_configMode)
            {
                case TonSaveLoadMode.SaveOnly:
                    _isSaveMode = true;
                    break;
                case TonSaveLoadMode.LoadOnly:
                    _isSaveMode = false;
                    break;
                case TonSaveLoadMode.BothDefaultSave:
                    _isSaveMode = true;
                    break;
                case TonSaveLoadMode.BothDefaultLoad:
                    _isSaveMode = false;
                    break;
            }

            // カーソル位置の初期化
            _selectedSlotIndex = 0;
        }

        /// <summary>
        /// メニューを閉じます。
        /// </summary>
        public void Close()
        {
            _isOpen = false;
            Ton.Input.ConsumeInput(); // 閉じた直後の誤操作防止
        }

        /// <summary>
        /// メニューが開いているかどうかを確認します。
        /// </summary>
        /// <returns>開いている場合は true</returns>
        public bool IsOpen()
        {
            return _isOpen;
        }

        /// <summary>
        /// メニューの更新処理。入力操作を行います。
        /// </summary>
        public void Update()
        {
            if (!_isOpen) return;

            // フィードバックメッセージの表示タイマーを減らす
            if (_feedbackTimer > 0) _feedbackTimer--;

            // ダイアログ表示中はダイアログの操作のみ受け付ける
            if (_isShowDialog)
            {
                UpdateDialog();
                return;
            }

            // タブ切り替え操作（L/Rボタン）
            // 両方表示モードの場合のみ切り替え可能
            if (CanSwitchMode())
            {
                if (Ton.Input.IsJustPressed("Left") || Ton.Input.IsJustPressed("Right"))
                {
                    _isSaveMode = !_isSaveMode; // モード反転
                    
                    // セーブモードに切り替えた際、もしオートセーブ(-1)を選択していたらスロット0に戻す
                    // （オートセーブスロットへの手動セーブは不可のため）
                    if (_isSaveMode && _selectedSlotIndex == -1)
                    {
                        _selectedSlotIndex = 0;
                    }
                }
            }

            // スロット選択操作（上下キー）
            if (Ton.Input.IsJustPressed("Up"))
            {
                _selectedSlotIndex--;
                
                // 範囲チェックとループ処理
                // セーブモード時: 0 ～ 3
                // ロードモード時: -1(オートセーブ) ～ 3
                int minIndex = _isSaveMode ? 0 : -1;
                
                // 最小値より小さくなったら最大値（一番下）へループ
                if (_selectedSlotIndex < minIndex) _selectedSlotIndex = MAX_SLOTS - 1;
            }
            if (Ton.Input.IsJustPressed("Down"))
            {
                _selectedSlotIndex++;
                
                // 最大値を超えたら最小値（一番上）へループ
                if (_selectedSlotIndex >= MAX_SLOTS)
                {
                    _selectedSlotIndex = _isSaveMode ? 0 : -1;
                }
            }

            // 決定操作（Aボタン）
            if (Ton.Input.IsJustPressed("A"))
            {
                OnConfirm();
            }

            // キャンセル操作（Bボタン）
            if (Ton.Input.IsJustPressed("B"))
            {
                Close();
            }
        }

        /// <summary>
        /// モード切り替えが可能かどうかを判定します。
        /// </summary>
        private bool CanSwitchMode()
        {
            // Both設定の時だけ切り替え可能
            return _configMode == TonSaveLoadMode.BothDefaultSave || _configMode == TonSaveLoadMode.BothDefaultLoad;
        }

        /// <summary>
        /// 決定ボタン押下時の処理。
        /// セーブ・ロードの実行確認を行います。
        /// </summary>
        private void OnConfirm()
        {
            // ロードモードの場合、データが存在するかチェック
            if (!_isSaveMode)
            {
                string filename = GetSaveFileName(_selectedSlotIndex);
                if (!Ton.Storage.Exists(filename))
                {
                    // データが無い場合はメッセージを表示して終了
                    ShowFeedback("No Data");
                    return;
                }
            }
            // セーブモードの場合は常に可能（Ton.Dataは常に存在するため）

            // 確認ダイアログの準備
            string action = _isSaveMode ? "SAVE" : "LOAD"; // 操作名
            string slotName = _selectedSlotIndex == -1 ? "AUTO SAVE" : $"SLOT {_selectedSlotIndex + 1}"; // スロット名
            _dialogMessage = $"{action} to {slotName}?"; // メッセージ「～しますか？」
            
            _isShowDialog = true; // ダイアログ表示フラグON
            _dialogResult = false; // デフォルトは「No」（安全のため）
        }

        /// <summary>
        /// 確認ダイアログ表示中の更新処理。
        /// </summary>
        private void UpdateDialog()
        {
             // 左右キーでYes/No選択
             if (Ton.Input.IsJustPressed("Left") || Ton.Input.IsJustPressed("Right"))
             {
                 _dialogResult = !_dialogResult;
             }

             // Aボタンで決定
             if (Ton.Input.IsJustPressed("A"))
             {
                 if (_dialogResult) // YESが選択された場合
                 {
                     if (_isSaveMode) ExecuteSave();
                     else ExecuteLoad();
                 }
                 _isShowDialog = false; // ダイアログを閉じる
                 Ton.Input.ConsumeInput();
             }
             
             // Bボタンでキャンセル
             if (Ton.Input.IsJustPressed("B"))
             {
                 _isShowDialog = false; // ダイアログを閉じる
                 Ton.Input.ConsumeInput();
             }
        }

        /// <summary>
        /// セーブ処理の実行。
        /// </summary>
        private void ExecuteSave()
        {
            if (_selectedSlotIndex == -1) return; // オートセーブスロットへの手動セーブは不可

            string filename = GetSaveFileName(_selectedSlotIndex);
            
            try
            {
                // セーブ前の事前更新フック呼び出し
                Ton.Data.BeforeSave();

                // Ton.Data (TonGameData) を直接保存
                Ton.Storage.Save(filename, Ton.Data);
                
                ShowFeedback("Saved Successfully!"); // 成功メッセージ表示
                Ton.Sound.PlaySE("save");
            }
            catch(Exception ex)
            {
                Ton.Log.Error($"Save Error: {ex.Message}");
                ShowFeedback("Save Failed");
            }
        }

        /// <summary>
        /// ロード処理の実行。
        /// </summary>
        private void ExecuteLoad()
        {
            string filename = GetSaveFileName(_selectedSlotIndex);
            
            try
            {
                // TonGameData としてロード
                var loadedData = Ton.Storage.Load<TonGameData>(filename);
                
                if (loadedData != null)
                {
                    // インスタンスを差し替え
                    Ton.Instance.gamedata = loadedData;
                    
                    // ロード後の復元フック呼び出し
                    Ton.Data.AfterLoad();
                    
                    // 開発者定義の完了処理実行
                    OnLoaded?.Invoke();

                    ShowFeedback("Loaded Successfully!");
                    Ton.Sound.PlaySE("load");
                }
                else
                {
                    ShowFeedback("Load Failed");
                }
            }
            catch(Exception ex)
            {
                Ton.Log.Error($"Load Error: {ex.Message}");
                ShowFeedback("Load Exception");
            }
        }
        
        /// <summary>
        /// オートセーブを実行する静的メソッド。
        /// Ton.Data の内容を保存します。
        /// </summary>
        public static void ExecuteAutoSave()
        {
            try
            {
                string filename = "autosave.json";
                Ton.Data.BeforeSave(); // 事前更新
                Ton.Storage.Save(filename, Ton.Data);
                Ton.Log.Info("AutoSave executed.");
            }
            catch(Exception ex)
            {
                Ton.Log.Error("AutoSave failed: " + ex.Message);
            }
        }

        /// <summary>
        /// スロットインデックスに対応するファイル名を取得します。
        /// </summary>
        /// <param name="index">スロットインデックス (-1 ～ 3)</param>
        /// <returns>ファイル名</returns>
        private string GetSaveFileName(int index)
        {
            if (index == -1) return "autosave.json";
            return $"save_{index}.json";
        }

        /// <summary>
        /// フィードバックメッセージ（一時的な通知）を表示します。
        /// </summary>
        /// <param name="msg">表示するメッセージ</param>
        private void ShowFeedback(string msg)
        {
            _feedbackMessage = msg;
            _feedbackTimer = 120; // 60fpsで約2秒間表示
        }

        /// <summary>
        /// メニューの描画処理。
        /// </summary>
        public void Draw()
        {
            if (!_isOpen) return;

            int w = Ton.Game.VirtualWidth;
            int h = Ton.Game.VirtualHeight;

            // 背景オーバーレイ（画面を少し暗くする）
            Ton.Gra.FillRect(10, 10, w - 20, h - 20, new Color(0, 0, 0, 180));

            // Saveテキスト描画
            if(_configMode != TonSaveLoadMode.LoadOnly)
            {
                Color headerColor;

                if (_isSaveMode)
                {
                    headerColor = Color.Orange;
                }else
                {
                    headerColor = Color.Gray;
                }

                Ton.Gra.DrawText("- Save -", 50, 50, headerColor, 1.0f);
            }

            // Loadテキスト描画
            if (_configMode != TonSaveLoadMode.SaveOnly)
            {
                Color headerColor;

                if (!_isSaveMode)
                {
                    headerColor = Color.Orange;
                }
                else
                {
                    headerColor = Color.Gray;
                }

                Ton.Gra.DrawText("- Load -", 350, 50, headerColor, 1.0f);
            }

            // スロットリストの描画位置設定
            int startY = 120; // 描画開始Y座標
            int slotHeight = 80; // スロット1つ分の高さ
            
            int displayY = startY;
            
            // スロット1つ分を描画するローカル関数
            void DrawSlot(int slotIndex, int yPos)
            {
                bool isSelected = (slotIndex == _selectedSlotIndex);
                Color baseColor = isSelected ? Color.Yellow : Color.White; // 選択中は黄色
                if (_isShowDialog) baseColor = Color.Gray; // ダイアログ表示中は暗くする

                // 枠線の描画
                Ton.Gra.DrawRect(100, yPos, w - 200, slotHeight - 10, baseColor, isSelected ? 2 : 1);
                
                // タイトルテキストの描画（AUTO SAVE または FILE X）
                string title = (slotIndex == -1) ? "AUTO SAVE" : $"FILE {slotIndex + 1}";
                Ton.Gra.DrawText(title, 120, yPos + 10, baseColor);

                // メタ情報（データの有無）の描画
                string filename = GetSaveFileName(slotIndex);
                if (Ton.Storage.Exists(filename))
                {
                    // データが存在する場合
                    try
                    {
                        // 具体的な日時などが取得できないため、ひとまず"Data Exists"と表示
                        // 将来的にはここでセーブ日時を表示すると親切
                        Ton.Gra.DrawText("Data Exists", 300, yPos + 25, Color.Gray, 0.7f);
                    }
                    catch {}
                }
                else
                {
                    // データが無い場合
                    Ton.Gra.DrawText("-- NO DATA --", 300, yPos + 25, Color.DarkGray, 0.7f);
                }
            }

            // ロードモードの場合、先頭に「オートセーブ」を表示
            if (!_isSaveMode)
            {
                DrawSlot(-1, displayY);
                displayY += slotHeight;
            }

            // 通常スロット（0～MAX_SLOTS）を順に描画
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                DrawSlot(i, displayY);
                displayY += slotHeight;
            }

            // 確認ダイアログの描画
            if (_isShowDialog)
            {
                // ダイアログのサイズと位置計算
                int dw = 400; // 幅
                int dh = 200; // 高さ
                int dx = (w - dw) / 2; // X位置（中央）
                int dy = (h - dh) / 2; // Y位置（中央より少し上でもよいが今回は中央）
                
                // ダイアログ背景（半透明の濃い青）
                Ton.Gra.FillRect(dx, dy, dw, dh, new Color(0, 0, 50, 240));
                // ダイアログ枠線（白）
                Ton.Gra.DrawRect(dx, dy, dw, dh, Color.White, 2);
                
                // メッセージ描画
                Ton.Gra.DrawText(_dialogMessage, dx + 50, dy + 50, Color.White, 0.7f);
                
                // Yes/No 選択肢描画
                string yesStr = _dialogResult ? "> YES" : "  YES";
                string noStr = !_dialogResult ? "> NO" : "  NO";
                
                Ton.Gra.DrawText(yesStr, dx + 60, dy + 120, _dialogResult ? Color.Yellow : Color.White, 0.8f);
                Ton.Gra.DrawText(noStr, dx + 200, dy + 120, !_dialogResult ? Color.Yellow : Color.White, 0.8f);
            }

            // フィードバックメッセージの描画（画面下部にポップアップ）
            if (_feedbackTimer > 0)
            {
                 Ton.Gra.DrawText(_feedbackMessage, w/2 - 100, h - 90, Color.Lime, 0.7f);
            }
        }
    }
}

