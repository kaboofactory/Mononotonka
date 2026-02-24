# Mononotonka Audit Report

## 1. 実施情報
- 実施フェーズ: Phase 2-3（実装健全性レビュー / マニュアル整合性検証）
- 対象: `mononotonka/*.cs`
- 実施日: 2026-02-24
- 最終更新日: 2026-02-24
- 評価方針: `docs/mononotonka_audit_plan.md` の重大度定義に準拠

## 2. 実害のある問題

### 問題ID: MONO-AUD-001
- 重大度: High
- 対象ファイル（実装/manual）: 実装 `mononotonka/TonSound.cs`
- 根拠（該当API、該当箇所）:
  - `StopBGM(float fadeSeconds = 0.0f, bool bPause = false)` の即時停止分岐で `_currentBgmName = null` のみ実行され、`_isFading` が解除されない（`TonSound.cs:525-553`）。
  - `Update(GameTime)` のフェード処理で `_bgmResources[_currentBgmName]` を無条件参照している（`TonSound.cs:189-214`）。
- 影響範囲:
  - フェード中に即時停止が発生したフレーム以降、`_currentBgmName == null` の参照により `Update` で例外が発生しうる。
  - サウンド更新のクラッシュに直結する。
- 推奨対応:
  - 即時停止時に `_isFading = false` と `_stopAfterFade = false` を明示的にリセットする。
  - `Update` 側でも `_currentBgmName` の null/存在チェックを行って防御する。
- 対応方針（実装修正 / 文書修正 / 両方）: 実装修正

### 問題ID: MONO-AUD-002
- 重大度: High
- 対象ファイル（実装/manual）: 実装 `mononotonka/TonSound.cs`
- 根拠（該当API、該当箇所）:
  - `PlaySE` は毎回 `SoundEffectInstance` を生成して `Instances` に追加（`TonSound.cs:562-588`）。
  - `Update` のクリーンアップは `RemoveAll(i => i.State == SoundState.Stopped || i.IsDisposed)` のみで、停止済みインスタンスを `Dispose` していない（`TonSound.cs:228`）。
- 影響範囲:
  - 長時間プレイで SE 再生回数が増えるほど、未解放インスタンスが残り続ける。
  - メモリ/ハンドル枯渇による不安定化リスクがある。
- 推奨対応:
  - 停止済みインスタンスを `Dispose` してから管理リストから除去する。
  - 例: `for` で走査し `Stopped` を明示破棄、または `RemoveAll` 前に破棄処理を追加。
- 対応方針（実装修正 / 文書修正 / 両方）: 実装修正

### 問題ID: MONO-AUD-003
- 重大度: High
- 対象ファイル（実装/manual）: 実装 `mononotonka/TonGame.cs`, `mononotonka/TonGraphics.cs`
- 根拠（該当API、該当箇所）:
  - `TonGame.SetVirtualResolution` は `VirtualWidth/VirtualHeight` の更新のみ（`TonGame.cs:280-286`）。
  - 描画先 RT は `TonGraphics.Initialize` 時に一度だけ `Ton.Game.VirtualWidth/VirtualHeight` で生成（`TonGraphics.cs:126-127`）。
- 影響範囲:
  - 実行中に仮想解像度変更を行うと、論理解像度と RT サイズが乖離する。
  - 描画欠け/座標ずれ/フィルタ結果不整合が発生する可能性が高い。
- 推奨対応:
  - 仮想解像度変更時に `_virtualScreen/_tempScreen` を再生成する API を `TonGraphics` に追加し、`TonGame.SetVirtualResolution` から連携する。
- 対応方針（実装修正 / 文書修正 / 両方）: 実装修正
- 補足（推論）:
  - 本件は `SetVirtualResolution` と `Initialize` の実装差分からの推論であり、実機再現は Phase 5 で確認推奨。

### 問題ID: MONO-AUD-004
- 重大度: Medium
- 対象ファイル（実装/manual）: 実装 `mononotonka/TonGraphics.cs`
- 根拠（該当API、該当箇所）:
  - `SetRenderTarget` は存在しない `targetName` が渡された場合でも `_currentTargetName = targetName` を保持し続ける（`TonGraphics.cs:447-461`）。
  - `End` は `_currentTargetName == null` のときだけ最終転送処理を行う（`TonGraphics.cs:585-590`）。
- 影響範囲:
  - 誤ったターゲット名指定時に表示転送経路が外れ、画面更新が失敗する可能性がある。
  - 呼び出し側で原因特定しにくい（ログや例外が出ない）。
- 推奨対応:
  - `targetName` 未登録時は `ArgumentException` か `Ton.Log.Error` を出し、`_currentTargetName` を変更しない。
- 対応方針（実装修正 / 文書修正 / 両方）: 実装修正

### 問題ID: MONO-AUD-005
- 重大度: Medium
- 対象ファイル（実装/manual）: 実装 `mononotonka/TonConfigMenu.cs`
- 根拠（該当API、該当箇所）:
  - `Close()` の dirty 保存処理で `Ton.Storage.Save(CONFIG_FILENAME, data);` が2回連続実行されている（`TonConfigMenu.cs:109-112`）。
- 影響範囲:
  - 毎回不要な二重書き込みが発生し、I/Oコストとファイル破損リスクを増やす。
  - 監査方針の「過剰な保存・再読込を避ける」に反する。
- 推奨対応:
  - 重複呼び出し1回分を削除し、保存は1回に統一する。
- 対応方針（実装修正 / 文書修正 / 両方）: 実装修正

## 3. 改善提案（実害は現時点で未確認）

### 提案ID: MONO-AUD-SUG-001
- 重大度: Low
- 対象ファイル（実装/manual）: 実装 `mononotonka/TonLog.cs`, `mononotonka/TonSaveLoadMenu.cs`, `mononotonka/TonSound.cs`
- 根拠（該当API、該当箇所）:
  - 空 `catch {}` が複数箇所にあり、例外理由が失われる（`TonLog.cs:142`, `TonSaveLoadMenu.cs:432`, `TonSound.cs:431`, `TonSound.cs:575`）。
- 影響範囲:
  - 障害時の原因追跡が難しくなる。
- 推奨対応:
  - 少なくとも `Ton.Log.Warning/Error` で例外情報を残す。
- 対応方針（実装修正 / 文書修正 / 両方）: 実装修正

## 4. Phase 2-3 完了判定
- 実害のある問題と改善提案を分離して記録: 完了
- 重大度（High/Medium/Low）付与: 完了
- 根拠行の明記: 完了
- Phase 3 差分整合:
  - `docs/mononotonka_manual_gap_matrix.md` の `3.2 差分一覧（manual公開対象）` は全件解消済み。
  - `3.3 実装のみだが未掲載許容（公開対象外）` は運用ルールに基づき未修正で確定。
- Phase 5 のビルド/テスト実施結果:
  - 実装修正時に `dotnet build Mononotonka.csproj -v minimal` を実施。
  - 結果: 成功（0 Warning / 0 Error）。

## 5. Phase 4（最小実施）結果
- 対象: `GameMain.cs`, `SampleScene08.cs`, `SampleScene09.cs`
- 観点: manual主要導線（`Ton.Instance.Initialize/Update/Draw`, `Ton.ConfigMenu`, `Ton.SaveLoadMenu`, `Ton.Storage`）との整合
- 判定:
  - `GameMain.cs` のライフサイクル呼び出しは manual と整合。
  - `SampleScene08.cs`（`Ton.ConfigMenu`, `Ton.Storage`）は manual と整合。
  - `SampleScene09.cs`（`Ton.SaveLoadMenu`）は manual と整合。
  - manual側で `Ton.configmenu` の旧表記を1件検出し、`Ton.ConfigMenu` に修正済み（`manual/class_tonconfigmenu.html`）。
