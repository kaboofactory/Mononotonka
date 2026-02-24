# mononotonka manual gap matrix

## 1. Phase 1 実装APIインベントリ（manual突合前）
- 対象: mononotonka/*.cs
- 抽出方針: public 型と、その型直下の公開メンバー（interface/enumの暗黙publicを含む）を列挙
- 用途: Phase 3 で manual と突合するための実装基準データ

### 1.1 型サマリー
| 型 | 種別 | ソース | 公開メンバー数 | 前提条件（Phase1暫定） | 副作用（Phase1暫定） |
| --- | --- | --- | ---: | --- | --- |
| Ton | class | mononotonka/Ton.cs:11 | 37 | Ton.Instance.Initialize(Game, GraphicsDeviceManager) を先に1回実行し、以後はゲームループから Update/Draw を呼ぶこと | サブシステムの生成・更新・描画・終了を一括制御し、内部状態を更新する |
| CharacterAnimType | enum | mononotonka/TonCharacter.cs:10 | 10 | 特になし（値定義/データ構造） | なし（値/データ保持のみ） |
| TonCharacter | class | mononotonka/TonCharacter.cs:37 | 18 | キャラクターIDを AddCharacter で登録後に各操作APIを呼ぶこと。Update/Draw を毎フレーム呼ぶこと | キャラクター辞書・物理状態・アニメ状態を更新し描画する |
| TonCharacter.AnimConfig | struct | mononotonka/TonCharacter.cs:154 | 7 | 特になし（値定義/データ構造） | なし（値/データ保持のみ） |
| TonConfigMenu | class | mononotonka/TonConfigMenu.cs:11 | 6 | Initialize 後に Open/Update/Draw を利用すること | 設定UI状態を変更し、必要に応じて設定値を保存/反映する |
| TonConfigMenu.ConfigData | class | mononotonka/TonConfigMenu.cs:29 | 5 | 特になし（設定/状態データとして利用） | 設定/状態値を保持する（呼び出し側で参照・更新） |
| TonGame | class | mononotonka/TonGame.cs:11 | 31 | Initialize 後に利用すること。画面サイズ変更系は有効な解像度を指定すること | ウィンドウ状態・解像度・フルスクリーン状態を変更し、場合によりゲーム終了を実行する |
| TonGameData | class | mononotonka/TonGameData.cs:14 | 18 | 特になし（ゲーム状態コンテナとして使用） | ゲーム進行データ（HP/Flags/Vars 等）を保持・更新する |
| TonGraphics | class | mononotonka/TonGraphics.cs:15 | 50 | Initialize 後に利用すること。描画系は Begin/End の整合を保つこと。テクスチャ名は事前に LoadTexture 済みであること | GPU描画状態、レンダーターゲット、テクスチャキャッシュを変更し、描画結果を出力する |
| AnimDirection | enum | mononotonka/TonGraphicsDef.cs:8 | 2 | 特になし（値定義/データ構造） | なし（値/データ保持のみ） |
| ScreenFilterType | enum | mononotonka/TonGraphicsDef.cs:18 | 15 | 特になし（値定義/データ構造） | なし（値/データ保持のみ） |
| TonAnimState | class | mononotonka/TonGraphicsDef.cs:56 | 16 | 特になし（設定/状態データとして利用） | 設定/状態値を保持する（呼び出し側で参照・更新） |
| TonDrawParam | class | mononotonka/TonGraphicsDef.cs:187 | 6 | 特になし（設定/状態データとして利用） | 設定/状態値を保持する（呼び出し側で参照・更新） |
| TonDrawParamEx | class | mononotonka/TonGraphicsDef.cs:206 | 12 | 特になし（設定/状態データとして利用） | 設定/状態値を保持する（呼び出し側で参照・更新） |
| TonFilterParam | class | mononotonka/TonGraphicsDef.cs:234 | 3 | 特になし（設定/状態データとして利用） | 設定/状態値を保持する（呼び出し側で参照・更新） |
| TonBlendState | class | mononotonka/TonGraphicsDef.cs:251 | 6 | 特になし（設定/状態データとして利用） | 設定/状態値を保持する（呼び出し側で参照・更新） |
| MouseButton | enum | mononotonka/TonInput.cs:15 | 5 | 特になし（値定義/データ構造） | なし（値/データ保持のみ） |
| TonInput | class | mononotonka/TonInput.cs:24 | 18 | Initialize 後に利用すること。名前入力は RegisterButton 済みのボタン名を使うこと | キーボード/パッド/マウス状態を保持更新し、入力消費フラグを変更する |
| TonLog | class | mononotonka/TonLog.cs:12 | 7 | 利用開始時に出力先が有効であること | ログ出力先へ書き込み、内部の最終ログ状態を更新する |
| MagicLevel | enum | mononotonka/TonMagicEffect.cs:10 | 4 | 特になし（値定義/データ構造） | なし（値/データ保持のみ） |
| TonMagicEffectParam | class | mononotonka/TonMagicEffect.cs:25 | 3 | 特になし（設定/状態データとして利用） | 設定/状態値を保持する（呼び出し側で参照・更新） |
| TonMagicEffect | class | mononotonka/TonMagicEffect.cs:39 | 17 | 使用前に初期化/登録系セットアップを完了すること（詳細は Phase 2 で確認） | エフェクト状態（寿命・位置・描画情報）を更新し描画する |
| TonMath | class | mononotonka/TonMath.cs:9 | 8 | 特になし（純粋計算ユーティリティ） | なし（計算結果を返すのみ） |
| TonMenu | class | mononotonka/TonMenu.cs:12 | 44 | Items 追加後に Control/Draw を呼ぶこと。入力は呼び出し側で供給すること | カーソル・選択状態を更新し、決定/キャンセルイベントを発火する |
| TonMenu.InputType | enum | mononotonka/TonMenu.cs:18 | 6 | 特になし（値定義/データ構造） | なし（値/データ保持のみ） |
| TonMenuItem | class | mononotonka/TonMenu.cs:549 | 8 | 特になし（引数妥当性は呼び出し側で担保） | 公開メンバーに応じて内部状態を変更する可能性がある（詳細は Phase 2 で確認） |
| TonMenuElement | class | mononotonka/TonMenu.cs:603 | 1 | 特になし（引数妥当性は呼び出し側で担保） | 公開メンバーに応じて内部状態を変更する可能性がある（詳細は Phase 2 で確認） |
| ElementAlignment | enum | mononotonka/TonMenu.cs:616 | 0 | 特になし（値定義/データ構造） | なし（値/データ保持のみ） |
| TonMenuText | class | mononotonka/TonMenu.cs:622 | 8 | 特になし（引数妥当性は呼び出し側で担保） | 公開メンバーに応じて内部状態を変更する可能性がある（詳細は Phase 2 で確認） |
| TonMenuIcon | class | mononotonka/TonMenu.cs:700 | 5 | 特になし（引数妥当性は呼び出し側で担保） | 公開メンバーに応じて内部状態を変更する可能性がある（詳細は Phase 2 で確認） |
| TonMenuPanel | class | mononotonka/TonMenu.cs:748 | 3 | 特になし（引数妥当性は呼び出し側で担保） | 公開メンバーに応じて内部状態を変更する可能性がある（詳細は Phase 2 で確認） |
| TonMenuPanel.LayoutType | enum | mononotonka/TonMenu.cs:751 | 3 | 特になし（値定義/データ構造） | なし（値/データ保持のみ） |
| TonMenuManager | class | mononotonka/TonMenuManager.cs:11 | 7 | Push でメニューを積んだ状態で Update/Draw を呼ぶこと | メニュースタックを更新し、Push/Pop時にコールバックを実行する |
| TonMessage | class | mononotonka/TonMessage.cs:13 | 14 | Initialize 後に利用すること。Show/LoadScript 後に Update/Draw を継続呼び出しすること | メッセージ進行状態、イベント通知、表示内容を更新し描画に反映する |
| TonParticleParam | class | mononotonka/TonParticle.cs:12 | 20 | 特になし（設定/状態データとして利用） | 設定/状態値を保持する（呼び出し側で参照・更新） |
| TonParticle | class | mononotonka/TonParticle.cs:60 | 6 | Register でパラメータ登録後に Play を呼ぶこと。Update/Draw を毎フレーム呼ぶこと | パーティクルの生成/更新/破棄を行い描画する |
| TonPrimitive | class | mononotonka/TonPrimitive.cs:12 | 18 | Initialize 後に描画APIを呼ぶこと | プリミティブ描画コマンドを発行し描画結果を出力する |
| TonSaveLoadMode | enum | mononotonka/TonSaveLoadMenu.cs:12 | 4 | 特になし（値定義/データ構造） | なし（値/データ保持のみ） |
| TonSaveLoadMenu | class | mononotonka/TonSaveLoadMenu.cs:28 | 7 | Ton.Storage/Ton.Data が初期化済みであること。Open 後は Update/Draw を呼ぶこと | セーブ/ロードUI状態を変更し、ロード時に Ton.Data 等へ反映する |
| IScene | interface | mononotonka/TonScene.cs:10 | 4 | 実装クラス側でライフサイクル契約を満たすこと | なし（契約定義のみ） |
| TonScene | class | mononotonka/TonScene.cs:22 | 6 | Initialize 後に利用すること。Change へ渡す IScene 実装は契約メソッドを実装済みであること | シーン遷移状態を更新し、必要に応じてシーンの Initialize/Terminate を呼ぶ |
| TonSceneTemplete | class | mononotonka/TonSceneTemplate.cs:12 | 4 | IScene 契約（Initialize/Terminate/Update/Draw）に従って使用すること | 実装内容に応じてゲーム状態と描画結果を変更する |
| TonSound | class | mononotonka/TonSound.cs:14 | 21 | Initialize 後に利用すること。再生対象は LoadSound/LoadBGM 済みであること | 音声リソースのロード/解放、BGM/SE再生・停止、音量・フェード状態を変更する |
| TonSpec | class | mononotonka/TonSpec.cs:11 | 1 | Windows API 呼び出し可能環境であること | システム情報を取得しログ出力する |
| TonStorage | class | mononotonka/TonStorage.cs:11 | 4 | 保存対象がシリアライズ可能であり、ファイル名が有効であること | セーブデータのファイルI/O（保存・読込）を行う |

## 2. 型別公開API一覧

### Ton
- 種別: class
- ソース: mononotonka/Ton.cs:11
- 前提条件（Phase1暫定）: Ton.Instance.Initialize(Game, GraphicsDeviceManager) を先に1回実行し、以後はゲームループから Update/Draw を呼ぶこと
- 副作用（Phase1暫定）: サブシステムの生成・更新・描画・終了を一括制御し、内部状態を更新する
- 公開メンバー（シグネチャ）:
  - L17: public static Ton Instance => _instance ??= new Ton();
  - L20: public static TonLog Log => Instance.log;
  - L21: public static TonGame Game => Instance.game;
  - L22: public static TonInput Input => Instance.input;
  - L23: public static TonGraphics Gra => Instance.gra;
  - L24: public static TonSound Sound => Instance.sound;
  - L25: public static TonMessage Msg => Instance.msg;
  - L26: public static TonCharacter Character => Instance.character;
  - L27: public static TonConfigMenu ConfigMenu => Instance.configmenu;
  - L28: public static TonScene Scene => Instance.scene;
  - L29: public static TonMath Math => Instance.math;
  - L30: public static TonStorage Storage => Instance.storage;
  - L31: public static TonParticle Particle => Instance.particle;
  - L32: public static TonSaveLoadMenu SaveLoadMenu => Instance.saveload;
  - L33: public static TonPrimitive Primitive => Instance.primitive;
  - L35: public static TonMagicEffect Magic => Instance.magic;
  - L37: public static TonGameData Data => Instance.gamedata;
  - L41: public TonLog log { get; private set; }
  - L43: public TonGame game { get; private set; }
  - L45: public TonInput input { get; private set; }
  - L47: public TonGraphics gra { get; private set; }
  - L49: public TonSound sound { get; private set; }
  - L51: public TonMessage msg { get; private set; }
  - L53: public TonCharacter character { get; private set; }
  - L55: public TonConfigMenu configmenu { get; private set; }
  - L57: public TonSaveLoadMenu saveload { get; private set; }
  - L59: public TonGameData gamedata { get; internal set; }
  - L61: public TonScene scene { get; private set; }
  - L65: public TonMath math { get; private set; }
  - L69: public TonStorage storage { get; private set; }
  - L71: public TonParticle particle { get; private set; }
  - L73: public TonPrimitive primitive { get; private set; }
  - L75: public TonMagicEffect magic { get; private set; }
  - L104: public void Initialize(Game game, GraphicsDeviceManager graphics)
  - L127: public void Update(GameTime gameTime)
  - L155: public void Draw(GameTime gameTime)
  - L186: public void Terminate()

### CharacterAnimType
- 種別: enum
- ソース: mononotonka/TonCharacter.cs:10
- 前提条件（Phase1暫定）: 特になし（値定義/データ構造）
- 副作用（Phase1暫定）: なし（値/データ保持のみ）
- 公開メンバー（シグネチャ）:
  - L12: (enum) Idle,
  - L14: (enum) Walk,
  - L16: (enum) Run,
  - L18: (enum) Jump,
  - L20: (enum) Surprise,
  - L22: (enum) Panic,
  - L24: (enum) Happy,
  - L26: (enum) Damage,
  - L28: (enum) Die,
  - L30: (enum) UserDefine

### TonCharacter
- 種別: class
- ソース: mononotonka/TonCharacter.cs:37
- 前提条件（Phase1暫定）: キャラクターIDを AddCharacter で登録後に各操作APIを呼ぶこと。Update/Draw を毎フレーム呼ぶこと
- 副作用（Phase1暫定）: キャラクター辞書・物理状態・アニメ状態を更新し描画する
- 公開メンバー（シグネチャ）:
  - L109: public void AddCharacter(string id, int x, int y, float gravity = 1800.0f)
  - L126: public void RemoveCharacter(string id = null)
  - L180: public void AddAnim(string id, CharacterAnimType type, AnimConfig config)
  - L203: public void AddAnim(string id, CharacterAnimType type, string imageName, int x1, int y1, int w, int h, int frameCount, int duration, bool isLoop = true)
  - L215: public void SetPhysics(string id, bool useGravity, bool checkGround, float friction)
  - L229: public void SetVelocity(string id, float vx, float vy)
  - L247: public void SetScale(string id, float scale)
  - L264: public void MoveTo(string id, int targetX, float speed, CharacterAnimType moveAnim = CharacterAnimType.Walk)
  - L287: public void RoundTrip(string id, int distance, float speed, CharacterAnimType moveAnim = CharacterAnimType.Walk)
  - L310: public void PlayAction(string id, CharacterAnimType type)
  - L326: public void Stop(string id)
  - L349: public void SetClipping(string id, bool enable, int minX = 0, int maxX = 0)
  - L376: public void Jump(string id, float vx, float vy)
  - L393: public bool IsMoving(string id)
  - L402: public bool IsOnGround(string id)
  - L422: public Vector2 GetPos(string id)
  - L432: public void Update(GameTime gameTime)
  - L628: public void Draw()

### TonCharacter.AnimConfig
- 種別: struct
- ソース: mononotonka/TonCharacter.cs:154
- 前提条件（Phase1暫定）: 特になし（値定義/データ構造）
- 副作用（Phase1暫定）: なし（値/データ保持のみ）
- 公開メンバー（シグネチャ）:
  - L156: public string ImageName;
  - L157: public int X, Y;
  - L158: public int Width, Height;
  - L159: public int FrameCount;
  - L160: public int Duration;
  - L161: public bool IsLoop;
  - L163: public AnimConfig(string imageName, int x, int y, int w, int h, int frameCount, int duration, bool isLoop = true)

### TonConfigMenu
- 種別: class
- ソース: mononotonka/TonConfigMenu.cs:11
- 前提条件（Phase1暫定）: Initialize 後に Open/Update/Draw を利用すること
- 副作用（Phase1暫定）: 設定UI状態を変更し、必要に応じて設定値を保存/反映する
- 公開メンバー（シグネチャ）:
  - L38: public void Initialize()
  - L73: public void Open()
  - L93: public void Close()
  - L121: public bool IsOpen()
  - L129: public void Update()
  - L253: public void Draw()

### TonConfigMenu.ConfigData
- 種別: class
- ソース: mononotonka/TonConfigMenu.cs:29
- 前提条件（Phase1暫定）: 特になし（設定/状態データとして利用）
- 副作用（Phase1暫定）: 設定/状態値を保持する（呼び出し側で参照・更新）
- 公開メンバー（シグネチャ）:
  - L31: public int Width { get; set; }
  - L32: public int Height { get; set; }
  - L33: public bool IsFullScreen { get; set; }
  - L34: public float MasterVolume { get; set; }
  - L35: public int MsgSpeed { get; set; }

### TonGame
- 種別: class
- ソース: mononotonka/TonGame.cs:11
- 前提条件（Phase1暫定）: Initialize 後に利用すること。画面サイズ変更系は有効な解像度を指定すること
- 副作用（Phase1暫定）: ウィンドウ状態・解像度・フルスクリーン状態を変更し、場合によりゲーム終了を実行する
- 公開メンバー（シグネチャ）:
  - L17: public int VirtualWidth { get; private set; } = 1280;
  - L19: public int VirtualHeight { get; private set; } = 720;
  - L22: public int WindowWidth => _graphics.PreferredBackBufferWidth;
  - L24: public int WindowHeight => _graphics.PreferredBackBufferHeight;
  - L27: public TimeSpan TotalGameTime { get; private set; }
  - L30: public Microsoft.Xna.Framework.Content.ContentManager Content => _game.Content;
  - L46: public bool IsFullScreen => _graphics.IsFullScreen;
  - L51: public float UpdateFPS => _currentUpdateFps;
  - L56: public float DrawFPS => _currentDrawFps;
  - L63: public void Initialize(Game game, GraphicsDeviceManager graphics)
  - L110: public void Update(GameTime gameTime)
  - L129: public void Draw(GameTime gameTime)
  - L146: public int GetVirtualWidth()
  - L155: public int GetVirtualHeight()
  - L164: public int GetWindowWidth()
  - L173: public int GetWindowHeight()
  - L182: public TimeSpan GetTotalGameTime()
  - L191: public bool GetIsFullScreen()
  - L200: public float GetUpdateFPS()
  - L209: public float GetDrawFPS()
  - L217: public System.Collections.Generic.List<Point> GetAvailableResolutions()
  - L243: public void SetWindowSize(int width, int height)
  - L255: public void CenterWindow()
  - L270: public void SetResizable(bool enable)
  - L280: public void SetVirtualResolution(int width, int height)
  - L291: public void ToggleFullScreen(bool isFullscreen)
  - L305: public void SetWindowTitle(string title)
  - L314: public void SetMouseVisible(bool visible)
  - L322: public void Exit()
  - L331: public Rectangle GetScreenDestinationRect()
  - L362: public Vector2 ConvertWindowToVirtual(Vector2 windowPos)

### TonGameData
- 種別: class
- ソース: mononotonka/TonGameData.cs:14
- 前提条件（Phase1暫定）: 特になし（ゲーム状態コンテナとして使用）
- 副作用（Phase1暫定）: ゲーム進行データ（HP/Flags/Vars 等）を保持・更新する
- 公開メンバー（シグネチャ）:
  - L19: public int HP { get; set; } = 100;
  - L20: public int MaxHP { get; set; } = 100;
  - L21: public int Level { get; set; } = 1;
  - L22: public int Exp { get; set; } = 0;
  - L23: public int Money { get; set; } = 0;
  - L26: public float PlayerX { get; set; }
  - L27: public float PlayerY { get; set; }
  - L28: public string CurrentSceneName { get; set; }
  - L31: public HashSet<string> Flags { get; set; } = new HashSet<string>();
  - L33: public Dictionary<string, int> Vars { get; set; } = new Dictionary<string, int>();
  - L43: public object TempCacheData { get; set; }
  - L49: public void SetFlag(string flagName) => Flags.Add(flagName);
  - L50: public void RemoveFlag(string flagName) => Flags.Remove(flagName);
  - L51: public bool CheckFlag(string flagName) => Flags.Contains(flagName);
  - L53: public void SetVar(string name, int val) => Vars[name] = val;
  - L54: public int GetVar(string name) => Vars.ContainsKey(name) ? Vars[name] : 0;
  - L60: public void BeforeSave()
  - L68: public void AfterLoad()

### TonGraphics
- 種別: class
- ソース: mononotonka/TonGraphics.cs:15
- 前提条件（Phase1暫定）: Initialize 後に利用すること。描画系は Begin/End の整合を保つこと。テクスチャ名は事前に LoadTexture 済みであること
- 副作用（Phase1暫定）: GPU描画状態、レンダーターゲット、テクスチャキャッシュを変更し、描画結果を出力する
- 公開メンバー（シグネチャ）:
  - L53: public long TotalTextureMemory => _totalTextureMemory;
  - L58: public long MaxTextureMemory => _maxTextureMemory;
  - L115: public void Initialize(Game game, GraphicsDeviceManager graphics)
  - L164: public void Terminate()
  - L199: public void SetCacheTimeout(double seconds)
  - L207: public void Update(GameTime gameTime)
  - L278: public Texture2D LoadTexture(string path, string name, string contentId = "Default")
  - L361: public void Unload(string contentId)
  - L427: public int GetTextureWidth(string name)
  - L437: public int GetTextureHeight(string name)
  - L447: public void SetRenderTarget(string targetName = null)
  - L470: public void CreateRenderTarget(string targetName, int width, int height)
  - L490: public void ReleaseRenderTarget(string targetName)
  - L512: public void SetAntiAliasing(bool enabled)
  - L521: public TonBlendState GetBlendState()
  - L530: public void Clear(Color? color = null)
  - L539: public void Begin()
  - L566: public void SetBlendState(TonBlendState blend)
  - L585: public void End()
  - L671: public void SuspendBatch()
  - L680: public void ResumeBatch()
  - L840: public void Draw(string imageName, int toX, int toY, TonDrawParam param = null)
  - L859: public void Draw(string imageName, int toX, int toY, int fromX, int fromY, int w, int h, TonDrawParam param = null)
  - L898: public void DrawBackground(string imageName, TonDrawParam param = null)
  - L946: public void DrawEx(string imageName, float toX, float toY, int fromX, int fromY, int w, int h, TonDrawParamEx param)
  - L982: public void DrawAnim(string imageName, int x, int y, TonAnimState anim, TonDrawParam param = null)
  - L1023: public void DrawAnimEx(string imageName, float x, float y, TonAnimState anim, TonDrawParamEx param)
  - L1059: public void LoadFont(string path, string name)
  - L1095: public Vector2 MeasureString(string text, string fontId = null)
  - L1114: public bool HasFont(string name)
  - L1128: public void DrawText(string text, int x, int y, Color? color = null, float scale = 1.0f, string fontId = null)
  - L1155: public void DrawText(string text, int x, int y)
  - L1163: public void DrawText(string text, int x, int y, string fontId)
  - L1171: public void DrawText(string text, int x, int y, float scale)
  - L1179: public void DrawText(string text, int x, int y, string fontId, float scale)
  - L1187: public void DrawText(string text, int x, int y, string fontId, Color color)
  - L1203: public void DrawTextEx(string text, float x, float y, Color? color = null, float scale = 1.0f, float rotation = 0f, string fontId = null)
  - L1246: public void DebugSaveFontTexture(string savePath, string id = null)
  - L1323: public void FillRect(int x, int y, int w, int h, Color color)
  - L1337: public void DrawRect(int x, int y, int w, int h, Color color, int thickness = 1)
  - L1352: public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
  - L1364: public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1f)
  - L1379: public void FillRoundedRect(string imageName, int x, int y, int w, int h, int pw, int ph)
  - L1426: public void ShakeScreen(float seconds, float ratioX, float ratioY, float frequency = 20.0f)
  - L1441: public void SetScreenFilter(TonFilterParam param)
  - L1445: public void SetScreenFilter(string targetName, TonFilterParam param)
  - L1457: public void AddScreenFilter(TonFilterParam param)
  - L1461: public void AddScreenFilter(string targetName, TonFilterParam param)
  - L1481: public void ClearScreenFilter(string targetName = null)
  - L1500: public void DebugForceExpireCache(string contentId)

### AnimDirection
- 種別: enum
- ソース: mononotonka/TonGraphicsDef.cs:8
- 前提条件（Phase1暫定）: 特になし（値定義/データ構造）
- 副作用（Phase1暫定）: なし（値/データ保持のみ）
- 公開メンバー（シグネチャ）:
  - L10: (enum) LeftToRight,
  - L12: (enum) TopToBottom

### ScreenFilterType
- 種別: enum
- ソース: mononotonka/TonGraphicsDef.cs:18
- 前提条件（Phase1暫定）: 特になし（値定義/データ構造）
- 副作用（Phase1暫定）: なし（値/データ保持のみ）
- 公開メンバー（シグネチャ）:
  - L21: (enum) None,
  - L23: (enum) Greyscale,
  - L25: (enum) Sepia,
  - L27: (enum) ScanLine,
  - L29: (enum) Mosaic,
  - L31: (enum) Blur,
  - L33: (enum) ChromaticAberration,
  - L35: (enum) Vignette,
  - L37: (enum) Invert,
  - L39: (enum) Distortion,
  - L41: (enum) Noise,
  - L43: (enum) EdgeDetect,
  - L45: (enum) RadialBlur,
  - L47: (enum) Posterize,
  - L49: (enum) FishEye

### TonAnimState
- 種別: class
- ソース: mononotonka/TonGraphicsDef.cs:56
- 前提条件（Phase1暫定）: 特になし（設定/状態データとして利用）
- 副作用（Phase1暫定）: 設定/状態値を保持する（呼び出し側で参照・更新）
- 公開メンバー（シグネチャ）:
  - L59: public bool IsLoop = true;
  - L61: public int FrameCount = 1;
  - L63: public int FrameDuration = 100; // ms
  - L65: public AnimDirection direction = AnimDirection.LeftToRight;
  - L69: public float Timer { get; private set; } = 0f;
  - L71: public int CurrentFrame { get; private set; } = 0;
  - L73: public float TimeAfterFinished { get; private set; } = 0f;
  - L76: public int x1;
  - L78: public int y1;
  - L80: public int width;
  - L82: public int height;
  - L87: public bool IsFinished => TimeAfterFinished > 0;
  - L92: public void Reset()
  - L103: public static TonAnimState CreateLoop(int x1, int y1, int width, int height, int frameCount, int durationMs, double totalSeconds)
  - L129: public Rectangle GetSourceRect()
  - L140: public void Update(GameTime gameTime)

### TonDrawParam
- 種別: class
- ソース: mononotonka/TonGraphicsDef.cs:187
- 前提条件（Phase1暫定）: 特になし（設定/状態データとして利用）
- 副作用（Phase1暫定）: 設定/状態値を保持する（呼び出し側で参照・更新）
- 公開メンバー（シグネチャ）:
  - L190: public float Alpha = 1.0f;
  - L192: public Color Color = Color.White;
  - L194: public bool FlipH = false;
  - L196: public bool FlipV = false;
  - L198: public TonDrawParam() { }
  - L199: public TonDrawParam(Color color) { Color = color; }

### TonDrawParamEx
- 種別: class
- ソース: mononotonka/TonGraphicsDef.cs:206
- 前提条件（Phase1暫定）: 特になし（設定/状態データとして利用）
- 副作用（Phase1暫定）: 設定/状態値を保持する（呼び出し側で参照・更新）
- 公開メンバー（シグネチャ）:
  - L209: public float ScaleX = 1.0f;
  - L211: public float ScaleY = 1.0f;
  - L213: public float Angle = 0.0f;
  - L215: public float Alpha = 1.0f;
  - L217: public Color Color = Color.White;
  - L219: public bool FlipH = false;
  - L221: public bool FlipV = false;
  - L223: public float MosaicSize = 0.0f;
  - L225: public TonDrawParamEx() { }
  - L226: public TonDrawParamEx(float scale) { ScaleX = scale; ScaleY = scale; }
  - L227: public TonDrawParamEx(float scale, float angle) { ScaleX = scale; ScaleY = scale; Angle = angle; }
  - L228: public TonDrawParamEx(float scale, float angle, Color color) { ScaleX = scale; ScaleY = scale; Angle = angle; Color = color; }

### TonFilterParam
- 種別: class
- ソース: mononotonka/TonGraphicsDef.cs:234
- 前提条件（Phase1暫定）: 特になし（設定/状態データとして利用）
- 副作用（Phase1暫定）: 設定/状態値を保持する（呼び出し側で参照・更新）
- 公開メンバー（シグネチャ）:
  - L237: public ScreenFilterType Type;
  - L239: public float Amount;
  - L241: public TonFilterParam(ScreenFilterType type, float amount = 1.0f)

### TonBlendState
- 種別: class
- ソース: mononotonka/TonGraphicsDef.cs:251
- 前提条件（Phase1暫定）: 特になし（設定/状態データとして利用）
- 副作用（Phase1暫定）: 設定/状態値を保持する（呼び出し側で参照・更新）
- 公開メンバー（シグネチャ）:
  - L253: public Microsoft.Xna.Framework.Graphics.BlendState State { get; private set; }
  - L255: public TonBlendState(Microsoft.Xna.Framework.Graphics.BlendState state)
  - L261: public static readonly TonBlendState AlphaBlend = new TonBlendState(Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend);
  - L263: public static readonly TonBlendState Additive = new TonBlendState(Microsoft.Xna.Framework.Graphics.BlendState.Additive);
  - L265: public static readonly TonBlendState NonPremultiplied = new TonBlendState(Microsoft.Xna.Framework.Graphics.BlendState.NonPremultiplied);
  - L267: public static readonly TonBlendState Opaque = new TonBlendState(Microsoft.Xna.Framework.Graphics.BlendState.Opaque);

### MouseButton
- 種別: enum
- ソース: mononotonka/TonInput.cs:15
- 前提条件（Phase1暫定）: 特になし（値定義/データ構造）
- 副作用（Phase1暫定）: なし（値/データ保持のみ）
- 公開メンバー（シグネチャ）:
  - L17: (enum) Left,
  - L18: (enum) Right,
  - L19: (enum) Middle,
  - L20: (enum) XButton1,
  - L21: (enum) XButton2

### TonInput
- 種別: class
- ソース: mononotonka/TonInput.cs:24
- 前提条件（Phase1暫定）: Initialize 後に利用すること。名前入力は RegisterButton 済みのボタン名を使うこと
- 副作用（Phase1暫定）: キーボード/パッド/マウス状態を保持更新し、入力消費フラグを変更する
- 公開メンバー（シグネチャ）:
  - L47: public TonInput()
  - L74: public void Initialize()
  - L128: public void RegisterButton(string name, Keys key, Buttons btn)
  - L148: public void Update(GameTime gameTime)
  - L172: public void Update()
  - L200: public void ConsumeInput()
  - L208: public bool IsPressed(string buttonName)
  - L228: public bool IsJustPressed(string buttonName)
  - L248: public bool IsJustReleased(string buttonName)
  - L269: public double GetPressedDuration(string buttonName)
  - L283: public void ClearPressedDuration(string buttonName)
  - L294: public Vector2 GetVector()
  - L314: public Vector2 GetMousePosition()
  - L326: public void Vibrate(float seconds, float motor)
  - L330: public void Vibrate(float seconds, float motorLeft, float motorRight)
  - L375: public bool IsMousePressed(MouseButton button)
  - L384: public bool IsMouseJustPressed(MouseButton button)
  - L394: public bool IsMouseJustReleased(MouseButton button)

### TonLog
- 種別: class
- ソース: mononotonka/TonLog.cs:12
- 前提条件（Phase1暫定）: 利用開始時に出力先が有効であること
- 副作用（Phase1暫定）: ログ出力先へ書き込み、内部の最終ログ状態を更新する
- 公開メンバー（シグネチャ）:
  - L23: public string LastLog { get; private set; } = "";
  - L28: public TonLog()
  - L127: public void Close()
  - L151: public void Info(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
  - L162: public void Warning(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
  - L173: public void Error(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
  - L184: public void Debug(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)

### MagicLevel
- 種別: enum
- ソース: mononotonka/TonMagicEffect.cs:10
- 前提条件（Phase1暫定）: 特になし（値定義/データ構造）
- 副作用（Phase1暫定）: なし（値/データ保持のみ）
- 公開メンバー（シグネチャ）:
  - L13: (enum) Level1 = 1,
  - L15: (enum) Level2 = 2,
  - L17: (enum) Level3 = 3,
  - L19: (enum) Level4 = 4

### TonMagicEffectParam
- 種別: class
- ソース: mononotonka/TonMagicEffect.cs:25
- 前提条件（Phase1暫定）: 特になし（設定/状態データとして利用）
- 副作用（Phase1暫定）: 設定/状態値を保持する（呼び出し側で参照・更新）
- 公開メンバー（シグネチャ）:
  - L28: public int ParticleCount = 20;
  - L30: public float Scale = 1.0f;
  - L32: public float Duration = 1.0f;

### TonMagicEffect
- 種別: class
- ソース: mononotonka/TonMagicEffect.cs:39
- 前提条件（Phase1暫定）: 使用前に初期化/登録系セットアップを完了すること（詳細は Phase 2 で確認）
- 副作用（Phase1暫定）: エフェクト状態（寿命・位置・描画情報）を更新し描画する
- 公開メンバー（シグネチャ）:
  - L783: public void Fire(float x, float y, int level = 1)
  - L819: public void Fire(float x, float y, MagicLevel level)
  - L830: public void Ice(float x, float y, int level = 1)
  - L863: public void Ice(float x, float y, MagicLevel level)
  - L874: public void Wind(float x, float y, int level = 1)
  - L907: public void Wind(float x, float y, MagicLevel level)
  - L918: public void Earth(float x, float y, int level = 1)
  - L955: public void Earth(float x, float y, MagicLevel level)
  - L966: public void Heal(float x, float y, int level = 1)
  - L999: public void Heal(float x, float y, MagicLevel level)
  - L1011: public void Poison(float x, float y, int level = 1)
  - L1052: public void Poison(float x, float y, MagicLevel level)
  - L1064: public void Light(float x, float y, int level = 1)
  - L1097: public void Light(float x, float y, MagicLevel level)
  - L1105: public void Update(GameTime gameTime)
  - L1274: public int ActiveCount => _activeEffects.Count;
  - L1279: public void Clear()

### TonMath
- 種別: class
- ソース: mononotonka/TonMath.cs:9
- 前提条件（Phase1暫定）: 特になし（純粋計算ユーティリティ）
- 副作用（Phase1暫定）: なし（計算結果を返すのみ）
- 公開メンバー（シグネチャ）:
  - L19: public int Rand(int min, int max)
  - L30: public float RandF(float min, float max)
  - L38: public float GetAngle(float x1, float y1, float x2, float y2)
  - L46: public float GetDistance(float x1, float y1, float x2, float y2)
  - L58: public float Lerp(float current, float target, float amount)
  - L66: public bool HitCheckRect(Rectangle rect1, Rectangle rect2)
  - L74: public bool HitCheckCircle(Vector2 pos1, float r1, Vector2 pos2, float r2)
  - L84: public bool IsPointInRect(float x, float y, Rectangle rect)

### TonMenu
- 種別: class
- ソース: mononotonka/TonMenu.cs:12
- 前提条件（Phase1暫定）: Items 追加後に Control/Draw を呼ぶこと。入力は呼び出し側で供給すること
- 副作用（Phase1暫定）: カーソル・選択状態を更新し、決定/キャンセルイベントを発火する
- 公開メンバー（シグネチャ）:
  - L39: public List<TonMenuItem> Items { get; private set; } = new List<TonMenuItem>();
  - L45: public int CursorIndex { get; private set; } = 0;
  - L51: public int ScrollOffset { get; private set; } = 0;
  - L56: public Rectangle WindowRect { get; private set; }
  - L59: public int ColCount { get; private set; }
  - L62: public int RowCount { get; private set; }
  - L65: public int ItemWidth { get; private set; }
  - L68: public int ItemHeight { get; private set; }
  - L74: public bool AllowBlankSelect { get; private set; }
  - L77: public bool AllowMultiSelect { get; private set; }
  - L80: public bool IsActive { get; set; } = true;
  - L85: public string DefaultFontId { get; private set; } = null;
  - L88: public Color DefaultTextColor { get; private set; } = Color.White;
  - L91: public Color DisabledTextColor { get; private set; } = Color.Gray;
  - L94: public Color SelectedCursorColor { get; private set; } = Color.FromNonPremultiplied(255, 255, 200, 100);
  - L97: public Color CursorColor { get; private set; } = Color.White;
  - L100: public float ContentScale { get; private set; } = 1.0f;
  - L105: public int TextOffset { get; private set; } = 10;
  - L108: public string CursorIcon { get; private set; } = null;
  - L111: public Vector2 CursorIconOffset { get; private set; } = Vector2.Zero;
  - L114: public bool IsLoop { get; private set; } = false;
  - L119: public bool IsDecided { get; private set; }
  - L122: public bool IsCancelled { get; private set; }
  - L127: public Action OnEnter;
  - L130: public Action OnExit;
  - L133: public Action OnPause;
  - L136: public Action OnResume;
  - L142: public Action<TonMenu> OnPostDraw;
  - L145: public Action<TonMenuItem> OnSelectionChanged;
  - L159: public TonMenu(Rectangle rect, int column, int row, int width, int height, bool bAllowBlankSelect, bool bAllowMultiSelect = false)
  - L176: public void SetTextOffset(int offset)
  - L185: public void SetContentScale(float scale)
  - L195: public void SetCursorIcon(string iconName, Vector2? offset = null)
  - L205: public void SetFont(string fontId)
  - L215: public void SetTextColor(Color defaultColor, Color? disabledColor = null)
  - L226: public void SetCursorColor(Color selectedColor, Color? frameColor = null)
  - L236: public void SetLoopable(bool isLoop)
  - L246: public void AddItem(TonMenuItem item)
  - L255: public void Clear()
  - L269: public TonMenuItem GetCurrentItem()
  - L282: public void Control(InputType type)
  - L407: public bool CanScrollUp => ScrollOffset > 0;
  - L412: public bool CanScrollDown => (ScrollOffset + RowCount) < GetMaxRow();
  - L472: public void Draw()

### TonMenu.InputType
- 種別: enum
- ソース: mononotonka/TonMenu.cs:18
- 前提条件（Phase1暫定）: 特になし（値定義/データ構造）
- 副作用（Phase1暫定）: なし（値/データ保持のみ）
- 公開メンバー（シグネチャ）:
  - L21: (enum) Up,
  - L23: (enum) Down,
  - L25: (enum) Left,
  - L27: (enum) Right,
  - L29: (enum) OK,
  - L31: (enum) Cancel

### TonMenuItem
- 種別: class
- ソース: mononotonka/TonMenu.cs:549
- 前提条件（Phase1暫定）: 特になし（引数妥当性は呼び出し側で担保）
- 副作用（Phase1暫定）: 公開メンバーに応じて内部状態を変更する可能性がある（詳細は Phase 2 で確認）
- 公開メンバー（シグネチャ）:
  - L552: public TonMenu ParentMenu { get; set; }
  - L555: public object Tag { get; set; }
  - L558: public bool Enabled { get; set; } = true;
  - L561: public Action<TonMenuItem> OnDecided;
  - L564: public Action<TonMenuItem> OnSelectionChanged;
  - L572: public TonMenuItem()
  - L581: public void SetLayout(TonMenuPanel panel)
  - L591: public void Draw(Rectangle rect)

### TonMenuElement
- 種別: class
- ソース: mononotonka/TonMenu.cs:603
- 前提条件（Phase1暫定）: 特になし（引数妥当性は呼び出し側で担保）
- 副作用（Phase1暫定）: 公開メンバーに応じて内部状態を変更する可能性がある（詳細は Phase 2 で確認）
- 公開メンバー（シグネチャ）:
  - L610: public abstract void Draw(Rectangle area, TonMenuItem item);

### ElementAlignment
- 種別: enum
- ソース: mononotonka/TonMenu.cs:616
- 前提条件（Phase1暫定）: 特になし（値定義/データ構造）
- 副作用（Phase1暫定）: なし（値/データ保持のみ）
- 公開メンバー: なし

### TonMenuText
- 種別: class
- ソース: mononotonka/TonMenu.cs:622
- 前提条件（Phase1暫定）: 特になし（引数妥当性は呼び出し側で担保）
- 副作用（Phase1暫定）: 公開メンバーに応じて内部状態を変更する可能性がある（詳細は Phase 2 で確認）
- 公開メンバー（シグネチャ）:
  - L628: public string FontId { get; set; } = null;
  - L631: public Color? Color { get; set; } = null;
  - L634: public float? Scale { get; set; } = null;
  - L637: public ElementAlignment Alignment { get; set; } = ElementAlignment.Left;
  - L640: public Vector2 Offset { get; set; } = Vector2.Zero;
  - L643: public TonMenuText(string text) { _staticText = text; }
  - L646: public TonMenuText(Func<string> textFunc) { _textFunc = textFunc; }
  - L648: public override void Draw(Rectangle area, TonMenuItem item)

### TonMenuIcon
- 種別: class
- ソース: mononotonka/TonMenu.cs:700
- 前提条件（Phase1暫定）: 特になし（引数妥当性は呼び出し側で担保）
- 副作用（Phase1暫定）: 公開メンバーに応じて内部状態を変更する可能性がある（詳細は Phase 2 で確認）
- 公開メンバー（シグネチャ）:
  - L706: public Vector2 Offset { get; set; } = Vector2.Zero;
  - L709: public float Scale { get; set; } = 1.0f;
  - L711: public TonMenuIcon(string iconName) { _staticIcon = iconName; }
  - L712: public TonMenuIcon(Func<string> iconFunc) { _iconFunc = iconFunc; }
  - L714: public override void Draw(Rectangle area, TonMenuItem item)

### TonMenuPanel
- 種別: class
- ソース: mononotonka/TonMenu.cs:748
- 前提条件（Phase1暫定）: 特になし（引数妥当性は呼び出し側で担保）
- 副作用（Phase1暫定）: 公開メンバーに応じて内部状態を変更する可能性がある（詳細は Phase 2 で確認）
- 公開メンバー（シグネチャ）:
  - L765: public TonMenuPanel(LayoutType type)
  - L778: public void AddChild(TonMenuElement element, float ratioOrSize = 1.0f)
  - L783: public override void Draw(Rectangle area, TonMenuItem item)

### TonMenuPanel.LayoutType
- 種別: enum
- ソース: mononotonka/TonMenu.cs:751
- 前提条件（Phase1暫定）: 特になし（値定義/データ構造）
- 副作用（Phase1暫定）: なし（値/データ保持のみ）
- 公開メンバー（シグネチャ）:
  - L754: (enum) Free,
  - L756: (enum) Vertical,
  - L758: (enum) Horizontal

### TonMenuManager
- 種別: class
- ソース: mononotonka/TonMenuManager.cs:11
- 前提条件（Phase1暫定）: Push でメニューを積んだ状態で Update/Draw を呼ぶこと
- 副作用（Phase1暫定）: メニュースタックを更新し、Push/Pop時にコールバックを実行する
- 公開メンバー（シグネチャ）:
  - L24: public bool IsMenuOpen => _menuStack.Count > 0;
  - L31: public void Push(TonMenu menu, Action<object> onResult = null)
  - L58: public void Pop(object result = null)
  - L82: public void Clear()
  - L105: public void SetInputButtons(string ok, string cancel)
  - L114: public void Update()
  - L142: public void Draw(Action<TonMenu> backgroundDrawer = null)

### TonMessage
- 種別: class
- ソース: mononotonka/TonMessage.cs:13
- 前提条件（Phase1暫定）: Initialize 後に利用すること。Show/LoadScript 後に Update/Draw を継続呼び出しすること
- 副作用（Phase1暫定）: メッセージ進行状態、イベント通知、表示内容を更新し描画に反映する
- 公開メンバー（シグネチャ）:
  - L64: public void SetTextStyle(float scale, float lineSpacing = 10.0f, float kerningOffset = 3.0f)
  - L75: public void SetTextSpeed(float speedMs)
  - L87: public void SetInputWaitingIcon(string imageId)
  - L135: public void Initialize()
  - L146: public void SetWindowRect(int x, int y, int width, int height)
  - L156: public void LoadScript(string filePath, string scriptName = null)
  - L225: public void Show(string scriptText)
  - L249: public void Close()
  - L259: public void Clear()
  - L277: public bool IsBusy()
  - L285: public string GetEvent()
  - L294: public void Next()
  - L350: public void Update(GameTime gameTime, bool isInput = false)
  - L655: public void Draw()

### TonParticleParam
- 種別: class
- ソース: mononotonka/TonParticle.cs:12
- 前提条件（Phase1暫定）: 特になし（設定/状態データとして利用）
- 副作用（Phase1暫定）: 設定/状態値を保持する（呼び出し側で参照・更新）
- 公開メンバー（シグネチャ）:
  - L15: public string ImageName;
  - L17: public int MinLife = 500;
  - L19: public int MaxLife = 1000;
  - L21: public float MinSpeed = 1f;
  - L23: public float MaxSpeed = 3f;
  - L25: public float MinAngle = 0f;
  - L27: public float MaxAngle = MathHelper.TwoPi;
  - L29: public float MinScale = 0.5f;
  - L31: public float MaxScale = 1.0f;
  - L33: public float Gravity = 0f;
  - L35: public Color StartColor = Color.White;
  - L37: public Color EndColor = Color.Transparent; // デフォルトでフェードアウト
  - L39: public bool IsAdditive = false; // 加算合成を使用するか
  - L41: public float MinRotationSpeed = 0f;
  - L43: public float MaxRotationSpeed = 0f;
  - L45: public float OrbitalRadius = 0f;
  - L47: public float OrbitalSpeed = 0f;
  - L49: public bool HasShadow = false;
  - L51: public Color ShadowColor = new Color(0, 0, 0, 150);
  - L53: public float ShadowScale = 1.2f;

### TonParticle
- 種別: class
- ソース: mononotonka/TonParticle.cs:60
- 前提条件（Phase1暫定）: Register でパラメータ登録後に Play を呼ぶこと。Update/Draw を毎フレーム呼ぶこと
- 副作用（Phase1暫定）: パーティクルの生成/更新/破棄を行い描画する
- 公開メンバー（シグネチャ）:
  - L83: public TonParticle()
  - L94: public void Register(string name, TonParticleParam param)
  - L106: public void Play(string name, float x, float y, int count = 1)
  - L165: public void Update(GameTime gameTime)
  - L206: public void Clear()
  - L214: public void Draw()

### TonPrimitive
- 種別: class
- ソース: mononotonka/TonPrimitive.cs:12
- 前提条件（Phase1暫定）: Initialize 後に描画APIを呼ぶこと
- 副作用（Phase1暫定）: プリミティブ描画コマンドを発行し描画結果を出力する
- 公開メンバー（シグネチャ）:
  - L18: public void Initialize(Game game, GraphicsDeviceManager graphics)
  - L61: public void DrawSpline(List<Vector2> points, float thickness, Color color)
  - L95: public void DrawArrow(List<Vector2> points, float thickness, Color color, float headSize = 20f, Color? borderColor = null, float borderThickness = 2f)
  - L167: public void GetSplineInfo(List<Vector2> points, float t, out Vector2 position, out float radians)
  - L364: public void DrawCircle(Vector2 center, float radius, Color color, int segments = 36)
  - L377: public void DrawCircle(Vector2 center, float radiusX, float radiusY, Color color, int segments = 36)
  - L435: public void DrawRectangle(Vector2 position, Vector2 size, Color color, float rotation = 0f, Vector2? origin = null)
  - L566: public void DrawTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color color)
  - L596: public void DrawPolygon(List<Vector2> vertices, Color color)
  - L642: public void DrawArc(Vector2 center, float radius, float startAngle, float endAngle, float thickness, Color color, int segments = 24)
  - L678: public void DrawRoundedRectangle(Vector2 position, Vector2 size, float cornerRadius, Color color, float rotation = 0f, Vector2? origin = null)
  - L822: public void DrawSplineArea(List<Vector2> points, float bottomY, Color topColor, Color bottomColor, bool isAdditive = false)
  - L875: public void DrawSector(Vector2 center, float radius, float startAngle, float endAngle, Color color, int segments = 24, float innerRadius = 0f)
  - L960: public void DrawSplineDashed(List<Vector2> points, float thickness, Color color, float dashLength = 10f, float gapLength = 10f)
  - L1005: public void DrawArrowDashed(List<Vector2> points, float thickness, Color color, float headSize = 20f, float dashLength = 10f, float gapLength = 10f, Color? borderColor = null, float borderThickness = 2f)
  - L1202: public void DrawRibbon(List<Vector2> points, float startWidth, float endWidth, Color startColor, Color endColor)
  - L1294: public void DrawBolt(Vector2 start, Vector2 end, float thickness, Color color, float difficulty, float updatesPerSecond = 60f)
  - L1392: public void DrawFocusLines(Vector2 center, float outerRadius, float intensity, Color color, float updatesPerSecond = 0f)

### TonSaveLoadMode
- 種別: enum
- ソース: mononotonka/TonSaveLoadMenu.cs:12
- 前提条件（Phase1暫定）: 特になし（値定義/データ構造）
- 副作用（Phase1暫定）: なし（値/データ保持のみ）
- 公開メンバー（シグネチャ）:
  - L15: (enum) SaveOnly,
  - L17: (enum) LoadOnly,
  - L19: (enum) BothDefaultSave,
  - L21: (enum) BothDefaultLoad

### TonSaveLoadMenu
- 種別: class
- ソース: mononotonka/TonSaveLoadMenu.cs:28
- 前提条件（Phase1暫定）: Ton.Storage/Ton.Data が初期化済みであること。Open 後は Update/Draw を呼ぶこと
- 副作用（Phase1暫定）: セーブ/ロードUI状態を変更し、ロード時に Ton.Data 等へ反映する
- 公開メンバー（シグネチャ）:
  - L56: public Action OnLoaded { get; set; }
  - L62: public void Open(TonSaveLoadMode mode)
  - L96: public void Close()
  - L106: public bool IsOpen()
  - L114: public void Update()
  - L319: public static void ExecuteAutoSave()
  - L358: public void Draw()

### IScene
- 種別: interface
- ソース: mononotonka/TonScene.cs:10
- 前提条件（Phase1暫定）: 実装クラス側でライフサイクル契約を満たすこと
- 副作用（Phase1暫定）: なし（契約定義のみ）
- 公開メンバー（シグネチャ）:
  - L12: (implicit public) void Initialize();
  - L13: (implicit public) void Update(GameTime gameTime);
  - L14: (implicit public) void Draw();
  - L15: (implicit public) void Terminate();

### TonScene
- 種別: class
- ソース: mononotonka/TonScene.cs:22
- 前提条件（Phase1暫定）: Initialize 後に利用すること。Change へ渡す IScene 実装は契約メソッドを実装済みであること
- 副作用（Phase1暫定）: シーン遷移状態を更新し、必要に応じてシーンの Initialize/Terminate を呼ぶ
- 公開メンバー（シグネチャ）:
  - L38: public void Initialize()
  - L45: public void Terminate()
  - L57: public void Change(IScene nextScene, float durationOut = 0.0f, float durationIn = -1.0f, Color fadeColor = default)
  - L75: public IScene GetCurrentScene()
  - L84: public void Update(GameTime gameTime)
  - L126: public void Draw()

### TonSceneTemplete
- 種別: class
- ソース: mononotonka/TonSceneTemplate.cs:12
- 前提条件（Phase1暫定）: IScene 契約（Initialize/Terminate/Update/Draw）に従って使用すること
- 副作用（Phase1暫定）: 実装内容に応じてゲーム状態と描画結果を変更する
- 公開メンバー（シグネチャ）:
  - L17: public void Initialize()
  - L26: public void Terminate()
  - L37: public void Update(GameTime gameTime)
  - L51: public void Draw()

### TonSound
- 種別: class
- ソース: mononotonka/TonSound.cs:14
- 前提条件（Phase1暫定）: Initialize 後に利用すること。再生対象は LoadSound/LoadBGM 済みであること
- 副作用（Phase1暫定）: 音声リソースのロード/解放、BGM/SE再生・停止、音量・フェード状態を変更する
- 公開メンバー（シグネチャ）:
  - L49: public long TotalSoundMemory => _totalSoundMemory;
  - L54: public long MaxSoundMemory => _maxSoundMemory;
  - L99: public void Initialize(IServiceProvider serviceProvider, string rootDirectory = "Content")
  - L165: public void SetCacheTimeout(double seconds)
  - L173: public void Update(GameTime gameTime)
  - L256: public void Unload(string contentId)
  - L302: public void UnloadAll()
  - L349: public void LoadSound(string path, string name, string contentId = "Default", float baseVolume = 1.0f)
  - L385: public void LoadBGM(string path, string name, string contentId = "Default", float baseVolume = 1.0f)
  - L419: public void PlayBGM(string bgmName, float fadeSeconds = 0.0f, float volume = 1.0f)
  - L517: public void StopBGM(bool bPause = false)
  - L521: public void StopBGM(float fadeSeconds)
  - L525: public void StopBGM(float fadeSeconds = 0.0f, bool bPause = false)
  - L563: public void PlaySE(string seName, float volume = 1.0f)
  - L595: public void StopAll()
  - L605: public void SetMasterVolume(float volume)
  - L617: public float GetMasterVolume()
  - L626: public TimeSpan GetBGMPosition()
  - L643: public TimeSpan GetBGMLength()
  - L655: public void Terminate()
  - L667: public void DebugForceExpireCache(string contentId)

### TonSpec
- 種別: class
- ソース: mononotonka/TonSpec.cs:11
- 前提条件（Phase1暫定）: Windows API 呼び出し可能環境であること
- 副作用（Phase1暫定）: システム情報を取得しログ出力する
- 公開メンバー（シグネチャ）:
  - L39: public static void LogSpecs()

### TonStorage
- 種別: class
- ソース: mononotonka/TonStorage.cs:11
- 前提条件（Phase1暫定）: 保存対象がシリアライズ可能であり、ファイル名が有効であること
- 副作用（Phase1暫定）: セーブデータのファイルI/O（保存・読込）を行う
- 公開メンバー（シグネチャ）:
  - L18: public TonStorage()
  - L40: public void Save<T>(string fileName, T data)
  - L61: public T Load<T>(string fileName)
  - L83: public bool Exists(string fileName)

## 3. Phase 3 追記欄（manual突合用）
### 3.1 判定ルール（今回適用）
- `manual公開対象`:
  - `manual/class_*.html` の `method-sig` に記載された型/メンバー
  - `manual` の利用手順サンプルで直接呼び出しを案内している型/メンバー
- `公開対象外（未掲載許容）`:
  - フレームループ内の内部連携（`Initialize/Update/Draw/Terminate` など）を主目的とする公開メンバー
  - デバッグ補助API（`Debug*`）や内部状態参照用プロパティ
  - データ保持用の公開フィールド/プロパティ（設定DTO、Param、State等）

### 3.2 差分一覧（manual公開対象）
| 型/メンバー | 実装シグネチャ | manual記載 | 差分分類 | 修正先（実装/manual） | 備考 |
| --- | --- | --- | --- | --- | --- |
| `Ton.Initialize/Update/Draw/Terminate` 呼び出し例 | `public void Initialize/Update/Draw/Terminate`（インスタンスメソッド） | `Ton.Instance.Initialize/Update/Draw/Terminate` に修正済み | 解消済み（manual更新） | manual | `manual/class_ton.html`, `manual/getting_started.html` を更新 |
| `TonCharacter.MoveTo` | `MoveTo(string id, int targetX, float speed, CharacterAnimType moveAnim = CharacterAnimType.Walk)` | 同シグネチャを記載済み | 解消済み（manual更新） | manual | `manual/class_toncharacter.html` を更新 |
| `TonGraphics.FillRect` | `FillRect(int x, int y, int w, int h, Color color)` | 同シグネチャを記載済み | 解消済み（manual更新） | manual | `manual/class_tongraphics.html` を更新 |
| `TonGraphics.ShakeScreen` | `ShakeScreen(float seconds, float ratioX, float ratioY, float frequency = 20.0f)` | 同シグネチャを記載済み | 解消済み（manual更新） | manual | `manual/class_tongraphics.html` を更新 |
| `Ton.ConfigMenu`, `Ton.SaveLoadMenu` | `public static TonConfigMenu ConfigMenu`, `public static TonSaveLoadMenu SaveLoadMenu` | `Ton` プロパティ一覧および関連サンプル記述へ反映済み | 解消済み（manual更新） | manual | `manual/class_ton.html`, `manual/class_tonconfigmenu.html` を更新 |
| `TonStorage.Exists` | `public bool Exists(string fileName)` | メソッド説明とサンプル呼び出しを追記済み | 解消済み（manual更新） | manual | `manual/class_tonstorage.html` を更新 |

### 3.3 実装のみだが未掲載許容（公開対象外）
| 型/メンバー | 実装シグネチャ | manual記載 | 差分分類 | 修正先（実装/manual） | 備考 |
| --- | --- | --- | --- | --- | --- |
| サブシステムのライフサイクル群 | 例: `TonGraphics.Initialize/Update/Begin/End/Terminate`, `TonSound.Initialize/Update/Terminate`, `TonParticle.Update/Draw`, `TonSaveLoadMenu.Update/Draw` | 一部または未記載 | 公開対象外（未掲載許容） | なし | フレーム内内部連携のため |
| デバッグ系 | `TonGraphics.DebugForceExpireCache`, `TonSound.DebugForceExpireCache` | 未記載 | 公開対象外（未掲載許容） | なし | デバッグ専用 |
| 状態参照の冗長公開 | 例: `TonGame.VirtualWidth/VirtualHeight/UpdateFPS/DrawFPS`（`Get*` も別途存在） | `Get*`中心に記載 | 公開対象外（未掲載許容） | なし | manualは利用導線優先で絞り込み |
| 設定/データ保持型の生メンバー | 例: `TonConfigMenu.ConfigData`, `TonCharacter.AnimConfig`, 各 `Param/State` のフィールド | 一部要約記載 | 公開対象外（未掲載許容） | なし | 内部連携・データコンテナ用途 |
| `TonSceneTemplete`（テンプレートクラス） | `public class TonSceneTemplete : IScene` | `class TonSceneTemplate` 記載あり | 公開対象外（未掲載許容） | なし | ファイルコピー起点の特殊テンプレート運用として、名称差は修正対象外 |

### 3.4 判定メモ
- ユーザー指定に基づき、`public` であっても「クラス間連携のための公開」は差分不具合として扱わない。
- Phase 3 で修正対象にするのは「manual公開対象」と判定した差分のみ。

## 4. Phase 4 利用例整合確認（最小実施）
- 確認対象: `GameMain.cs`, `SampleScene08.cs`, `SampleScene09.cs`
- 確認観点:
  - ライフサイクル呼び出し: `Ton.Instance.Initialize/Update/Draw`
  - 主要導線: `Ton.ConfigMenu`, `Ton.SaveLoadMenu`, `Ton.Storage`
- 結果:
  - `GameMain.cs` は `Ton.Instance.Initialize/Update/Draw` を使用しており、manualと整合。
  - `SampleScene08.cs` は `Ton.ConfigMenu`, `Ton.Storage` を使用しており、manualと整合。
  - `SampleScene09.cs` は `Ton.SaveLoadMenu` を使用しており、manualと整合。
  - manual側で `Ton.configmenu` の旧表記が1件見つかり、`Ton.ConfigMenu` に修正済み（`manual/class_tonconfigmenu.html`）。

