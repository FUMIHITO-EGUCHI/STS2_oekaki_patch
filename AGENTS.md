# AGENTS.md

@./.agents/skills/genshijin/SKILL.md

## Project
Slay the Spire 2 のマップ描画消しゴム幅を調整できるようにする Windows 向け MOD。

## Goal
ゲーム本体の挙動を最小限の Harmony / IL パッチで拡張し、マップ画面の消しゴムを使いやすくする。

重視する点:
1. ゲーム更新後に壊れた箇所を追いやすいこと
2. 元 DLL を復元できること
3. 変更範囲が小さく、失敗時の原因を切り分けやすいこと

## Current Scope
### In scope
- `src/EraserMod/`: Harmony パッチ DLL
- `src/Injector/`: `sts2.dll` の `<Module>.cctor` に bootstrap 呼び出しを注入するツール
- `install.ps1` / `uninstall.ps1`: DLL 配置、バックアップ、復元
- `README.md`: 対応ゲームバージョン、ビルド、導入手順
- `decompiled/`: ILSpy 等で展開した解析用コード。基本は参照専用
- `refs/`: ビルド参照用 DLL。配布物としては扱わない

### Out of scope
- ゲーム本体の再配布
- `decompiled/` の大規模編集
- STS2 の UI / アセット差し替え
- 既存セーブデータや設定ファイルへの破壊的変更

## Architecture Notes
- ゲーム本体は Godot 4 + .NET / C#。
- Runtime patch は Harmony を使い、`EraserMod.Bootstrap.Init()` から `Harmony.PatchAll()` する。
- Injector は dnlib で `sts2.dll` を書き換える。`sts2.dll.orig` が復元元になる。
- 設定は `%LOCALAPPDATA%\MegaCrit\SlayTheSpire2\EraserMod\config.txt`。
- ログは同フォルダの `log.txt`。
- ゲーム更新でメソッド名、シグネチャ、アセンブリ構成が変わる可能性がある。挙動不良時はまず `decompiled/` と `src/EraserMod/Patches.cs` の対象を照合する。

## Working Rules
- feature branch 前提で作業する。着手時は `sh scripts/start-issue.sh <type> <number> <topic>` で branch を作成する。
- branch 命名は `<type>/<number>-<topic>`（例: `fix/13-erasure-width`）。Issue を使わない雑務は `chore/skip-<topic>`。
- `master` / `main` への直接 commit は hook が拒否する。メンテナンス時のみ `ALLOW_MASTER_COMMIT=1` で override する。
- commit message には `#<issue>` を含める。Issue 対象外の雑務は `[skip-issue]` を含める。
- PR 本文には可能なら `Closes #<number>` を含める。
- `git push` はユーザーから明示指示がある場合のみ行う。
- `.gitignore` 対象の `bin/`、`obj/`、`refs/*.dll`、`decompiled/` は原則 commit しない。
- 既存のユーザー変更を巻き戻さない。未コミット差分がある場合は、作業前後に差分範囲を確認する。

## Verification
実装変更時は、影響範囲に応じて次を確認する。

- `dotnet build src/EraserMod -c Release`
- `dotnet build src/Injector -c Release`
- `install.ps1` / `uninstall.ps1` に触れた場合は PowerShell 構文とパス処理を確認する
- ゲーム挙動に触れた場合は、可能なら対象 STS2 バージョンで手動確認する
- README に記載した対応ゲームバージョンやホットキーが実装と一致していることを確認する

## GitHub Issue Operations
- タスク管理は GitHub Issues を single source of truth とする。
- Issue 作成時は `.github/ISSUE_TEMPLATE/` の `Task` / `Bug` / `Investigation` を使う。
- 作業完了時は Issue コメントに `Result`、`Verification`、`Changed files` を簡潔に残す。
- close は原則人間が行う。AI は完了条件を満たした根拠を提示する。

## Local Skills
- Codex の既定会話スタイルは `genshijin` 通常モードとする。通常の説明、進捗報告、質疑応答は簡潔な原始人スタイルを優先する。
- 例外: セキュリティ上重要な説明、脆弱性説明、破壊的操作確認、権限昇格確認、事故につながる可能性がある指示は通常日本語で明瞭に書く。
- 例外場面を抜けたら `genshijin` 通常モードへ戻す。
- ユーザーが `原始人`、`genshijin`、`短く`、`簡潔に`、`トークン節約` を明示したら `.agents/skills/genshijin/SKILL.md` を使う。
- ユーザーが `原始人やめて` または `通常モード` を明示したら、そのターンでは通常日本語へ戻す。

## Decision Priorities
1. 復元可能性
2. 対応ゲームバージョンでの安定動作
3. パッチ対象の追跡しやすさ
4. 実装の単純さ
5. 導入手順の分かりやすさ

## Things To Avoid
- `sts2.dll` のバックアップなし上書き
- 対象メソッドの確認なしに Harmony patch を増やすこと
- `decompiled/` を正本として扱うこと
- ゲーム本体 DLL や参照 DLL を Git に追加すること
- 手元環境の絶対パスをソースやドキュメントに固定すること
