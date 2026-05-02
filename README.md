# STS2_oekaki_patch

マップに描いた線を消す消しゴムの太さ、鉛筆の太さ・色、Undo を調整できるようにする MOD。

ゲームバージョン v0.104.0 (commit dc286199, 2026-04-23) で動作確認。

## 仕組み

- ゲーム本体は Godot 4 + .NET (C#)。`0Harmony.dll` と `MonoMod` が同梱されているので、Harmony で実行時パッチを当てる方式。
- ゲーム公式の MOD ローダー (`MegaCrit.Sts2.Core.Modding.ModManager`) 経由でロードする。`<game>/mods/EraserMod/` に `EraserMod.dll` と `manifest.json` を置き、`Bootstrap` クラスの `[ModInitializer("Init")]` から `Harmony.PatchAll()` を呼ぶ。**`sts2.dll` には触らない**。
- パッチ対象は主に `NMapDrawings.BeginLineLocal` / `CreateLineForPlayer(Player, bool isErasing)` で、ローカル描画だけを拡張する。
- マルチプレイ同期は未実装。色・太さ・Undo は現時点ではローカル表示のみ。

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

```powershell
.\install.ps1                # 既定の Steam パスを想定
.\install.ps1 -GameDir "..."

.\uninstall.ps1              # mods/EraserMod/ を削除
```

導入後、ゲームの設定で **MOD 機能を ON** にする必要がある (初回のみ警告ダイアログで同意)。

`install.ps1` は EraserMod を `<game>/mods/EraserMod/` にコピーするだけで `sts2.dll` を改変しない。ゲームをアップデートしても基本壊れず、Steam の整合性チェックの影響も受けない。`NMapDrawings` のメソッド名・シグネチャがゲーム更新で変わった場合のみ `decompiled/` を再生成して再ビルドが必要。

旧 Injector 方式 (v0.0.1) で導入していた場合は、`install.ps1` 実行時に自動で `sts2.dll` を `sts2.dll.orig` から復元し、旧 `EraserMod.dll` を `data_sts2_windows_x86_64/` から取り除く。

## ファイル構成

```
STS2_oekaki_patch/
├── src/
│   ├── EraserMod/         Harmony パッチ DLL
│   │   ├── Bootstrap.cs       [ModInitializer] エントリポイント / ログ
│   │   ├── manifest.json      ModManager 用マニフェスト
│   │   ├── Config.cs          JSON 設定の永続化
│   │   ├── HotkeyHandler.cs   hotkey 処理
│   │   ├── Toolbar.cs         ツールバー UI
│   │   ├── CursorPreview.cs   消しゴムプレビュー
│   │   ├── UndoStack.cs       ローカル Undo
│   │   └── Patches.cs         Harmony パッチ定義
│   └── Injector/          (legacy) sts2.dll への IL 注入。現方式では不要
├── refs/                  ビルド時参照する DLL コピー
├── decompiled/            ILSpy で展開した解析用ソース（参考）
├── install.ps1
└── uninstall.ps1
```
