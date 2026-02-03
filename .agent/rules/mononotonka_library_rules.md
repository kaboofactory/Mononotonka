# Mononotonka Library Usage Rules

この文書は、Mononotonkaライブラリを使用してコードを生成するすべてのAIエージェントおよび開発者のためのガイドラインです。
MononotonkaはMonoGameフレームワークの強力なラッパーライブラリであり、開発効率とパフォーマンスを最大化するために、**常に生のMonoGame APIよりも優先して使用しなければなりません**。

## 1. 基本原則 (Core Principles)

1.  **Mononotonka優先の法則**:
    *   MonoGameの標準機能（`SpriteBatch`, `ContentManager`, `KeyboardState`など）を直接使用する前に、`Ton`クラス経由で同等の機能が提供されていないか確認すること。
    *   提供されている場合は、必ずラッパーを使用すること。これにより、リソース管理、仮想解像度対応、入力の抽象化などが自動的に処理される。

2.  **Singletonアクセス**:
    *   すべての機能は `Mononotonka.Ton` クラスのシングルトンインスタンス、またはその静的ショートカットを通じてアクセスする。
    *   `new TonGraphics()` のようにサブシステムを直接インスタンス化してはならない。

    ```csharp
    // Convert calling conventions
    // BAD
    SpriteBatch.Draw(...);
    
    // GOOD
    Ton.Gra.SpriteBatch.Draw(...); // または Ton.Gra ラッパーメソッド
    ```

3.  **言語**:
    *   ドキュメント、コメント、およびユーザーとの対話はすべて**日本語**で行うこと。

## 2. 主要サブシステムの使用法 (Subsystems)

各機能へは `Ton` クラスの静的プロパティからアクセスする。

### 2.1. 共通アクセス (Ton Class)
*   **名前空間**: `Mononotonka`
*   **エントリポイント**: `Ton.Instance`
*   **ショートカット**: `Ton.Access` (例: `Ton.Gra`, `Ton.Input`)

### 2.2. グラフィックス (Ton.Gra / TonGraphics)
描画周りの処理は `Ton.Gra` に集約されている。

*   **テクスチャ読み込み**:
    *   `Content.Load<Texture2D>` を直接使用しない。
    *   `Ton.Gra.LoadTexture(path, name)` を使用する。
    *   これにより、重複読み込み防止（キャッシュ）、自動メモリ管理、コンテンツグループによる一括破棄が適用される。

*   **描画**:
    *   原則として `Ton.Gra.Begin()` / `Ton.Gra.End()` のサイクル内で描画を行う。
    *   これにより、仮想解像度（Virtual Resolution）のスケーリングや、画面シェイク、スクリーンフィルターが自動適用される。

### 2.3. 入力 (Ton.Input / TonInput)
キーボード、マウス、ゲームパッドの入力を統合管理する。

*   **入力取得**:
    *   `Keyboard.GetState()` などを毎フレーム呼ばない。
    *   `Ton.Input.IsKeyDown(...)`, `Ton.Input.IsTrigger(...)` などを使用する。
    *   入力の「押しっぱなし」「トリガー（押した瞬間）」の判定が容易になる。

### 2.4. サウンド (Ton.Sound / TonSound)
BGMとSE（効果音）を管理する。

*   **再生**:
    *   `Ton.Sound.PlayBGM(...)`, `Ton.Sound.PlaySE(...)` を使用する。
    *   ボリューム管理や、シーン切り替え時のフェードアウト処理などが統合されている。

### 2.5. ログ (Ton.Log / TonLog)
デバッグ出力用。

*   `Console.WriteLine` ではなく `Ton.Log.Info(...)`, `Ton.Log.Error(...)` を使用する。
*   ファイルへのログ出力とコンソール出力を同時に行う機能があるため。

### 2.6. その他
*   **Ton.Math**: 乱数 (`RandF`, `Rand`) や数学ヘルパー関数。 `System.Random` をその都度newしないこと。
*   **Ton.Scene**: シーン管理。 `IGameScene` インターフェースを実装したシーンクラスの遷移を管理する。
*   **Ton.ConfigMenu / Ton.SaveLoadMenu**: 組み込みのUI画面。

## 3. 実装パターン (Implementation Patterns)

### 初期化 (Initialization)
`Game1.cs` (またはメインクラス) で必ず初期化を行う。

```csharp
protected override void Initialize()
{
    // ... GraphicsDeviceManagerの初期化後 ...
    Ton.Instance.Initialize(this, _graphics);
    base.Initialize();
}
```

### 更新と描画 (Update & Draw)
メインループで必ず `Ton.Update` と `Ton.Draw` を呼ぶ。

```csharp
protected override void Update(GameTime gameTime)
{
    Ton.Instance.Update(gameTime);
    base.Update(gameTime);
}

protected override void Draw(GameTime gameTime)
{
    Ton.Instance.Draw(gameTime);
    base.Draw(gameTime);
}
```

## 4. 開発者への指示

新しいコードを作成する際は、既存の `mononotonka` フォルダ内のコード（特に `SampleScene` クラス群）を参考にし、ライブラリの設計思想に合わせること。
自己流のヘルパー関数を作る前に、`Ton` クラス内に既に同等の機能がないか確認すること。
