# TonGraphics 本画面背景描画 実装計画

## 1. 目的
- 既存の `Ton.Gra.DrawBackground()` は仮想画面（`_virtualScreen`）への描画であるため、実画面（バックバッファ）基準で背景を出したい用途に対応できない。
- 本計画では、バックバッファへ直接背景を描画する新APIを追加し、レターボックス領域や実解像度基準の背景演出を可能にする。

## 2. 現状整理
- 既存フローは `Ton.Draw` 内で `Ton.Gra.Begin()` → 各種描画 → `Ton.Gra.End()`。
- `Begin()` 時は既定で描画先が `_virtualScreen`。
- `End()` で `_virtualScreen` をバックバッファへ転送（フィルター適用含む）。
- よって Scene 側から即時にバックバッファへ描くと描画順や状態管理が崩れやすい。

## 3. 要件
- 既存 `DrawBackground` の挙動は変更しない。
- 新規に「バックバッファ描画版」の公開APIを追加する。
- 呼び出しは Scene の `Draw()` 内から可能にする。
- 実際の描画は `End()` 側で実行し、既存の SpriteBatch/RenderTarget 制御を壊さない。
- フィルター有無の双方で破綻しない。

## 4. API案
- 新規メソッド（公開）:
  - `void DrawBackgroundToBackBuffer(string imageName, TonDrawParam param = null)`
- 役割:
  - 呼び出し時は「描画要求」を登録するのみ（即時描画しない）。
  - `End()` でバックバッファ描画フェーズに入った際、要求があれば先に背景を描画。

## 5. 実装方針
1. `TonGraphics` に「バックバッファ背景要求」用の状態を追加。
   - 例: `_pendingBackbufferBgName`, `_pendingBackbufferBgParam`
2. `DrawBackgroundToBackBuffer` は要求を更新（最後に呼ばれた内容を採用）。
3. `End()` の `_currentTargetName == null` 分岐で、`_virtualScreen` 転送前に要求背景を描画。
4. 背景描画後、既存どおり `_virtualScreen` を `destRect` へ転送。
5. `End()` 終了時に要求状態をクリア（毎フレーム明示呼び出し式）。

## 6. 描画仕様（提案）
- 拡大方式: 既存 `DrawBackground` と同じ Aspect Fill。
- 基準サイズ: バックバッファ実サイズ（`GraphicsDevice.PresentationParameters.BackBufferWidth/Height`）。
- 色/反転: `TonDrawParam` を適用。
- 失敗時: 画像未登録なら `Ton.Log.Warning` を出して何もしない。

## 7. 影響範囲
- 実装:
  - `mononotonka/TonGraphics.cs`
- 文書:
  - `manual/class_tongraphics.html` に新メソッド説明と利用例を追記
- 非対象:
  - 既存 `DrawBackground` のシグネチャ・挙動変更
  - `Ton.cs` の描画ループ構造変更

## 8. 検証項目
- ケース1: 新API未使用時、既存描画結果が変わらない。
- ケース2: 新API使用時、バックバッファ全体に背景が出る。
- ケース3: レターボックスあり解像度で、余白領域にも背景が表示される。
- ケース4: スクリーンフィルター有効時でも例外なく描画される。
- ケース5: 画像未登録時にクラッシュしない（Warningログのみ）。

## 9. 品質ゲート
- `dotnet build Mononotonka.csproj -v minimal` を実行し、0 warning / 0 error を確認。

## 10. 受け入れ基準
- 新APIが public で追加され、Scene.Draw から呼べること。
- バックバッファ背景描画が `_virtualScreen` 転送より前に実行されること。
- manual に新API説明が追加されること。
