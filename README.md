# STS2_oekaki_patch

Slay the Spire 2 のマップ描画を拡張する Windows 向け MOD。

ゲームバージョン v0.104.0 (commit dc286199, 2026-04-23) で動作確認。

## MOD の概要

- マップ画面の消しゴム幅を変更できます。
- 鉛筆の太さと色を変更できます。
- ローカル描画を 1 つ戻す Undo を追加します。
- マップ画面に小さなツールバーを表示します。
- 鉛筆/消しゴムの現在範囲をカーソル位置に円で表示します。

現時点ではローカル表示のみ対応です。マルチプレイ相手への太さ、色、Undo の同期は未実装です。

## 基本操作

ツールバーはマップ画面で自動表示されます。ドラッグで好きな位置へ移動できます。

| 操作 | 効果 |
|---|---|
| ツールバーの Eraser スライダー | 消しゴム幅を変更 |
| ツールバーの Pencil スライダー | 鉛筆幅を変更 |
| 色ボタン | 鉛筆色を変更 |
| `D` 色ボタン | ゲーム標準色へ戻す |
| `Undo` ボタン | ローカル描画を 1 つ戻す |
| `Ctrl + Shift + E` | ツールバー表示切替 |
| `Ctrl + Shift + L` | MOD ログ表示切替 |

マップ画面では次のショートカットも使えます。

| キー | 効果 |
|---|---|
| `[` | 消しゴム幅を縮小 |
| `]` | 消しゴム幅を拡大 |
| `\` | 消しゴム幅を 1.0x に戻す |
| `Shift + [` | 鉛筆幅を縮小 |
| `Shift + ]` | 鉛筆幅を拡大 |
| `Shift + \` | 鉛筆幅を 1.0x に戻す |
| `Ctrl + Z` | ローカル描画を 1 つ戻す |

倍率は `0.5x` ステップで `0.5x` から `12.0x` まで変更できます。消しゴムのデフォルトは `3.0x`、鉛筆のデフォルトは `1.0x` です。

設定ファイル: `%LOCALAPPDATA%\MegaCrit\SlayTheSpire2\EraserMod\config.json`

ログ: `%LOCALAPPDATA%\MegaCrit\SlayTheSpire2\EraserMod\log.txt`

## インストール方法

1. GitHub Releases から `STS2_oekaki_patch-v0.0.1.zip` をダウンロードします。
2. zip を任意のフォルダへ展開します。
3. Slay the Spire 2 を終了します。
4. PowerShell で展開先フォルダを開きます。
5. 次のコマンドを実行します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install.ps1
```

Steam 以外の場所にインストールしている場合は、ゲームフォルダを指定します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -GameDir "C:\path\to\Slay the Spire 2"
```

`install.ps1` は `sts2.dll.orig` をバックアップとして作成し、`sts2.dll` に MOD 読み込み処理を注入します。バックアップがある場合はそれを元に再注入するため、同じフォルダで再実行できます。

## アンインストール方法

Slay the Spire 2 を終了してから、PowerShell で次を実行します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\uninstall.ps1
```

Steam 以外の場所にインストールしている場合:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\uninstall.ps1 -GameDir "C:\path\to\Slay the Spire 2"
```

`uninstall.ps1` は `sts2.dll.orig` から `sts2.dll` を復元します。

## 注意

- ゲーム更新、Steam の整合性チェック、再インストールでパッチが上書きされる場合があります。その場合は MOD を再インストールしてください。
- ゲーム本体の内部実装が変わると、この MOD は動作しなくなる可能性があります。
- `sts2.dll` や `refs/*.dll` など、ゲーム本体由来の DLL は配布物に含めていません。

## 免責

この MOD は非公式です。導入、利用、アンインストール、ゲーム更新後の再導入によって発生した不具合、データ損失、ゲームの起動不能、その他あらゆる損害について、作者は責任を負いません。自己責任で使用してください。
