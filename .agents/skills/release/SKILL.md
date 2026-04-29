---
name: release
description: STS2_oekaki_patch のリリース手順を実行する（検証、バージョン確認、配布 zip 生成、GitHub Release 作成）
disable-model-invocation: true
argument-hint: "[version]"
---

# /release - リリース手順

引数としてバージョン番号を受け取る（例: `/release 0.1.0`）。
引数がない場合は、README と git tag を確認して次のバージョンをユーザーに確認する。

このプロジェクトは Slay the Spire 2 向け Windows MOD。ゲーム本体 DLL と `refs/*.dll` は配布物に含めない。

---

## 手順

### 1. 事前確認

- `git status --short --branch` でワーキングツリーが clean か確認する
- 未コミットの変更があればユーザーに通知して停止する
- 現在のブランチ名を確認する（`master` / `main` で直接作業していないこと）
- README の対応ゲームバージョン、ホットキー、設定パスが実装と一致するか確認する

### 2. ビルド検証

```powershell
dotnet build src/EraserMod -c Release
dotnet build src/Injector -c Release
```

エラーがあれば停止してユーザーに報告する。

`install.ps1` / `uninstall.ps1` に変更がある場合は PowerShell 構文も確認する。

```powershell
pwsh -NoProfile -Command { [scriptblock]::Create((Get-Content -Raw install.ps1)) > $null; [scriptblock]::Create((Get-Content -Raw uninstall.ps1)) > $null }
```

### 3. リリース内容の確認

以下を確認する。

```powershell
git log --oneline --decorate --max-count 30
git diff --stat master...HEAD
```

`master` が存在しない場合は `main` を基準にする。
含める変更、既知の制約、手動確認の有無をまとめる。

### 4. バージョン反映

必要に応じて README の対応ゲームバージョン、リリース対象バージョン、導入手順を更新する。

このリポジトリには `package.json` のような単一の version source はない。バージョンは git tag と GitHub Release 名で管理する。

### 5. 配布物の生成

`dist/` を作り直し、配布に必要なファイルだけを zip に含める。
`install.ps1` はリポジトリ相対の `src/.../bin/Release/...` を参照するため、zip 内も同じ相対配置を維持する。

含めるもの:

- `src/EraserMod/bin/Release/EraserMod.dll`
- `src/Injector/bin/Release/net8.0/` の実行に必要な出力一式
- `install.ps1`
- `uninstall.ps1`
- `README.md`

含めないもの:

- `sts2.dll`
- `refs/*.dll`
- `decompiled/`
- `bin/`
- `obj/`
- `*.orig`

例:

```powershell
$version = "X.Y.Z"
$dist = "dist\STS2_oekaki_patch-v$version"
Remove-Item -Recurse -Force dist -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force "$dist\src\EraserMod\bin\Release" | Out-Null
New-Item -ItemType Directory -Force "$dist\src\Injector\bin\Release\net8.0" | Out-Null
Copy-Item src\EraserMod\bin\Release\EraserMod.dll "$dist\src\EraserMod\bin\Release\"
Copy-Item src\Injector\bin\Release\net8.0\* "$dist\src\Injector\bin\Release\net8.0\" -Recurse
Copy-Item install.ps1,uninstall.ps1,README.md $dist
Compress-Archive -Path "$dist\*" -DestinationPath "dist\STS2_oekaki_patch-v$version.zip" -Force
```

zip 作成後、内容を確認する。

```powershell
tar -tf "dist\STS2_oekaki_patch-vX.Y.Z.zip"
```

### 6. リリースコミット

README などリリース用変更がある場合だけコミットする。
Issue 対象外の雑務なら `[skip-issue]` を含める。

```powershell
git add README.md
git commit -m "chore: release vX.Y.Z [skip-issue]"
```

変更がない場合はコミットしない。

### 7. タグ付け

```powershell
git tag vX.Y.Z
```

既存タグがある場合は停止してユーザーに確認する。

### 8. push 確認

push はユーザーから明示指示がある場合のみ実行する。

確認前に以下を報告する。

- 現在のブランチ
- 新バージョン
- タグ名
- zip パス
- 検証結果

承認後:

```powershell
git push
git push --tags
```

### 9. GitHub Release 作成

push 完了後、ユーザーの承認がある場合だけ GitHub Release を作成する。

```powershell
gh release create vX.Y.Z `
  --title "vX.Y.Z" `
  --notes "リリース内容をここに記載" `
  "dist\STS2_oekaki_patch-vX.Y.Z.zip"
```

成功したら GitHub Release URL をユーザーに報告する。

---

## 注意事項

- `git push --force` は使わない
- ゲーム本体 DLL、参照 DLL、逆コンパイル成果物は配布しない
- `sts2.dll.orig` が復元元なので、導入手順でバックアップなし上書きを許さない
- ゲーム更新後は `decompiled/` と `src/EraserMod/Patches.cs` の対象を照合してから release する
