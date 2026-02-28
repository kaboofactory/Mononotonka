# Mononotonka Config BGM/SE Volume 実装計画

## 1. 目的
- `TonConfigMenu` から、`Master Volume` に加えて `BGM Volume` と `SE Volume` を調整可能にする。
- `TonSound` に「BGM全体係数」「SE全体係数」を追加し、ゲーム全体の音量バランス調整を容易にする。

## 2. 背景
- 現状は `SetMasterVolume` が中心で、専用の全体BGM/SEボリューム設定APIがない。
- 再生時引数やロード時 `baseVolume` はあるが、プレイヤー設定として「全体BGMだけ下げる」「全体SEだけ上げる」がやりづらい。

## 3. 適用範囲
- 実装対象:
  - `mononotonka/TonSound.cs`
  - `mononotonka/TonConfigMenu.cs`
  - `manual/class_tonsound.html`
  - `manual/class_tonconfigmenu.html`
- 非対象:
  - 個別SE/BGMごとの永続設定
  - 既存 `baseVolume` 設計の変更
  - ミキサーや外部OS音量との連携

## 4. 仕様（確定案）
1. `TonSound` に以下を追加する。
   - `SetBGMVolume(float volume)`
   - `GetBGMVolume()`
   - `SetSEVolume(float volume)`
   - `GetSEVolume()`
2. 音量は `0.0f - 1.0f` にクランプする。
3. 実効音量計算は乗算方式とする。
   - BGM: `bgmFadeVolume * masterVolume * bgmVolume * baseVolume`
   - SE: `playArgVolume * masterVolume * seVolume * baseVolume`
4. `TonConfigMenu` に `BGM Volume` / `SE Volume` 項目を追加する。
5. `config.json` に `BgmVolume` / `SeVolume` を保存する。
6. 旧 `config.json`（新項目なし）では既定値 `1.0f` として動作させる。

## 5. 実装タスク
1. `TonSound` 実装
   - フィールド追加: `_bgmVolumeScale`, `_seVolumeScale`（初期値 `1.0f`）
   - API追加: Set/Get BGM/SE volume
   - `ApplyCurrentBgmVolume` にBGM係数を反映
   - `PlaySE` とフォールバックSEにSE係数を反映
2. `TonConfigMenu` 実装
   - `_items` に `BGM Volume`, `SE Volume` を追加
   - `ChangeSetting` と `Draw` の値表示を追加
   - `ConfigData` に `BgmVolume`, `SeVolume` を追加
   - `Initialize` 読込時適用、`Close` 保存時書込
3. manual 更新
   - `class_tonsound.html`: 新規4APIを追記
   - `class_tonconfigmenu.html`: 設定項目説明を更新

## 6. 検証項目
- ケース1: `BGM Volume` 変更でBGMのみ音量が変化する。
- ケース2: `SE Volume` 変更でSEのみ音量が変化する。
- ケース3: `Master Volume` と組み合わせ時、乗算結果どおりに変化する。
- ケース4: 旧 `config.json` でも起動時例外が発生しない。
- ケース5: 設定変更後、再起動して値が保持される。
- ケース6: `Mute In Background` との併用で挙動が破綻しない。

## 7. 品質ゲート
- `dotnet build Mononotonka.csproj -v minimal` を実行し、0 warning / 0 error を確認する。

## 8. 受け入れ基準
- `TonSound` で BGM/SE 全体ボリュームのSet/Get APIが利用できること。
- `TonConfigMenu` で `Master/BGM/SE` の3種ボリュームを変更できること。
- `config.json` に `BgmVolume` / `SeVolume` が保存されること。
- manual にAPI/設定項目が反映されていること。
