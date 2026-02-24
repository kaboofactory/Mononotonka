# Mononotonka ラッパーAPI拡張 実装計画

## 1. 目的
- MonoGame直実装を減らすため、Mononotonka側で不足している実用APIを最小構成で補完する。
- 対象は以下の4点に限定する。
  - `TonSound` の停止系安全化の仕上げ
  - 拡大・縮小表示時のカーソル位置逆変換API
  - デバイス最大テクスチャサイズ取得・保持API
  - `MosaicSize` 記述の削除

## 2. 適用範囲
- 実装対象:
  - `mononotonka/TonSound.cs`
  - `mononotonka/TonGraphics.cs`
  - `mononotonka/TonGraphicsDef.cs`
- 文書対象:
  - `manual/class_tonsound.html`
  - `manual/class_tongraphics.html`
- 非対象:
  - 画面全体/部分フィルタの新規仕様追加
  - `TonInput`/`TonScene` の機能追加
  - `Mosaic` フィルタ自体の新実装

## 3. 背景（現状）
- `TonSound` は安全化が進んでいるが、状態参照APIが不足している。
- マップ拡縮時に「仮想画面座標 -> テクスチャ座標」を求める公開APIがない。
- 最大テクスチャサイズは `TonSpec` でログ出力しておらず、利用側が判定できない。
- `TonDrawParamEx.MosaicSize` は未使用で、現状 no-op。

## 4. 仕様（確定）

### 4.1 TonSound 停止系安全化の仕上げ
- 既存挙動は維持し、未再生時停止を no-op とする現仕様を固定化する。
- 公開APIとして再生状態を取得できるようにする。
- API案:
  - `bool IsBGMPlaying()`

### 4.2 座標逆変換API（マップ拡縮用途）
- `DrawEx` 相当のパラメータから、仮想画面座標が指すテクスチャ座標を逆算する。
- 対象外判定（描画外/切り出し外）は `false` を返す。
- API案（TonGraphics）:
  - `bool TryGetTexturePointFromDrawEx(Vector2 virtualPoint, float toX, float toY, int fromX, int fromY, int w, int h, TonDrawParamEx param, out Point texturePoint)`
- 計算対象:
  - `ScaleX/ScaleY`, `Angle`, `FlipH/FlipV`, 中心原点（`DrawEx` と同じ）

### 4.3 最大テクスチャサイズ取得・保持API
- 実行中デバイスで利用可能な最大テクスチャ寸法を取得し、内部キャッシュする。
- 初回取得時に計算し、以後はキャッシュ返却とする。
- 失敗時は GraphicsProfile 由来の安全値へフォールバックする。
- API案（TonGraphics）:
  - `int GetMaxTextureSize()`

### 4.4 MosaicSize 記述削除
- `TonDrawParamEx.MosaicSize` を削除する。
- 関連マニュアル記述があれば削除する。

## 5. 実装方針
1. `TonSound`
   - `IsBGMPlaying()` を追加し、内部状態 `_isBgmPlaying` を返す。
   - 既存停止処理の分岐を崩さず、回帰を防ぐ。
2. `TonGraphics`（座標逆変換）
   - `DrawEx` と同じ変換行列を組み立て、逆行列で `virtualPoint` をローカル座標へ戻す。
   - ローカル座標が `0 <= x < w`, `0 <= y < h` のときのみ `texturePoint` を算出。
   - `texturePoint = (fromX + floor(localX), fromY + floor(localY))` とする。
3. `TonGraphics`（最大テクスチャ）
   - フィールド例: `_cachedMaxTextureSize`（未計算時は 0）。
   - 内部プローブ（作成テスト）で最大値を探索し、`Dispose` を徹底する。
   - プローブ失敗時は `GraphicsProfile.Reach=2048`, `HiDef=4096` を返す。
4. `TonGraphicsDef`
   - `MosaicSize` フィールドとドキュメントコメントを削除する。
5. `manual`
   - `TonSound` に `IsBGMPlaying()` を追記。
   - `TonGraphics` に上記2APIの説明と最小使用例を追記。
   - `MosaicSize` 記述があれば削除。

## 6. 検証項目
- ケース1: BGM未再生時に `StopBGM()` 呼び出しで例外が出ない。
- ケース2: `IsBGMPlaying()` が再生中 true / 停止後 false を返す。
- ケース3: 拡縮マップ中央付近を指したとき、期待するテクスチャ座標が返る。
- ケース4: 描画外クリック時に `TryGetTexturePointFromDrawEx` が `false` を返す。
- ケース5: `GetMaxTextureSize()` を複数回呼んでも同値を返し、再計算しない。
- ケース6: `MosaicSize` 参照コードがビルドエラーなく解消される。

## 7. 品質ゲート
- `dotnet build Mononotonka.csproj -v minimal` を実行し、0 warning / 0 error を確認する。

## 8. 受け入れ基準
- 本計画の「4. 仕様（確定）」を満たすこと。
- 既存の `DrawEx` 表示結果・`TonSound` 再生/停止挙動に回帰がないこと。
- manual に新規APIが反映され、`MosaicSize` 記述が除去されていること。
