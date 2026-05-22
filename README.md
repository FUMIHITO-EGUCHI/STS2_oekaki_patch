# STS2_oekaki_patch

マップに描いた線を消す消しゴムの太さ、鉛筆の太さ・色、Undo を調整できるようにする MOD。

ゲームバージョン v0.106.0 (commit cb2fbf47, 2026-05-21) で動作確認。

## 仕組み

- ゲーム本体は Godot 4 + .NET (C#)。`0Harmony.dll` と `MonoMod` が同梱されているので、Harmony で実行時パッチを当てる方式。
- ゲーム公式の MOD ローダー (`MegaCrit.Sts2.Core.Modding.ModManager`) 経由でロードする。`<game>/mods/EraserMod/` に `EraserMod.dll` と `manifest.json` を置き、`Bootstrap` クラスの `[ModInitializer("Init")]` から `Harmony.PatchAll()` を呼ぶ。**`sts2.dll` には触らない**。
- パッチ対象は `NMapDrawings.BeginLineLocal` / `CreateLineForPlayer(Player, bool isErasing)` / `Initialize` / `_ExitTree`。
- マルチプレイ対応: 接続時ハンドシェイクで MOD 保持 peer を検出し、描画開始時に色・太さを通知、Ctrl+Z 時は Undo も peer へ送信。vanilla peer との混在も可（下記参照）。

## ホットキー（マップ画面で有効）

| Key | 効果 |
|-----|------|
| `[` | 消しゴム幅を縮小 |
| `]` | 消しゴム幅を拡大 |
| `\` | 倍率を 1.0x にリセット |
| `Shift + [` | 鉛筆幅を縮小 |
| `Shift + ]` | 鉛筆幅を拡大 |
| `Shift + \` | 鉛筆幅を 1.0x にリセット |
| `Ctrl + Z` | ローカル描画を 1 つ Undo |
| `Ctrl + Shift + E` | ツールバー表示切替 |
| `Ctrl + Shift + L` | MOD ログ表示切替 |

倍率は `0.5x` ステップで `0.5 ~ 12.0x` の範囲。消しゴムのデフォルトは **3.0x**、鉛筆のデフォルトは **1.0x**。

設定ファイル: `%LOCALAPPDATA%\MegaCrit\SlayTheSpire2\EraserMod\config.json`
ログ: 同フォルダの `log.txt`

`config.txt` が残っている場合は初回起動時に `config.json` へ移行する。

`config.json` のデフォルト構造:

```json
{
  "Schema": 1,
  "EraserMultiplier": 3.0,
  "PencilMultiplier": 1.0,
  "PencilColorHex": null,
  "SelectedTool": "Pencil",
  "ToolbarVisible": true
}
```

`PencilColorHex` は `null` でキャラクターのデフォルト色。`#RRGGBB` 形式で指定可能。

## マルチプレイ

- MOD peer 同士: 接続時ハンドシェイク後、描画開始のたびに色・太さを通知。相手の描画に反映。
- **vanilla peer 混在**: EraserMod 未導入の peer とも接続できる。EraserMod peer の描画は vanilla 側ではデフォルト幅・色で表示される（MOD の通知パケットは vanilla クライアントで無視される）。Ctrl+Z の Undo 通知も vanilla peer には届かない。
- 全 peer に EraserMod の導入を強制はしない。

## ビルド

```powershell
# 必要: .NET 8 SDK + .NET 9 SDK
dotnet build src/EraserMod -c Release
dotnet build src/Injector -c Release  # legacy、ModManager 経由ロードでは不要
```

`src/Injector/` は旧 sts2.dll 改造方式の名残で、現方式 (ModManager 経由) では使用しない。pre-commit hook と整合のため暫定保持しているが、別 issue で撤去予定。

## Git 運用

Issue 起点の feature branch 運用を前提にしています。

```powershell
# 初回のみ Git hook をインストール
& "C:\Program Files\Git\bin\sh.exe" scripts/setup-hooks.sh

# Issue #12 の作業ブランチを作成
& "C:\Program Files\Git\bin\sh.exe" scripts/start-issue.sh fix 12 eraser-width
```

- branch 名は `feature|fix|refactor|docs|chore/<issue番号>-<topic>`、雑務のみ `chore/skip-<topic>`。
- commit message には `#<issue番号>`、雑務のみ `[skip-issue]` を含める。
- pre-commit hook は `dotnet build src/EraserMod -c Release` と `dotnet build src/Injector -c Release` を実行します。

## インストール / アンインストール

### 通常手順（エンドユーザー向け）

1. [GitHub Releases](https://github.com/FUMIHITO-EGUCHI/STS2_oekaki_patch/releases) から最新の zip をダウンロードして展開する
2. `install.bat` をダブルクリック（デフォルト Steam パスへ自動インストール）  
   または **ゲームフォルダを `install.bat` にドラッグ＆ドロップ**（非標準パスの場合）
3. ゲームの設定で **Mods: ON** にする（初回のみ確認ダイアログあり）

アンインストール: `uninstall.bat` を同様に実行（または ドラッグ＆ドロップ）

EraserMod は `<game>/mods/EraserMod/` に配置するだけで `sts2.dll` を改変しない。ゲームをアップデートしても Steam 整合性チェックの影響を受けない。

旧 Injector 方式 (v0.0.1) で導入していた場合は、`install.bat` 実行時に自動で `sts2.dll` を `sts2.dll.orig` から復元し、旧 `EraserMod.dll` を `data_sts2_windows_x86_64/` から取り除く。

### 開発者向け（ソースからビルドして導入）

```powershell
.\install.ps1                # dotnet build + コピー（デフォルト Steam パス）
.\install.ps1 -GameDir "..." # カスタムパス
.\uninstall.ps1              # mods/EraserMod/ を削除
```

## ライセンス

MIT License — 詳細は [LICENSE](LICENSE) を参照。

> `refs/` および `decompiled/` 配下はゲーム本体由来のファイルであり、本ライセンスの対象外。配布 zip にも含まれない。

## ファイル構成

```
STS2_oekaki_patch/
├── src/
│   ├── EraserMod/             Harmony パッチ DLL
│   │   ├── Bootstrap.cs           [ModInitializer] エントリポイント / ログ
│   │   ├── manifest.json          ModManager 用マニフェスト
│   │   ├── SupportedVersion.cs    対応ゲームバージョン定数（単一ソース）
│   │   ├── Config.cs              config.json の読み書き・マイグレ
│   │   ├── HotkeyHandler.cs       hotkey 処理
│   │   ├── Toolbar.cs             ツールバー UI
│   │   ├── CursorPreview.cs       鉛筆/消しゴムカーソル円プレビュー
│   │   ├── UndoStack.cs           ローカル Undo スタック
│   │   ├── MapReflection.cs       NMapDrawings 内部フィールドへのリフレクション
│   │   ├── ColorUtil.cs           #RRGGBB 解析ユーティリティ
│   │   ├── Toast.cs               一時メッセージ表示
│   │   ├── LogOverlay.cs          MOD ログオーバーレイ表示
│   │   ├── Patches.cs             Harmony パッチ定義（描画幅・色）
│   │   ├── NetPatches.cs          Harmony パッチ定義（MP ハンドラ登録・解除）
│   │   └── Net/
│   │       ├── NetSync.cs             INetGameService ラッパー・送信ヘルパー
│   │       ├── PeerStyleCache.cs      peer ごとのスタイルキャッシュ
│   │       ├── zEraserModHelloMessage.cs    接続時ハンドシェイク
│   │       ├── zEraserModLineStyleMessage.cs 描画前スタイル通知
│   │       └── zEraserModUndoMessage.cs     Undo 通知
│   └── Injector/              (legacy) sts2.dll への IL 注入。現方式では不要
├── refs/                      ビルド時参照する DLL コピー
├── decompiled/                ILSpy で展開した解析用ソース（参考）
├── install.ps1
└── uninstall.ps1
```
