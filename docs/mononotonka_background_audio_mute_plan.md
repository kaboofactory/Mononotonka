# Mononotonka バックグラウンド時オーディオミュート 実装計画

## 1. 目的
- `TonSound` に、BGM と SE（効果音）をミュート/解除できる公開関数を追加する。
- ウィンドウがバックグラウンド（非アクティブ）時に音を自動ミュートし、アクティブ復帰で自動解除する。
- 上記自動ミュート機能を `TonConfigMenu` から ON/OFF できるようにする。

## 2. 要求の解釈
- 本計画では、ユーザー要望の「音楽」は `SE（効果音）` として扱う。
- 対象音声は `BGM + SE` の2系統とする。
- ミュートは「停止」ではなく「無音化」を基本とする。
  - BGM: 再生状態は維持しつつ無音化。
  - SE: ミュート中は新規再生を抑止し、ON切替時に再生中SEを停止して即時無音化。

## 3. 対象範囲
- 実装対象:
  - `mononotonka/TonSound.cs`
  - `mononotonka/Ton.cs`
  - `mononotonka/TonGame.cs`
  - `mononotonka/TonConfigMenu.cs`
- 文書対象:
  - `manual/class_tonsound.html`
  - `manual/class_tonconfigmenu.html`
- 非対象:
  - 個別SEごとのミュート設定
  - シーン単位の自動ミュートルール追加
  - 外部OSミキサー制御

## 4. 実装方針
1. `TonSound` に手動ミュートAPIを追加する。
2. `TonSound` に「非アクティブ時自動ミュート」設定APIを追加する。
3. `TonGame` でウィンドウ状態を表す `enum` と、その `enum` を返す単一関数を追加する。
4. `Ton.Update` から毎フレーム、ウィンドウ状態を `TonSound` に通知する。
5. `TonConfigMenu` に `Mute In Background` 項目を追加し、設定を保存/読込する。
6. manual を更新して新規APIと設定項目を反映する。

## 5. 実装タスク
1. `TonSound` API追加
   - `SetSEMuted(bool muted)`, `IsSEMuted()`
   - `SetBGMMuted(bool muted)`, `IsBGMMuted()`
   - `SetMuteWhenInactive(bool enabled)`, `GetMuteWhenInactive()`
   - `ApplyWindowActivityState(TonWindowActivityState state)`
2. `TonSound` 内部ロジック更新
   - BGM音量反映時に実効ミュート状態を考慮する。
   - SE再生時に実効ミュート状態なら再生しない。
   - SEミュートON時は再生中SEを停止/破棄する。
   - 非アクティブ中に `SetMuteWhenInactive(false)` された場合は、その場でバックグラウンド由来ミュートを解除する。
3. `TonGame` API追加
   - 共通enum `TonWindowActivityState` を名前空間直下に追加する（例: `mononotonka/TonWindowActivityState.cs`）。
   - `GetWindowActivityState()` の単一関数を公開する。
   - `TonGame.Initialize(...)` 時に実際のウィンドウ状態で内部状態を初期化し、初回 `Update` の誤判定を防ぐ。
   - `TonGame.Update(...)` 内で前フレーム比較により enum 値を更新する。
4. `Ton.Update` 連携
   - `sound.Update(gameTime)` の前に `ApplyWindowActivityState(game.GetWindowActivityState())` を呼ぶ。
5. `TonConfigMenu` 追加項目
   - 表示項目: `Mute In Background`（ON/OFF）
   - `ConfigData` に `MuteWhenInactive` を追加し、保存/読込対応。
6. manual 更新
   - `class_tonsound.html`: 新規API説明を追加。
   - `class_tonconfigmenu.html`: 新規設定項目を追加。

## 6. 互換性方針
- 既存の `config.json` に `MuteWhenInactive` が無くても動作するようにする。
- 既存APIシグネチャは変更しない（追加のみ）。

## 7. 検証項目
- ケース1: 手動で `SetSEMuted(true/false)` が効く。
- ケース2: 手動で `SetBGMMuted(true/false)` が効く。
- ケース3: 非アクティブ遷移時に、設定ONならBGM/SEが無音化される。
- ケース4: アクティブ復帰時に、設定ONならBGM/SEが復帰する。
- ケース5: 設定OFF状態では、ウィンドウのアクティブ/非アクティブ遷移で音量状態が変化しない。
- ケース6: `TonConfigMenu` で設定変更後、再起動して値が保持される。
- ケース7: `GetWindowActivityState()` がアクティブ化フレームで `JustActivated` を返す。
- ケース8: `GetWindowActivityState()` が非アクティブ化フレームで `JustDeactivated` を返す。
- ケース9: 初回 `Update` で `JustActivated` / `JustDeactivated` が誤発火しない。
- ケース10: 非アクティブ中に `Mute In Background` を OFF にすると、その場でBGMミュートが解除される。

## 8. 品質ゲート
- 実装後に `dotnet build Mononotonka.csproj -v minimal` を実施し、0 warning / 0 error を確認する。

## 9. 受け入れ基準
- BGM/SEミュートAPIが `TonSound` で公開されること。
- `TonGame` で `GetWindowActivityState()` により状態と瞬間を取得できること。
- ウィンドウ非アクティブ時の自動ミュートが機能すること。
- 初回 `Update` で `JustActivated` / `JustDeactivated` が誤発火しないこと。
- 非アクティブ中に `Mute In Background` を OFF にした場合、その時点でBGMミュートが解除されること。
- `TonConfigMenu` から自動ミュートON/OFFを変更でき、永続化されること。
- manual に新規API/設定項目が反映されること。
