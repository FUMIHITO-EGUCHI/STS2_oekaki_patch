# Contributing

個人メンテの MOD プロジェクト。受付方針は控えめ。

## 歓迎

- **Bug report** — Issue でどうぞ。再現手順と環境（OS / STS2 バージョン）を明記
- **ドキュメント改善** — typo / リンク切れ / 説明不足は PR 直接 OK
- **質問・議論** — Discussions または Issue（`type: investigation`）

## 要相談

- **新機能・workflow 追加** — 先に Issue を立てて方向性合意してから PR
- **既存設計の変更** — `docs/decisions/` に新規 ADR を追加して提案

無相談の大型 PR は close することがある。

## ローカル開発

```sh
# git hooks インストール（commit-msg + pre-commit）
sh scripts/setup-hooks.sh

# ビルド確認
dotnet build src/EraserMod -c Release
dotnet build src/Injector  -c Release

# セキュリティスキャン（gitleaks インストール済みの場合）
bash scripts/security-scan.sh --staged

# ラベル同期（node 18+ と gh CLI 必要）
node scripts/sync-labels.mjs
```

## Commit message

- 通常: `#<issue>` を含める（commit-msg フックが enforce）
- Issue 対象外の雑務: `[skip-issue]` を含める

## License

contribute された変更は本リポジトリのライセンスで配布される。
