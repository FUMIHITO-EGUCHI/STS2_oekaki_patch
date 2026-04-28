# TODO: Visual Paint UI for Map Drawing

詳細は `plan.md` 参照。Phase 0 の調査結果が Phase 6 設計を決定。

## Phase 0: 調査（先行）
- [ ] **Task 0-1** ★ マルチプレイ protocol 調査 issue (`tasks/research-multiplayer.md`) — M
  - 結論 A=独自 message / B=ローカル限定 / C=ハイブリッド を決定

## Phase 1: Foundation
- [ ] **Task 1** Config JSON 化 + マルチ設定項目化 — S
- [ ] **Task 2** ツールバー UI スケルトン — M
- [ ] **Checkpoint A**: build OK / config 移行 / hotkey 互換 / パネル表示

## Phase 2: Visual Eraser
- [ ] **Task 3** 消しゴムサイズスライダ — S
- [ ] **Task 4** カーソルプレビュー円 — M
- [ ] **Checkpoint B**: スライダ・hotkey・プレビュー同期

## Phase 3: Pencil Width + Color (ローカル)
- [ ] **Task 5** 鉛筆太さ・色 patch — S
- [ ] **Task 6** 鉛筆太さスライダ + Shift hotkey — S
- [ ] **Task 7** カラーパレット UI — S
- [ ] **Checkpoint C**: ローカルで太さ・色反映、config 永続化

## Phase 4: Stamps (プリミティブ, ローカル)
- [ ] **Task 8** スタンプ Node2D プリミティブ (円・四角) — S
- [ ] **Task 9** スタンプ配置入力 (`BeginLineLocal` prefix) — M
- [ ] **Task 10** ツール選択 UI (鉛筆/消しゴム/円/四角) — M
- [ ] **Checkpoint D**: 4 ツール切替・配置・ClearMap で消去

## Phase 5: Undo (ローカル)
- [ ] **Task 11** Undo stack + Ctrl+Z — S
- [ ] **Task 12** Undo ボタン UI — XS
- [ ] **Checkpoint E**: hotkey + ボタンで undo

## Phase 6: Multiplayer Sync (Phase 0 結論依存)
- [ ] **Task 13** 独自 NetMessage 設計 (A の場合) — L (要分割)
- [ ] **Task 14** 鉛筆スタイル peer 反映 — M
- [ ] **Task 15** スタンプ・Undo peer 反映 — M
- [ ] **Checkpoint F**: 2 クライアント実機 + vanilla 混在で動作

## Phase 7: Polish
- [ ] **Task 16** README / CLAUDE.md 更新 — XS
- [ ] **Checkpoint G**: 全シナリオ + uninstall 復元確認

## Open Questions（実装前に解消）
- [ ] マルチ同期方式 (Phase 0 待ち)
- [ ] vanilla peer 受信挙動の確認手段
- [ ] Undo スコープ = 自分のみ で確定？
- [ ] ツールバー位置 = 右下 で確定？
- [ ] `config.json` schema versioning 戦略
