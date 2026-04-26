# STS2_oekaki_patch

マップに描いた線を消す消しゴムの太さを調整できるようにする MOD。

ゲームバージョン v0.104.0 (commit dc286199, 2026-04-23) で動作確認。

## 仕組み

- ゲーム本体は Godot 4 + .NET (C#)。`0Harmony.dll` と `MonoMod` が同梱されているので、Harmony で実行時パッチを当てる方式。
- `EraserMod.dll`(Harmony パッチ) を `data_sts2_windows_x86_64/` に配置し、`sts2.dll` の `<Module>.cctor` に `Assembly.LoadFrom("EraserMod.dll").EraserMod.Bootstrap.Init()` を呼ぶ IL を 1 行差し込む。
- パッチ対象は `NMapDrawings.CreateLineForPlayer(Player, bool isErasing)` で、`isErasing == true` のときに `Line2D.Width *= 倍率` を掛ける。

## ホットキー（マップ画面で有効）

| Key | 効果 |
|-----|------|
| `[` | 消しゴム幅を縮小 |
| `]` | 消しゴム幅を拡大 |
| `\` | 倍率を 1.0x にリセット |

倍率は `0.5x` ステップで `0.5 ~ 12.0x` の範囲。デフォルトは **3.0x**。

設定ファイル: `%LOCALAPPDATA%\MegaCrit\SlayTheSpire2\EraserMod\config.txt`
ログ: 同フォルダの `log.txt`

## ビルド

```powershell
# 必要: .NET 8 SDK + .NET 9 SDK
dotnet build src/EraserMod -c Release
dotnet build src/Injector -c Release
```

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

.\uninstall.ps1              # sts2.dll.orig から復元
```

注意: Steam の整合性チェックでパッチが上書きされる場合があります。ゲームをアップデートした場合は再インストール、もしくは `sts2.dll` のクラス名が変わっていたら `decompiled/` を再生成して再ビルドが必要。

## ファイル構成

```
STS2_oekaki_patch/
├── src/
│   ├── EraserMod/         Harmony パッチ DLL
│   │   ├── Bootstrap.cs       Init/ログ
│   │   ├── Config.cs          倍率の永続化
│   │   ├── HotkeyHandler.cs   [/]/\ ホットキー
│   │   └── Patches.cs         Harmony パッチ定義
│   └── Injector/          sts2.dll への IL 注入
├── refs/                  ビルド時参照する DLL コピー
├── decompiled/            ILSpy で展開した解析用ソース（参考）
├── install.ps1
└── uninstall.ps1
```
