# Mononotonka バックグラウンド時オーディオミュート 詳細設計

## 1. 設計方針
- 実装は「既存挙動を壊さない追加方式」で行う。
- ミュート要因を分離する。
  - 手動ミュート（ユーザー/ゲームロジックが明示設定）
  - バックグラウンドミュート（ウィンドウ非アクティブ時の自動設定）
- 実効ミュートは `手動 OR バックグラウンド` で判定する。

## 2. 変更対象と責務
- `TonSound.cs`
  - ミュート状態の保持、BGM/SEへの適用、公開API提供
- `TonGame.cs`
  - ウィンドウ状態enumの管理と単一取得APIの公開
- `Ton.cs`
  - 毎フレームのウィンドウ状態を `TonSound` に通知
- `TonConfigMenu.cs`
  - 「バックグラウンド時ミュート」設定のUIと永続化

## 3. TonSound 詳細設計

### 3.1 追加フィールド
- `private bool _isSeMutedByUser = false;`
- `private bool _isBgmMutedByUser = false;`
- `private bool _isMutedByInactiveWindow = false;`
- `private bool _muteWhenInactive = false;`

### 3.2 追加公開API
```csharp
/// <summary>
/// 効果音(SE)の手動ミュート状態を設定します。
/// </summary>
/// <param name="muted">trueでミュート、falseで解除</param>
public void SetSEMuted(bool muted);

/// <summary>
/// 効果音(SE)の手動ミュート状態を取得します。
/// </summary>
public bool IsSEMuted();

/// <summary>
/// BGMの手動ミュート状態を設定します。
/// </summary>
/// <param name="muted">trueでミュート、falseで解除</param>
public void SetBGMMuted(bool muted);

/// <summary>
/// BGMの手動ミュート状態を取得します。
/// </summary>
public bool IsBGMMuted();

/// <summary>
/// ウィンドウ非アクティブ時に自動ミュートするかを設定します。
/// </summary>
/// <param name="enabled">trueで有効、falseで無効</param>
public void SetMuteWhenInactive(bool enabled);

/// <summary>
/// ウィンドウ非アクティブ時自動ミュート設定を取得します。
/// </summary>
public bool GetMuteWhenInactive();

/// <summary>
/// ウィンドウのアクティブ状態を反映します。
/// </summary>
/// <param name="state">ウィンドウ状態</param>
public void ApplyWindowActivityState(TonWindowActivityState state);
```

### 3.3 追加内部ヘルパー
```csharp
private bool IsEffectiveSeMuted();
private bool IsEffectiveBgmMuted();
private void ApplyBgmMuteState();
private void StopAllSeInstancesForMute();
```

### 3.4 既存処理への反映点
- `ApplyCurrentBgmVolume()`
  - 実効BGMミュート時は `MediaPlayer.Volume = 0f`。
  - 非ミュート時は従来計算（`_bgmVolume * _masterVolume * BaseVolume`）。
- `PlaySE(...)`
  - 実効SEミュート時は再生処理を早期return。
- `SetMasterVolume(...)`
  - マスター変更後に `ApplyBgmMuteState()` を実行。
- `SetBGMMuted(...)`
  - 値変更時に `ApplyBgmMuteState()` を実行。
- `SetSEMuted(true)`
  - `StopAllSeInstancesForMute()` を実行して即時無音化。

### 3.5 ApplyWindowActivityState の仕様
- `state` が `Active` または `JustActivated` のとき、アクティブ扱いとする。
- `state` が `Inactive` または `JustDeactivated` のとき、非アクティブ扱いとする。
- `_muteWhenInactive == true` のときのみ `_isMutedByInactiveWindow` に反映。
- `_muteWhenInactive == false` の場合は `_isMutedByInactiveWindow = false` を維持。
- 非アクティブ中に `SetMuteWhenInactive(false)` が呼ばれた場合は、その時点で `_isMutedByInactiveWindow = false` にし、即時にBGMミュートを解除する。
- 上記の即時解除で、既に停止済みのSEは自動再生しない（解除後の新規SE再生から有効）。
- 状態変化時:
  - BGM: `ApplyBgmMuteState()` を呼ぶ。
  - SE: ミュートON遷移時のみ `StopAllSeInstancesForMute()` を呼ぶ。

## 4. TonGame 詳細設計

### 4.1 追加公開プロパティ/メソッド
```csharp
/// <summary>
/// ウィンドウの状態を表します。
/// </summary>
public enum TonWindowActivityState
{
    Active,
    JustActivated,
    Inactive,
    JustDeactivated
}

/// <summary>
/// 現在のウィンドウ状態を取得します。
/// </summary>
public TonWindowActivityState GetWindowActivityState();
```

### 4.1.1 enum定義配置
- `TonWindowActivityState` は `TonGame` ネストではなく、`Mononotonka` 名前空間直下の共通型として定義する。
- 配置先は `mononotonka/TonWindowActivityState.cs` とし、`TonGame` / `TonSound` の両方から参照する。

### 4.2 追加内部状態
- `private bool _prevWindowActive = true;`
- `private TonWindowActivityState _windowActivityState = TonWindowActivityState.Active;`

### 4.2.1 TonGame.Initialize での初期化仕様
- `currentActive = _game?.IsActive ?? true` を取得する。
- `Initialize(...)` 時点で以下を設定する。
  - `_prevWindowActive = currentActive`
  - `_windowActivityState = currentActive ? TonWindowActivityState.Active : TonWindowActivityState.Inactive`
- これにより、初回 `Update` で `JustActivated` / `JustDeactivated` が誤発火しないようにする。

### 4.3 TonGame.Update での更新仕様
- `currentActive = _game?.IsActive ?? true` を取得する。
- 状態を以下で更新する。
  - `currentActive && _prevWindowActive` → `Active`
  - `currentActive && !_prevWindowActive` → `JustActivated`
  - `!currentActive && _prevWindowActive` → `JustDeactivated`
  - `!currentActive && !_prevWindowActive` → `Inactive`
- `_prevWindowActive = currentActive`

### 4.4 備考
- 公開APIは `GetWindowActivityState()` の単一関数とし、呼び出し側で `switch` 判定する。
- `JustActivated` / `JustDeactivated` は1フレームのみ返るエッジ状態とする。

## 5. Ton.Update 連携設計

### 5.1 変更箇所
- `Ton.Update(GameTime gameTime)` 内のサウンド更新前に、以下を追加する。
```csharp
sound.ApplyWindowActivityState(game.GetWindowActivityState());
```

### 5.2 更新順序
1. `game.Update(gameTime)`
2. `input.Update(gameTime)`
3. `sound.ApplyWindowActivityState(game.GetWindowActivityState())` ← 追加
4. `sound.Update(gameTime)`
5. 既存の描画/シーン系更新

## 6. TonConfigMenu 詳細設計

### 6.1 設定項目追加
- 項目名: `Mute In Background`
- 値: `ON` / `OFF`

### 6.2 変更内容
- `_items` を以下に変更。
  - `Fullscreen`
  - `Resolution`
  - `Master Volume`
  - `Message Speed`
  - `Mute In Background`
  - `Close`
- `ConfigData` に `bool MuteWhenInactive { get; set; }` を追加。
- `Initialize()` で読込時に `Ton.Sound.SetMuteWhenInactive(data.MuteWhenInactive)` を適用。
- `Close()` で保存時に `MuteWhenInactive = Ton.Sound.GetMuteWhenInactive()` を保存。
- `ChangeSetting(...)` の `case` を1つ追加。
  - `case 4`: ON/OFFトグル
  - `case 5`: Close（現行の分岐条件文字列比較は維持可能）
- `Draw()` の値表示 `switch` に `Mute In Background` の表示を追加。

### 6.3 既存互換
- 旧 `config.json` では `MuteWhenInactive` が無くても `false` 扱いになる。
- 既存のフルスクリーン時 `Resolution` スキップ挙動は維持する。

## 7. manual 更新設計
- `manual/class_tonsound.html`
  - 新規メソッド（SE/BGMミュート系 + バックグラウンド自動ミュート系）を追記。
- `manual/class_tonconfigmenu.html`
  - 設定項目に `Mute In Background` を追加した旨を追記。

## 8. エラー/ログ方針
- 追加APIは基本no-throwで動作する。
- 状態変更時は必要最小限の `Ton.Log.Info` を出力する。
  - 例: `MuteWhenInactive ON/OFF`, `Window inactive mute applied`
- 毎フレームログは出さない（ログ汚染防止）。

## 9. 検証観点
- 手動ミュートON/OFFが即時反映されること。
- 非アクティブ遷移でミュートON、アクティブ復帰で解除されること。
- `Mute In Background` OFF状態では、ウィンドウのアクティブ/非アクティブ遷移で音状態が変わらないこと。
- 非アクティブ中に `SetMuteWhenInactive(false)` すると、その場でBGMミュートが解除されること。
- 設定保存後の再起動で値が保持されること。
- 既存API利用コードがビルドエラーにならないこと。
- `GetWindowActivityState()` がアクティブ化フレームで `JustActivated` を返すこと。
- `GetWindowActivityState()` が非アクティブ化フレームで `JustDeactivated` を返すこと。
- 初回 `Update` で `JustActivated` / `JustDeactivated` が誤発火しないこと。

## 10. 実装時の注意
- 同義機能の二重実装を避け、ミュート判定は `TonSound` に一元化する。
- `TonConfigMenu` 側で直接 `MediaPlayer` 操作は行わない。
- 追加処理は最小差分で入れ、既存フロー（Update/Draw/保存）を壊さない。
