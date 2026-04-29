# TODO: Visual Paint UI for Map Drawing

詳細は `plan.md` 参照。Phase 0 の調査結果が Phase 6 設計を決定。

## Phase 0: 調査（先行）
- [x] **Task 0-1** ★ マルチプレイ protocol 調査 issue (`tasks/research-multiplayer.md`) — M
  - 結論 A=独自 message。実機検証は Phase 6 前に別途必要

## Phase 1: Foundation
- [x] **Task 1** Config JSON 化 + マルチ設定項目化 — S
- [x] **Task 2** ツールバー UI スケルトン — M
- [x] **Checkpoint A**: build OK / config 移行 / hotkey 互換 / パネル表示

## Phase 2: Visual Eraser
- [x] **Task 3** 消しゴムサイズスライダ — S
- [x] **Task 4** カーソルプレビュー円 — M
- [x] **Checkpoint B**: スライダ・hotkey・プレビュー同期

## Phase 3: Pencil Width + Color (ローカル)
- [x] **Task 5** 鉛筆太さ・色 patch — S
- [x] **Task 6** 鉛筆太さスライダ + Shift hotkey — S
- [x] **Task 7** カラーパレット UI — S
- [x] **Checkpoint C**: ローカルで太さ・色反映、config 永続化

## Phase 4: Stamps (廃止)
- [x] **Task 8-10** スタンプ / Circle / Rect はスコープ外へ戻し — S
- [x] **Checkpoint D**: ツールバーは倍率・色・Undo のみ表示

## Phase 5: Undo (ローカル)
- [x] **Task 11** Undo stack + Ctrl+Z — S
- [x] **Task 12** Undo ボタン UI — XS
- [x] **Checkpoint E**: hotkey + ボタンで undo

## Phase 6: Multiplayer Sync (Phase 0 結論依存)
- [ ] **Task 13** 独自 NetMessage 設計 (A の場合) — L (要分割)
- [ ] **Task 14** 鉛筆スタイル peer 反映 — M
- [ ] **Task 15** Undo peer 反映 — M
- [ ] **Checkpoint F**: 2 クライアント実機 + vanilla 混在で動作

## Phase 7: Polish
- [ ] **Task 16** README / CLAUDE.md 更新 — XS
  - README は local-only 実装分のみ更新済み。CLAUDE.md と multiplayer 要件は Phase 6 後
- [ ] **Checkpoint G**: 全シナリオ + uninstall 復元確認

## Open Questions（実装前に解消）
- [x] マルチ同期方式 = A 独自 INetMessage
- [ ] vanilla peer 受信挙動の確認手段
- [x] Undo スコープ = 自分のみ
- [x] ツールバー位置 = 右下
- [x] `config.json` schema versioning 戦略 = Schema 1

## Verification Notes
- [x] `dotnet build src/EraserMod -c Release`
- [x] `dotnet build src/Injector -c Release`
- [ ] STS2 実機で UI / 描画 / hotkey / uninstall 復元を確認
