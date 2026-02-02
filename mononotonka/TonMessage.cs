using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace Mononotonka
{
    /// <summary>
    /// メッセージウィンドウ管理クラス。
    /// テキストの表示、演出制御（コマンドタグ）、ウィンドウ描画を行います。
    /// </summary>
    public class TonMessage
    {
        // 内部クラス：コマンド定義
        private enum CmdType { Text, NewLine, SetSize, SetColor, SetSpeed, SetRotate, SetFont, SetShake, Reset, Wait, Icon, Next, Event, End, Input }
        private class MessageCommand
        {
            public CmdType Type;
            public string StringValue;
            public float FloatValue;
            public int IntValue;
            public Color ColorValue;
            public float ShakeX, ShakeY, ShakeSpeed;
        }

        // 内部クラス：描画オブジェクト
        private class DrawObject
        {
            public string Text;
            public string IconName; // アイコンの場合
            public int X, Y;
            public Color Color;
            public float Scale;
            public float Rotate;
            public string FontId;
            public float ShakeX, ShakeY, ShakeSpeed;
            public bool Visible = false;
        }

        // ウィンドウ領域
        private Rectangle _windowRect;
        
        // スクリプト解析用
        private string _rawScript;
        private List<MessageCommand> _commands;
        private int _commandIndex = 0;
        
        // レイアウト設定
        private float _lineSpacing = 10f; // 行間のマージン
        private float _currentLineMaxHeight = 0f; // 現在の行の最大高さ
        private float _kerningOffset = 0f;
        private float _forceNextWaitTimer = 0f; // 強制ページ送りのタイマー
        private float _defaultScale = 1.0f;
        private float _defaultCharSpeed = 50f; // デフォルトの文字送り速度


        /// <summary>
        /// 文字の基本スタイルを設定します。
        /// </summary>
        /// <param name="scale">スケール値 (1.0f = 等倍)</param>
        /// <param name="lineSpacing">行間の高さ(pixel)</param>
        /// <param name="kerningOffset">文字間隔のオフセット(pixel)</param>
        public void SetTextStyle(float scale, float lineSpacing = 10.0f, float kerningOffset = 3.0f)
        {
            _defaultScale = scale;
            _lineSpacing = lineSpacing;
            _kerningOffset = kerningOffset;
        }

        /// <summary>
        /// デフォルトの文字送り速度を設定します。
        /// </summary>
        /// <param name="speedMs">1文字あたりの表示時間(ms)</param>
        public void SetTextSpeed(float speedMs)
        {
            _defaultCharSpeed = speedMs;
            // 現在の速度も更新（ただしスクリプト実行中でタグによる上書きがある場合は注意だが、
            // 基本設定の変更という意味で即時反映させておく）
            _charSpeed = speedMs;
        }

        /// <summary>
        /// 入力待ちアイコンの画像を設定します。nullを指定するとデフォルト（矩形）に戻ります。
        /// </summary>
        /// <param name="imageId">画像ID</param>
        public void SetInputWaitingIcon(string imageId)
        {
            _inputWaitingIconId = imageId;
        }

        // メッセージキュー
        private List<string> _drawQueue = new List<string>();
        
        // 制御変数
        private bool _isActive = false;
        private bool _isBusy = false;
        private bool _waitInput = false;

        private bool _isScriptEnded = false;
        private bool _isInputEnabled = true; // 入力(スキップ/ページ送り)が有効かどうか


        
        // 描画演出ステート
        private float _timer = 0;
        private float _charSpeed = 50f; // ms per char
        private float _waitTimer = 0;
        
        // 現在のテキストスタイル
        private float _currentScale = 1.0f;
        private Color _currentColor = Color.White;
        private float _currentRotate = 0f;
        private string _currentFontId = null; // フォントID (null=デフォルト)
        private float _currentShakeX = 0f;
        private float _currentShakeY = 0f;
        private float _currentShakeSpeed = 20f;
        private double _shakeTimer = 0; // 振動用タイマー(累積)

        // カーソル位置
        private float _cursorX, _cursorY;

        // 描画オブジェクトリスト（一文字、アイコン等）
        private List<DrawObject> _drawObjects = new List<DrawObject>();
        
        // イベント通知用(旧リンク)
        private Queue<string> _eventQueue = new Queue<string>();

        // 入力待ちアイコンID
        private string _inputWaitingIconId = null;

        /// <summary>
        /// 初期化処理。デフォルトのウィンドウ位置を設定します。
        /// </summary>
        public void Initialize()
        {
            // デフォルトは画面下部に配置
            int w = Ton.Game.VirtualWidth;
            int h = Ton.Game.VirtualHeight;
            _windowRect = new Rectangle(20, h - 200, w - 40, 180);
        }

        /// <summary>
        /// ウィンドウの位置とサイズを設定します。
        /// </summary>
        public void SetWindowRect(int x, int y, int width, int height)
        {
            _windowRect = new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// スクリプトファイルを読み込み、再生します。
        /// </summary>
        /// <param name="filePath">コンテンツルートからの相対パス (例: "script/test.txt")</param>
        /// <param name="scriptName">スクリプト内のラベル名 (例: "Opening")。nullの場合はファイル全体。</param>
        public void LoadScript(string filePath, string scriptName = null)
        {
            try
            {
                string fullPath = System.IO.Path.Combine(Ton.Game.Content.RootDirectory, filePath);
                string content = "";

                // TitleContainerを使用してストリームを開き、読み込んだら即座に閉じる
                using (var stream = Microsoft.Xna.Framework.TitleContainer.OpenStream(fullPath))
                using (var reader = new System.IO.StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }

                if (string.IsNullOrEmpty(scriptName))
                {
                    // ラベル指定なし：全体を再生
                    Show(content);
                }
                else
                {
                    // ラベル検索して抽出
                    string targetLabel = "#" + scriptName;
                    
                    int startIndex = content.IndexOf(targetLabel);
                    if (startIndex < 0)
                    {
                        Ton.Log.Error($"Script Label '{scriptName}' not found in {filePath}");
                        Show($"Error: Label '{scriptName}' not found[n]in {filePath}[next]");
                        return;
                    }

                    // ラベル行の終わりを探して、次の行から開始
                    int labelNewline = content.IndexOf('\n', startIndex);
                    if (labelNewline >= 0)
                    {
                        startIndex = labelNewline + 1;
                    }
                    else
                    {
                         // ファイル末尾など
                         startIndex = content.Length;
                    }

                    int endIndex = content.IndexOf("#", startIndex);
                    if (endIndex < 0) endIndex = content.Length;

                    // 範囲外チェック
                    if (startIndex >= endIndex)
                    {
                         Show(""); // 空
                         return;
                    }

                    string extracted = content.Substring(startIndex, endIndex - startIndex);
                    Ton.Log.Info($"Script Loaded: {scriptName}");
                    Show(extracted.Trim());
                }
            }
            catch (System.Exception ex)
            {
                Ton.Log.Error($"Failed to load script: {filePath} ({ex.Message})");
            }
        }

        /// <summary>
        /// テキストを表示します（スクリプト実行開始）。
        /// </summary>
        /// <param name="scriptText">表示するスクリプト文字列（タグ含む）</param>
        public void Show(string scriptText)
        {
            _rawScript = scriptText;
            ParseScript();
            _isActive = true;
            _isBusy = true;
            _waitInput = false;
            _isScriptEnded = false;
            _commandIndex = 0;
            _timer = 0;
            _drawObjects.Clear();
            _eventQueue.Clear();
            
            // スタイルリセット
            ResetStyle();
            
            // カーソル初期位置
            _cursorX = _windowRect.X;
            _cursorY = _windowRect.Y;
        }

        /// <summary>
        /// メッセージウィンドウを閉じます。
        /// </summary>
        public void Close()
        {
            _isActive = false;
            _isBusy = false;
        }

        /// <summary>
        /// メッセージ情報の破棄（クリア）を行います。
        /// コマンド、描画オブジェクト、イベントキューなどを全てリセットします。
        /// </summary>
        public void Clear()
        {
            Close();
            _commands?.Clear();
            _drawObjects.Clear();
            _eventQueue.Clear();
            _rawScript = "";
            _commandIndex = 0;
            _timer = 0;
            _waitTimer = 0;
            _forceNextWaitTimer = 0;
            _shakeTimer = 0;
            ResetStyle();
        }

        /// <summary>
        /// メッセージ表示中（または入力待ち）かどうかを返します。
        /// </summary>
        public bool IsBusy()
        {
            return _isActive && (_isBusy || _waitInput);
        }

        /// <summary>
        /// 発生したイベントIDを取得します。
        /// </summary>
        public string GetEvent()
        {
            if (_eventQueue.Count > 0) return _eventQueue.Dequeue();
            return null;
        }

        /// <summary>
        /// 次のメッセージへ進みます（入力トリガー）。
        /// </summary>
        public void Next()
        {
            if (!_isActive) return;

            if (_waitInput)
            {
                // 全てのコマンドを処理済みなら閉じる
                // または [end] タグで終了済みなら閉じる
                if (_commandIndex >= _commands.Count || _isScriptEnded)
                {
                    Close();
                    return;
                }

                // [next]待ちからの再開 = ページ送り
                // 表示内容をクリアしてカーソルをリセット
                _drawObjects.Clear();
                _cursorX = _windowRect.X;
                _cursorY = _windowRect.Y;

                _waitInput = false;
                _isBusy = true; // 処理再開

                // ページ先頭にある改行コマンド（スクリプト上の整形用改行など）をスキップする
                // これをしないと、[next]の直後の改行で1行目が空いてしまう
                while (_commandIndex < _commands.Count && _commands[_commandIndex].Type == CmdType.NewLine)
                {
                    _commandIndex++;
                }
            }
            else if (_isBusy)
            {
                // 文字送りスキップ
                Skip();
            }
            else
            {
                // 終了
                Close();
            }
        }

        private void Skip()
        {
            // 次の入力待ち(Next)または終了まで一気に処理を進める
            while (_isBusy && !_waitInput)
            {
                ProcessCommand(true); // 即時処理
            }
        }

        /// <summary>
        /// 更新処理。文字送りの制御や入力判定を行います。
        /// </summary>
        /// <param name="gameTime">GameTime</param>
        /// <param name="isInput">ページ送り/スキップ入力があったかどうか</param>
        public void Update(GameTime gameTime, bool isInput = false)
        {
            if (!_isActive) return;
            
            // 振動タイマー更新
            _shakeTimer += gameTime.ElapsedGameTime.TotalSeconds;

            // 強制ページ送り制御
            if (_forceNextWaitTimer > 0)
            {
                _forceNextWaitTimer -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if (_forceNextWaitTimer <= 0)
                {
                    _forceNextWaitTimer = 0;
                    Next();
                }
                return;
            }

            // 入力判定

            if (isInput && _isInputEnabled)
            {
                Next();
            }

            if (_waitInput)
            {
                // 入力待ち中のアイコン点滅などはDrawで処理
                return;
            }

            // ウェイト制御
            if (_waitTimer > 0)
            {
                _waitTimer -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if (_waitTimer <= 0) _waitTimer = 0;
                else return;
            }

            if (_isBusy)
            {
                _timer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                
                // 文字送り速度に従ってコマンドを処理
                // while条件変更: _timerチェックはループ内部で行う
                while (_isBusy && !_waitInput && _waitTimer <= 0 && _forceNextWaitTimer <= 0)
                {
                    // 範囲チェック
                    if (_commandIndex >= _commands.Count)
                    {
                        ProcessCommand(false); // 終了処理などを呼ぶために一応
                        break;
                    }

                    var nextCmd = _commands[_commandIndex];
                    if (IsConsumingCommand(nextCmd.Type))
                    {
                        // 消費コマンドの場合、タイマーチェック
                        if (_timer >= _charSpeed)
                        {
                            _timer -= _charSpeed;
                            ProcessCommand(false);
                            // まだ時間が余っていれば次へ
                        }
                        else
                        {
                            // 時間不足なので描画待ち
                            break;
                        }
                    }
                    else
                    {
                        // 非消費コマンド（設定変更など）は即時実行して時間消費しない
                        ProcessCommand(false);
                        // ループ継続
                    }
                }
            }
        }

        /// <summary>
        /// 1コマンド分の処理を実行します。
        /// </summary>
        /// <param name="instant">即時実行フラグ（スキップ時）</param>
        private void ProcessCommand(bool instant)
        {
            if (_commandIndex >= _commands.Count)
            {
                _isBusy = false;
                // 全て表示終了後、最後の入力待ち状態にする
                _waitInput = true;
                return;
            }

            var cmd = _commands[_commandIndex];
            _commandIndex++;

            switch (cmd.Type)
            {
                case CmdType.Text:
                    // 描画オブジェクト作成
                    // プロポーショナルフォント対応: 1文字ずつ幅を計測して配置
                    foreach (char c in cmd.StringValue)
                    {
                        string charStr = c.ToString();
                        
                        // 改行文字などはスキップ（本来コマンドで分離されているはずだが念のため）
                        if (c == '\n' || c == '\r') continue;

                        // 文字サイズの計測
                        Vector2 size = Ton.Gra.MeasureString(charStr, _currentFontId);
                        // スケール適用
                        float charWidth = size.X * _currentScale;
                        float charHeight = size.Y * _currentScale;

                        // 行の最大高さを更新
                        _currentLineMaxHeight = Math.Max(_currentLineMaxHeight, charHeight);

                        var obj = new DrawObject
                        {
                            Text = charStr,
                            X = (int)_cursorX,
                            Y = (int)_cursorY,
                            Color = _currentColor,
                            Scale = _currentScale,
                            Rotate = _currentRotate,
                            FontId = _currentFontId,
                            ShakeX = _currentShakeX,
                            ShakeY = _currentShakeY,
                            ShakeSpeed = _currentShakeSpeed,
                            Visible = true
                        };
                        _drawObjects.Add(obj);

                        // カーソルを進める (文字幅 + カーニングオフセット)
                        _cursorX += charWidth + _kerningOffset;
                    }
                    break;

                case CmdType.NewLine:
                    _cursorX = _windowRect.X; // X初期位置をリセット
                    
                    // 行送り (動的計算)
                    // 現在の行の最大高さが0の場合（空行など）、フォントの標準高さを使用
                    float lineHeight = _currentLineMaxHeight;
                    if (lineHeight <= 0)
                    {
                        // フォントIDに応じた標準高さを取得（暫定的に"A"で計測）
                        lineHeight = Ton.Gra.MeasureString("A", _currentFontId).Y * _currentScale;
                    }
                    
                    _cursorY += lineHeight + _lineSpacing;
                    _currentLineMaxHeight = 0f; // 次の行のためにリセット
                    
                    // 改行は一瞬で行うため、非即時モードでもウェイト消費なしとするか、1文字分待つか
                     if (!instant) _timer = 0; // 1文字分の時間を消費
                    break;

                case CmdType.SetSize:
                    _currentScale = cmd.IntValue / 24f; // 基準サイズ24pt
                    break;

                case CmdType.SetColor:
                    _currentColor = cmd.ColorValue;
                    break;

                case CmdType.SetSpeed:
                    // 安全策: 0以下は1msにする
                    int sp = cmd.IntValue;
                    if (sp <= 0) sp = 1;
                    _charSpeed = sp;
                    break;

                case CmdType.SetRotate:
                    _currentRotate = cmd.FloatValue;
                    break;

                case CmdType.SetShake:
                    _currentShakeX = cmd.ShakeX;
                    _currentShakeY = cmd.ShakeY;
                    _currentShakeSpeed = cmd.ShakeSpeed;
                    break;
                
                case CmdType.SetFont:
                    if (string.IsNullOrEmpty(cmd.StringValue))
                    {
                        // リセット(null)
                        _currentFontId = null;
                    }
                    else
                    {
                        // フォントが存在するか確認し、存在する場合のみ変更（なければ無視）
                        if (Ton.Gra.HasFont(cmd.StringValue))
                        {
                            _currentFontId = cmd.StringValue;
                        }
                    }
                    break;

                case CmdType.Reset:
                    ResetStyle(false); // inputタグの状態は維持する
                    break;

                case CmdType.Wait:
                    if (!instant) _waitTimer = cmd.IntValue;
                    break;
                
                case CmdType.Icon:
                    // テクスチャサイズを取得してカーソルを進める
                    var tex = Ton.Gra.LoadTexture(cmd.StringValue, cmd.StringValue);
                    int iconW = 40; // デフォルト
                    int iconH = 40;
                    if (tex != null)
                    {
                        iconW = tex.Width;
                        iconH = tex.Height;
                    }

                    // 行の最大高さを更新
                    _currentLineMaxHeight = Math.Max(_currentLineMaxHeight, iconH);

                    var icon = new DrawObject
                    {
                         IconName = cmd.StringValue,
                         X = (int)_cursorX, 
                         Y = (int)_cursorY,
                         Scale = 1.0f,
                         ShakeX = _currentShakeX,
                         ShakeY = _currentShakeY,
                         ShakeSpeed = _currentShakeSpeed,
                         Visible = true
                    };
                    _drawObjects.Add(icon);
                    _cursorX += iconW + _kerningOffset; // アイコン幅分進める（カーニング適用）
                    break;

                case CmdType.Next:
                    if (cmd.IntValue > 0)
                    {
                        // 時間指定がある場合は強制待機後に自動送り
                        _forceNextWaitTimer = cmd.IntValue;
                        // 強制待機中も入力をブロックするために待機フラグを立てておく
                        // ただしDrawでのアイコン表示区分けが必要なら別フラグにするが、今回は兼用する
                        _waitInput = true;
                    }
                    else
                    {
                        // 通常の入力待ち
                        _waitInput = true;
                    }
                    // ここで処理を中断し、次のUpdate/Next呼び出しを待つ
                    break;

                case CmdType.Event:
                    _eventQueue.Enqueue(cmd.StringValue);
                    break;

                case CmdType.End:
                    _isScriptEnded = true;
                    _waitInput = true;
                    break;

                case CmdType.Input:
                    _isInputEnabled = (cmd.IntValue != 0);
                    break;
            }
        }

        private void ResetStyle(bool fullReset = true)
        {
            _currentScale = _defaultScale;
            _currentColor = Color.White;
            _charSpeed = _defaultCharSpeed;
            _currentRotate = 0f;
            _currentFontId = null;
            _currentShakeX = 0f;
            _currentShakeY = 0f;

            _currentShakeSpeed = 20f;
            if (fullReset)
            {
                _isInputEnabled = true;
            }
        }

        /// <summary>
        /// メッセージの描画を行います。
        /// </summary>
        public void Draw()
        {
            if (!_isActive) return;

            // テキスト・アイコン描画
            var rng = new Random(); // 簡易ランダム (毎フレーム生成は非効率だが、振動のようなエフェクトなら許容範囲、本来はメンバー変数推奨)
            foreach (var obj in _drawObjects)
            {
                if (obj.Visible)
                {
                    float angleRad = MathHelper.ToRadians(obj.Rotate);

                    if (obj.Text != null)
                    {
                        float shakeX = 0, shakeY = 0;
                        if (obj.ShakeX > 0 || obj.ShakeY > 0)
                        {
                            float time = (float)_shakeTimer;
                            if (obj.ShakeX > 0) shakeX = (float)Math.Sin(time * obj.ShakeSpeed) * obj.ShakeX;
                            if (obj.ShakeY > 0) shakeY = (float)Math.Sin(time * obj.ShakeSpeed * 1.3f + 1.0f) * obj.ShakeY;
                        }

                        if (obj.Rotate == 0f)
                        {
                            Ton.Gra.DrawText(obj.Text, (int)(obj.X + shakeX), (int)(obj.Y + shakeY), obj.Color, obj.Scale, obj.FontId);
                        }
                        else
                        {
                            Ton.Gra.DrawTextEx(obj.Text, obj.X + shakeX, obj.Y + shakeY, obj.Color, obj.Scale, angleRad, obj.FontId);
                        }
                    }
                    else if (obj.IconName != null)
                    {
                        var tex = Ton.Gra.LoadTexture(obj.IconName, obj.IconName);
                        int w = (tex != null) ? tex.Width : 0;
                        int h = (tex != null) ? tex.Height : 0;
                        
                        float shakeX = 0, shakeY = 0;
                        if (obj.ShakeX > 0 || obj.ShakeY > 0)
                        {
                            float time = (float)_shakeTimer;
                            if (obj.ShakeX > 0) shakeX = (float)Math.Sin(time * obj.ShakeSpeed) * obj.ShakeX;
                            if (obj.ShakeY > 0) shakeY = (float)Math.Sin(time * obj.ShakeSpeed * 1.3f + 1.0f) * obj.ShakeY;
                        }

                        if (obj.Rotate == 0f)
                        {
                            Ton.Gra.Draw(obj.IconName, (int)(obj.X + shakeX), (int)(obj.Y + shakeY), 0, 0, w, h);
                        }
                        else
                        {
                            // DrawExはfloat座標を受け取る
                            var param = new TonDrawParamEx
                            {
                                Angle = angleRad,
                                ScaleX = 1.0f, // アイコンは基本スケール1.0 (obj.Scaleを使うならここ)
                                ScaleY = 1.0f,
                                Alpha = 1.0f
                            };
                            // DrawExは中心回転用に原点を設定するが、Drawは左上座標。
                            // DrawTextExと同様、左上指定座標+中心へのオフセットを渡す
                             Ton.Gra.DrawEx(obj.IconName, obj.X + w/2f + shakeX, obj.Y + h/2f + shakeY, 0, 0, w, h, param);
                        }
                    }
                }
            }
            
            // 入力待ちアイコン（点滅）
            // 強制ページ送り待機中は表示しない
            if (_waitInput && _forceNextWaitTimer <= 0)
            {
                 long blink = System.Environment.TickCount / 500;
                 if (blink % 2 == 0)
                 {
                     bool drawn = false;
                     // カスタムアイコンが設定されていれば画像を描画
                     if (!string.IsNullOrEmpty(_inputWaitingIconId))
                     {
                         var tex = Ton.Gra.LoadTexture(_inputWaitingIconId, _inputWaitingIconId);
                         // テクスチャがロードできた場合のみ描画
                         if (tex != null)
                         {
                             // ウィンドウの右下に配置
                             int x = _windowRect.Right - tex.Width;
                             int y = _windowRect.Bottom - tex.Height;
                             
                             // 描画 (色やアルファが必要な場合は適宜修正、ここでは白/不透明)
                             Ton.Gra.Draw(_inputWaitingIconId, x, y, 0, 0, tex.Width, tex.Height);
                             drawn = true;
                         }
                     }

                     // カスタムアイコンがない、またはロード失敗時はデフォルトの矩形を表示
                     if (!drawn)
                     {
                         Ton.Gra.FillRect(_windowRect.Right - 20, _windowRect.Bottom - 20, 20, 20, Color.White);
                     }
                 }
            }
        }

        private void ParseScript()
        {
            _commands = new List<MessageCommand>();
            
            // コメント行の削除 (;で始まる行)
            // 正規表現: 行頭の空白+セミコロン+改行まで
            _rawScript = Regex.Replace(_rawScript, @"^\s*;.*(\r\n|\r|\n)?", "", RegexOptions.Multiline);

            // 正規表現でタグを分離: \[ ( [^\]]+ ) \]
            // "Hello[wait:10]World" -> "Hello", "[wait:10]", "World"
            string pattern = @"(\[.*?\])";
            string[] parts = Regex.Split(_rawScript, pattern);

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (part.StartsWith("[") && part.EndsWith("]"))
                {
                    // タグ解析
                    string content = part.Substring(1, part.Length - 2);
                    ParseTag(content);
                }
                else
                {
                    // 通常テキスト（1文字ずつ分解）
                    foreach (char c in part)
                    {
                        if (c == '\n')
                        {
                            _commands.Add(new MessageCommand { Type = CmdType.NewLine });
                        }
                        else if (c == '\r')
                        {
                            // \r は無視
                        }
                        else
                        {
                            _commands.Add(new MessageCommand { Type = CmdType.Text, StringValue = c.ToString() });
                        }
                    }
                }
            }
            
            // 最後がNextでなければ強制的に追加（スクリプト終了時に閉じる挙動のため、あるいは入力待ちにするため）
            if (_commands.Count > 0 && _commands[_commands.Count-1].Type != CmdType.Next)
            {
                 _commands.Add(new MessageCommand { Type = CmdType.Next });
            }
        }

        /// <summary>
        /// 時間消費を伴う（文字送り待ちが発生する）コマンドかどうかを判定します。
        /// </summary>
        private bool IsConsumingCommand(CmdType type)
        {
            switch (type)
            {
                case CmdType.Text:
                case CmdType.Icon:
                case CmdType.NewLine:
                    return true;
                case CmdType.SetShake:
                case CmdType.Event:
                case CmdType.End:
                    return false;
                default:
                    return false;
            }
        }

        private void ParseTag(string tag)
        {
            // 例: "size:24", "wait:100", "n"
            string[] col = tag.Split(':');
            string key = col[0].ToLower().Trim();
            string val = (col.Length > 1) ? col[1].Trim() : "";

            switch (key)
            {
                case "input":
                    // input:disable / input:enable
                    if (val == "disable")
                    {
                        _commands.Add(new MessageCommand { Type = CmdType.Input, IntValue = 0 });
                    }
                    else
                    {
                        _commands.Add(new MessageCommand { Type = CmdType.Input, IntValue = 1 });
                    }
                    break;

                case "size":
                    if (int.TryParse(val, out int sz)) _commands.Add(new MessageCommand { Type = CmdType.SetSize, IntValue = sz });
                    break;
                case "color":
                     // 色指定（Reflectionで色名を取得）
                     Color c = Color.White;
                     var prop = typeof(Color).GetProperty(val, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                     if (prop != null) c = (Color)prop.GetValue(null);
                     _commands.Add(new MessageCommand { Type = CmdType.SetColor, ColorValue = c });
                    break;
                case "speed":
                    if (int.TryParse(val, out int sp)) _commands.Add(new MessageCommand { Type = CmdType.SetSpeed, IntValue = sp });
                    break;
                case "rotate":
                    if (float.TryParse(val, out float rot)) _commands.Add(new MessageCommand { Type = CmdType.SetRotate, FloatValue = rot });
                    break;
                case "shake":
                    // shake:val or shake:x,y or shake:x,y,speed
                    {
                        float sx = 0, sy = 0, shakeSpeed = 20f;
                        if (val.Contains(","))
                        {
                            string[] vals = val.Split(',');
                            if (vals.Length > 0) float.TryParse(vals[0], out sx);
                            if (vals.Length > 1) float.TryParse(vals[1], out sy);
                            if (vals.Length > 2) float.TryParse(vals[2], out shakeSpeed);
                        }
                        else
                        {
                            if (float.TryParse(val, out float s))
                            {
                                sx = s;
                                sy = s;
                            }
                        }
                        _commands.Add(new MessageCommand { Type = CmdType.SetShake, ShakeX = sx, ShakeY = sy, ShakeSpeed = shakeSpeed });
                    }
                    break;
                case "reset":
                    _commands.Add(new MessageCommand { Type = CmdType.Reset });
                    break;
                case "wait":
                    if (int.TryParse(val, out int w)) _commands.Add(new MessageCommand { Type = CmdType.Wait, IntValue = w });
                    break;
                case "icon":
                    _commands.Add(new MessageCommand { Type = CmdType.Icon, StringValue = val });
                    break;
                case "next":
                    int nextWait = 0;
                    if (!string.IsNullOrEmpty(val)) int.TryParse(val, out nextWait);
                    _commands.Add(new MessageCommand { Type = CmdType.Next, IntValue = nextWait });
                    break;
                case "event":
                    _commands.Add(new MessageCommand { Type = CmdType.Event, StringValue = val });
                    break;
                case "font":
                    _commands.Add(new MessageCommand { Type = CmdType.SetFont, StringValue = val });
                    break;
                case "end":
                    _commands.Add(new MessageCommand { Type = CmdType.End });
                    break;
            }
        }
    }
}
