# Implementation Plan: Visual Paint UI for Map Drawing

## Overview

現状の `[`/`]`/`\` キーのみの消しゴム倍率変更を、ペイントアプリ風の視覚 UI に拡張する。鉛筆の色・太さ、円/四角スタンプ、Undo (Ctrl+Z)、マルチプレイ peer 間同期まで対応する。

主要追加機能:
1. 視覚ツールバー（消しゴム/鉛筆/スタンプ切替、太さスライダ、色パレット）
2. 消しゴムカーソルプレビュー円
3. 鉛筆: 太さ倍率 + 色オーバーライド
4. スタンプ: 円・四角の Godot プリミティブ（PNG 不要）
5. Undo: Ctrl+Z でローカル & peer 双方の最後の描画を取消
6. マルチプレイ peer 間同期（要調査・後続 issue で確定）

## Architecture Decisions

- **マルチ同期は調査結果に依存**: 既存 `MapDrawingMessage` は `PacketWriter` バイナリ protocol、フィールド固定。色・太さ・スタンプ情報を流すには (a) 独自 `INetMessage` 実装を `_netService.RegisterMessageHandler<T>` で別 channel として流すか、(b) ローカルのみ。詳細は **Phase 0 調査 issue** で決定してから Phase 4-6 設計を確定する。
- **同 MOD 必須前提**: peer 同期するなら全員同じ MOD バージョン導入が必須。vanilla peer は MOD のメッセージを無視（disconnect させない設計）。
- **スタンプは Godot プリミティブ**: PNG 素材不要。`Node2D._Draw()` をオーバーライドして `DrawCircle` / `DrawRect`。サイズは PencilMultiplier 連動、色は PencilColor 連動。
- **Undo stack はプレイヤー毎**: `DrawingState.drawViewport.GetChildren()` の最後の `Node2D` を `QueueFree`。peer 同期する場合は `UndoMessage` を独自定義。
- **Config を JSON 化**: 設定項目増加のため `config.json` (`System.Text.Json`) に移行。旧 `config.txt` 自動取込。
- **UI レイヤ**: `_drawingToolHolder` 隣に独立 `Control` ツールバー。`Toast.cs` の Z-order 流用。
- **Harmony patch 追加対象**:
  - `CreateLineForPlayer` postfix → ローカル Line2D の Width / DefaultColor 上書き
  - `BeginLineLocal` prefix → スタンプモード時に line 生成 skip + プリミティブ Node2D 設置
  - `_Ready` postfix → ツールバー & プレビュー & Undo handler attach
  - 必要に応じ `Initialize` postfix → 独自 message handler 登録

## Constraints

- ゲーム更新でメソッド名・シグネチャ変更可能性 → 全 patch reflection + no-op fallback
- `sts2.dll.orig` 復元前提を崩さない
- `decompiled/` 編集禁止
- vanilla peer を強制 disconnect させない
- Godot 4 + .NET 8 / C#

---

## Task List

### Phase 0: 調査（先行）

#### Task 0-1: マルチプレイ protocol 調査 issue ★

**Description:** 独立 issue (`investigation`) を立てて以下を解析。実装前提の根拠資料を集める。本 plan の Phase 4-6 はこの結果に依存。

**調査項目:**
- `INetMessage` / `IPacketSerializable` の実装契約
- `_netService.RegisterMessageHandler<T>` での独自 message 登録可否
- 未知 packet ID を peer が受信したときの挙動（disconnect / 無視 / log）
- `MapDrawingMessage` の packet ID 採番方式（衝突回避）
- `NetTransferMode.Reliable` vs `Unreliable` の使い分け
- `ShouldBroadcast` / sender 判定 (`HandleDrawingMessage` の senderId 取得経路)
- `Player.Character.MapDrawingColor` がどこで決定されるか（ホスト/クライアント判定）
- `Sprite2D` / `Node2D` の `drawViewport` 直 add がネット同期で peer 側にも見えるか（おそらく見えない）
- vanilla peer + MOD peer 混在時の互換戦略

**Acceptance criteria:**
- [ ] `tasks/research-multiplayer.md` に上記すべての結論を記述
- [ ] 同期方式を確定: A=独自 message / B=ローカル限定 / C=ハイブリッド（描画はローカルのみ、Undo だけ既存 message で同期 等）
- [ ] vanilla peer 互換戦略を明記

**Verification:**
- [ ] 調査結果が peer 実機検証 1 件以上で裏付けされている（ホスト側のみ MOD で client が落ちないことを確認、または逆）

**Dependencies:** None（最初に着手）
**Files:** `tasks/research-multiplayer.md` (新規)
**Scope:** M (時間としては設計判断 + 実機検証)

---

### Phase 1: Foundation

#### Task 1: Config JSON 化 + マルチ設定項目化

**Description:** `config.txt` (float 1 行) を `config.json` (`System.Text.Json`) に移行。フィールド: `EraserMultiplier`, `PencilMultiplier`, `PencilColorHex`, `SelectedTool` (Pencil/Eraser/StampCircle/StampRect), `ToolbarVisible`, `Schema=1`。読込時に旧 `config.txt` あれば EraserMultiplier に取込 + 削除。

**Acceptance criteria:**
- [ ] `config.json` 読み書き、旧 fmt 自動マイグレ
- [ ] 不正値は `Math.Clamp` でデフォルト
- [ ] `WidthMultiplier` ラッパで既存 hotkey API 維持
- [ ] `Schema` フィールドで将来の破壊的変更に備え

**Verification:**
- [ ] `dotnet build src/EraserMod -c Release` 成功
- [ ] `dotnet build src/Injector  -c Release` 成功
- [ ] 旧 `config.txt` を仕込み起動 → `config.json` に移行 + 値継承を log 確認

**Dependencies:** None
**Files:** `src/EraserMod/Config.cs`
**Scope:** S

---

#### Task 2: ツールバー UI スケルトン

**Description:** `_drawingToolHolder` 隣に `Control` ツールバーを `AddChild`。空パネルのみ。`F9` でトグル、`AttachOnce` パターンで重複生成防止。

**Acceptance criteria:**
- [ ] マップ画面オープン時に表示
- [ ] `F9` トグル
- [ ] 画面遷移しても重複なし
- [ ] 子ノード追加用のクリック領域として機能

**Verification:**
- [ ] ビルド両方 OK
- [ ] マップで空パネル可視 → 別画面 → 戻る → 1 個だけ存在 (log)

**Dependencies:** Task 1
**Files:** `src/EraserMod/Toolbar.cs` (新規), `src/EraserMod/Patches.cs`
**Scope:** M

---

### Checkpoint A: Foundation
- [ ] 両 build OK
- [ ] config.json 移行成功
- [ ] 既存 hotkey 互換
- [ ] パネル表示／消失

---

### Phase 2: Visual Eraser

#### Task 3: 消しゴムサイズスライダ

**Description:** ツールバーに `HSlider` (Min=0.5, Max=12.0, Step=0.5) + ラベル `消しゴム: x3.00`。値変更で `Config.EraserMultiplier` 即時反映 + Save。

**Acceptance criteria:**
- [ ] スライダ操作で次の線から太さ変化
- [ ] `[`/`]`/`\` hotkey とスライダ位置同期
- [ ] ラベル F2 形式

**Verification:**
- [ ] ビルド両方 OK
- [ ] 線描画 → 太さ変化目視
- [ ] hotkey で動かして連動確認

**Dependencies:** Task 1, 2
**Files:** `src/EraserMod/Toolbar.cs`, `src/EraserMod/HotkeyHandler.cs`
**Scope:** S

---

#### Task 4: カーソルプレビュー円

**Description:** 消しゴムモード中、マウス位置にローカルのみの `Node2D` リング (`_Draw` で `DrawArc`)。半径 = 実効消しゴム幅 / 2。倍率変更で即追従。

**Acceptance criteria:**
- [ ] DrawingMode==Erasing のみ可視
- [ ] 倍率変更で半径追従
- [ ] モード解除で消失
- [ ] 画面外で非表示

**Verification:**
- [ ] ビルド両方 OK
- [ ] 消しゴム → 円カーソル → スライダで半径変化
- [ ] 鉛筆切替で消失

**Dependencies:** Task 3
**Files:** `src/EraserMod/CursorPreview.cs` (新規), `src/EraserMod/Patches.cs`
**Scope:** M

---

### Checkpoint B: Visual Eraser
- [ ] スライダ・hotkey・プレビューが同値で動作
- [ ] 画面遷移後の残骸なし

---

### Phase 3: Pencil Width + Color (ローカル)

#### Task 5: 鉛筆太さ・色 patch (ローカル先行)

**Description:** `CreateLineForPlayer` postfix で `isErasing==false` && local player のとき `Line2D.Width *= PencilMultiplier`。さらに `PencilColorHex != null` なら `DefaultColor = parsed`。同期は Phase 6 で対応。

**Acceptance criteria:**
- [ ] 自分の線のみ太さ・色変化
- [ ] 不正 hex は無視 (log warn)
- [ ] 消しゴムには影響なし

**Verification:**
- [ ] PencilMultiplier=2.0 で太く描ける
- [ ] PencilColorHex="#00FF00" で緑になる
- [ ] log で `pencil width: a -> b` 出力

**Dependencies:** Task 1
**Files:** `src/EraserMod/Patches.cs`
**Scope:** S

---

#### Task 6: 鉛筆太さスライダ + Shift hotkey

**Description:** ツールバーに鉛筆太さスライダ追加。`Shift+[` / `Shift+]` / `Shift+\` で同操作。

**Acceptance criteria:**
- [ ] スライダ即時反映
- [ ] Shift hotkey で同等操作
- [ ] 既存消しゴム hotkey と衝突なし

**Verification:**
- [ ] ビルド両方 OK
- [ ] スライダ + hotkey 双方で太さ変化

**Dependencies:** Task 5
**Files:** `src/EraserMod/Toolbar.cs`, `src/EraserMod/HotkeyHandler.cs`
**Scope:** S

---

#### Task 7: カラーパレット UI

**Description:** 8 色プリセット (白黒赤橙黄緑青紫) + 「キャラ既定」ボタン (`null` set)。クリックで `PencilColorHex` 即時更新、選択中ハイライト。

**Acceptance criteria:**
- [ ] 8 色 + リセット
- [ ] クリックで線色変化
- [ ] 選択状態が再起動後も保持

**Verification:**
- [ ] ビルド両方 OK
- [ ] 各色で線色目視確認
- [ ] 再起動後も最後の色が選択中

**Dependencies:** Task 5
**Files:** `src/EraserMod/Toolbar.cs`
**Scope:** S

---

### Checkpoint C: Pencil Local
- [ ] 鉛筆太さ・色がローカルで反映
- [ ] config 永続化

---

### Phase 4: Stamps (プリミティブ, ローカル)

#### Task 8: スタンプ Node2D プリミティブ

**Description:** `StampNode.cs` で `Node2D._Draw()` オーバーライド。`StampShape` (Circle/Rect) を引数に取り、PencilColor + PencilMultiplier ベースのサイズで描画。`Position` をクリック座標に設定。

**Acceptance criteria:**
- [ ] Circle / Rect 両方を描画可能
- [ ] サイズ = `8 * PencilMultiplier` 相当（要調整）
- [ ] 色 = PencilColor or キャラ既定
- [ ] `_Draw` で完結（テクスチャ不要）

**Verification:**
- [ ] テスト用に手動で `StampNode` を `drawViewport` に add → 表示確認

**Dependencies:** Task 5
**Files:** `src/EraserMod/StampNode.cs` (新規)
**Scope:** S

---

#### Task 9: スタンプ配置入力

**Description:** `BeginLineLocal` を prefix で intercept。`Config.SelectedTool == StampCircle/StampRect` のとき line 生成 skip + `StampNode` を `drawViewport` に AddChild。`QueueOrSendEvent` 呼ばない（マルチ同期は Phase 6 で対応）。

**Acceptance criteria:**
- [ ] スタンプツール選択中はクリックで Stamp 配置
- [ ] 鉛筆/消しゴム選択中は通常動作
- [ ] ネット側にメッセージ流れない (log で確認)
- [ ] `ClearMap` で消える

**Verification:**
- [ ] スタンプ選択 → クリック → スタンプ表示
- [ ] 鉛筆切替 → 線描画
- [ ] log で `QueueOrSendEvent` 未呼出を確認

**Dependencies:** Task 8
**Files:** `src/EraserMod/Patches.cs`, `src/EraserMod/StampPlacer.cs` (新規)
**Scope:** M

---

#### Task 10: ツール選択 UI

**Description:** ツールバーに 4 ボタン (鉛筆/消しゴム/円スタンプ/四角スタンプ)。クリックで `Config.SelectedTool` 更新 + 既存 `SetDrawingModeLocal(Drawing|Erasing)` を内部で呼ぶ（既存ボタンとの整合）。選択中ハイライト。

**Acceptance criteria:**
- [ ] 4 ツール切替
- [ ] 選択状態 UI 反映
- [ ] スタンプ選択時は内部 DrawingMode=Drawing 扱い (`BeginLineLocal` で intercept される)

**Verification:**
- [ ] 各ツールで期待動作
- [ ] 既存 NMapDrawButton/NMapEraseButton クリックでも UI 同期

**Dependencies:** Task 9
**Files:** `src/EraserMod/Toolbar.cs`, `src/EraserMod/Patches.cs` (`SetDrawingModeLocal` postfix で同期)
**Scope:** M

---

### Checkpoint D: Stamps Local
- [ ] 4 ツール切替・スタンプ配置・ClearMap で消える

---

### Phase 5: Undo (ローカル先行)

#### Task 11: Undo stack + Ctrl+Z

**Description:** `DrawingState.drawViewport.GetChildren()` の最後の `Node2D` (Line2D / StampNode) を `QueueFree` する関数を実装。ローカルプレイヤーの状態のみ対象。`Ctrl+Z` で発火。複数回押下で順次取消。

**Acceptance criteria:**
- [ ] Ctrl+Z で最後の描画が消える
- [ ] 連打で順次取消
- [ ] 線・スタンプ両対応
- [ ] 描画中（drag 中）は無視

**Verification:**
- [ ] 線 3 本引く → Ctrl+Z 3 回で全消去
- [ ] スタンプも同様
- [ ] 他プレイヤーの線は取消対象外（log）

**Dependencies:** Task 10
**Files:** `src/EraserMod/HotkeyHandler.cs`, `src/EraserMod/UndoStack.cs` (新規)
**Scope:** S

---

#### Task 12: Undo ボタン UI

**Description:** ツールバーに「Undo」ボタン追加。クリックで Ctrl+Z と同じ処理。

**Acceptance criteria:**
- [ ] ボタンクリックで undo
- [ ] hotkey と挙動一致

**Verification:**
- [ ] ボタン → 直前描画消去

**Dependencies:** Task 11
**Files:** `src/EraserMod/Toolbar.cs`
**Scope:** XS

---

### Checkpoint E: Undo Local
- [ ] Ctrl+Z + ボタン両方で undo

---

### Phase 6: Multiplayer Sync（Phase 0 結論依存）

> 結論 A = 独自 message なら以下 Task 13-15 を実装。
> 結論 B = ローカル限定なら Phase 6 全体スキップ + README に明記。
> 結論 C = ハイブリッドなら個別調整。

#### Task 13: 独自 NetMessage 設計（A の場合）

**Description:** `EraserModMessage : INetMessage, IPacketSerializable` を実装。subtype: `LineStyleAnnounce` (color hex + width multiplier), `StampPlace` (position + shape + color + size), `Undo` (target line/stamp index)。`_netService.RegisterMessageHandler` で送受信。

**Acceptance criteria:**
- [ ] 独自 packet が peer に届く
- [ ] vanilla peer が disconnect しない (Phase 0 で確認した受信挙動次第)
- [ ] Reliable / Unreliable を用途別に使い分け

**Verification:**
- [ ] 2 クライアント (両方 MOD) で peer 側に色付き線・スタンプ・undo が反映
- [ ] vanilla peer 1 + MOD peer 1 で MOD peer 側で例外出ない

**Dependencies:** Task 0-1, 5, 9, 11
**Files:** `src/EraserMod/Net/EraserModMessage.cs` (新規), `src/EraserMod/Patches.cs` (`Initialize` postfix で handler 登録)
**Scope:** L (要分割の可能性: 13a=Line, 13b=Stamp, 13c=Undo)

---

#### Task 14: 鉛筆スタイルの peer 反映

**Description:** ローカル側で `BeginLineLocal` の直前に `LineStyleAnnounce` を broadcast。peer 受信時、その playerId の次の `CreateLineForPlayer` 呼出に対して同じ Width / Color を適用。

**Acceptance criteria:**
- [ ] 自プレイヤーの色変更が peer 側にも見える
- [ ] 太さも同様

**Verification:**
- [ ] 2 クライアント実機で双方の線色・太さが一致

**Dependencies:** Task 13
**Files:** `src/EraserMod/Patches.cs`, `src/EraserMod/Net/PeerStyleCache.cs` (新規)
**Scope:** M

---

#### Task 15: スタンプ・Undo の peer 反映

**Description:** スタンプ配置を `StampPlace` で broadcast → peer 側で `StampNode` 生成。Undo を `Undo` で broadcast → peer 側でも同じ Node を `QueueFree`。

**Acceptance criteria:**
- [ ] 自スタンプが peer 画面に出る
- [ ] Undo が peer 側でも同期
- [ ] 順序ずれによる二重 free 等の例外なし

**Verification:**
- [ ] 2 クライアントで Stamp + Undo 同期
- [ ] log で `out of order` 等のエラー無し

**Dependencies:** Task 13, 14
**Files:** `src/EraserMod/Net/EraserModMessage.cs`, `src/EraserMod/Patches.cs`
**Scope:** M

---

### Checkpoint F: Multiplayer
- [ ] 2 クライアント実機で同期確認
- [ ] vanilla peer 混在シナリオの fallback 動作確認

---

### Phase 7: Polish

#### Task 16: ドキュメント更新

**Description:** `README.md` に新 hotkey (Ctrl+Z, Shift+brackets, F9)、ツールバー、`config.json` サンプル、マルチプレイ要件 (peer 全員 MOD 必要 等) を追記。`CLAUDE.md` Architecture 節に新ファイル追加。

**Acceptance criteria:**
- [ ] hotkey 表更新
- [ ] config.json サンプル
- [ ] マルチ要件明記

**Verification:**
- [ ] ドキュメント差分のみ

**Dependencies:** All
**Files:** `README.md`, `CLAUDE.md`
**Scope:** XS

---

### Checkpoint G: Complete
- [ ] STS2 v0.104.0 シングル + マルチで全シナリオ通る
- [ ] `uninstall.ps1` で `sts2.dll.orig` から完全復元
- [ ] PR レビュー完了

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| ゲーム更新で `CreateLineForPlayer` / `BeginLineLocal` シグネチャ変更 | High | reflection + no-op fallback、`decompiled/` 再生成手順を README に維持 |
| 独自 NetMessage が vanilla peer を切断させる | High | Phase 0 で先に検証。NG なら結論 B (ローカル限定) に倒す |
| packet ID 衝突 | Med | Phase 0 で採番方式を確認、被る場合は別 channel / 既存 message 再利用を検討 |
| `Line2D.DefaultColor` 上書きが peer 側で player 自身も変わる | Med | local player + own line のときだけ上書き、`HandleDrawingMessage` の `senderId` が local id でない場合は触らない |
| Godot ノードリーク | Med | `TreeExiting` で QueueFree、`AttachOnce` 徹底 |
| Undo の二重発火（Ctrl+Z 連打） | Low | `_prevCtrlZ` edge detect、最低 100ms cool-down |
| Stamp サイズが画面ズーム比で意図せず変わる | Low | `drawViewport` 座標系で `Position * 0.5f` のスケール係数を Line と揃える |

## Open Questions（実装前に解消）

1. **マルチ同期方式**: Phase 0 結果待ち（A/B/C）
2. **vanilla peer 受信時の挙動**: disconnect させずに無視させる手段の確認
3. **Undo のスコープ**: 自分の描画のみ vs 全プレイヤーの自分の最後の描画 → **自分のみ前提**
4. **ツールバー位置**: 右下 vs 左下 vs floating → **右下** デフォルト、要 UX レビュー
5. **`config.json` schema 1 で確定するか**: 将来追加項目でも `Schema=1` 維持で deserialize 互換取れる前提か → `JsonSerializerOptions.IgnoreReadOnlyProperties` 等で吸収

## Files Summary

新規:
- `src/EraserMod/Toolbar.cs`
- `src/EraserMod/CursorPreview.cs`
- `src/EraserMod/StampNode.cs`
- `src/EraserMod/StampPlacer.cs`
- `src/EraserMod/UndoStack.cs`
- `src/EraserMod/Net/EraserModMessage.cs` (Phase 6, A の場合)
- `src/EraserMod/Net/PeerStyleCache.cs` (Phase 6, A の場合)
- `tasks/research-multiplayer.md` (Phase 0 成果物)

変更:
- `src/EraserMod/Config.cs` (JSON 化, 新フィールド)
- `src/EraserMod/Patches.cs` (`CreateLineForPlayer` postfix, `BeginLineLocal` prefix, `_Ready` postfix, `SetDrawingModeLocal` postfix, `Initialize` postfix)
- `src/EraserMod/HotkeyHandler.cs` (Shift 修飾, Ctrl+Z, スライダ同期)
- `README.md`, `CLAUDE.md`

## GitHub Issue 構成案

- Issue (investigation): "Multiplayer net protocol survey for paint mod"
- Issue (feature): "Phase 1-2: visual eraser toolbar + cursor preview"
- Issue (feature): "Phase 3: pencil width + color (local)"
- Issue (feature): "Phase 4: primitive stamps (local)"
- Issue (feature): "Phase 5: undo (local)"
- Issue (feature): "Phase 6: multiplayer sync (depends on investigation)"
- Issue (docs): "Phase 7: docs update"

各 issue に対して `<type>/<number>-<topic>` branch + `#<number>` commit 規約で進める。
