# Security Policy

## Reporting a Vulnerability

このプロジェクト（MOD ツール自体 / CI スクリプト / DLL 注入手順）に脆弱性を見つけた場合:

- **公開 Issue にしない**
- GitHub の [Private vulnerability reporting](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability) を使うか、リポジトリ所有者へ直接連絡

返信は best-effort（個人メンテ、SLA なし）。

## Scope

対象:

- `.github/workflows/` の権限設定・secret 取り扱い
- `scripts/` の hook / scan 処理
- `src/Injector/` の IL 注入処理（不正な DLL 読み込みにつながる欠陥）
- `install.ps1` / `uninstall.ps1` のパス処理・ファイル上書きロジック

対象外:

- `refs/*.dll` や `decompiled/` のゲーム本体の脆弱性（MegaCrit / Valve へ報告）
- `docs/` の記述ミス（通常 Issue で OK）

## Secrets

このリポジトリは以下の secret を使用する。値は各利用者が自分の repo に設定する:

- `CLAUDE_CODE_OAUTH_TOKEN` — `/install-github-app` で自動登録（AI ワークフロー用）
- `GITHUB_TOKEN` — Actions 既定

リポジトリ自体には secret を含めない。
