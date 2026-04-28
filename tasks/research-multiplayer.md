# Multiplayer Net Protocol Survey for Paint Mod (Issue #4)

調査対象 STS2 バージョン: v0.104.0 (commit dc286199)
調査媒体: `decompiled/` の静的解析（実機検証は実装着手フェーズで補完）

## TL;DR — 結論

- **採用方針: A = 独自 INetMessage 実装** ただし以下の前提つき
  - **前提 1**: 現行 `Injector` 方式を **公式 ModManager loader 経由** に切替（または Reflection で `ModManager._mods` に手動登録）
  - **前提 2**: MOD 独自型の名前を **vanilla 既存全 INetMessage 型より string ordinal で大** にする（例: `~` プレフィックス）。さもなくば vanilla 型の packet ID が変動し protocol 互換が壊れる
  - **前提 3**: vanilla peer に対しては独自 packet を送らない（MOD 状態 handshake で peer の MOD 有無を判定して broadcast を抑制）
- **B / C は不採用**: 機能要求（peer 間で色・太さ・スタンプ・Undo を可視化）を満たせない
- vanilla peer は MOD packet を受信しても **disconnect しない**（`Log.Error` のみで無視）。互換戦略の弾力性は確保可能

---

## 1. INetMessage / IPacketSerializable 契約

`decompiled/MegaCrit.Sts2.Core.Multiplayer.Serialization/INetMessage.cs`:

```csharp
[GenerateSubtypes]
public interface INetMessage : IPacketSerializable {
    bool ShouldBroadcast { get; }
    NetTransferMode Mode { get; }
    LogLevel LogLevel { get; }
}
```

- `[GenerateSubtypes]` は source generator マーカ。`INetMessageSubtypes.All` を生成する（`MessageTypes.cs` で参照）
- `IPacketSerializable` は `Serialize(PacketWriter)` / `Deserialize(PacketReader)` の 2 メソッド契約
- 必須実装: 上記 3 プロパティ + 2 メソッド = 計 5 メンバ

## 2. RegisterMessageHandler の挙動

`decompiled/MegaCrit.Sts2.Core.Multiplayer/NetMessageBus.cs` L92-114:

```csharp
public void RegisterMessageHandler<T>(MessageHandlerDelegate<T> handler) where T : INetMessage {
    if (typeof(T) == typeof(INetMessage))
        throw new InvalidOperationException("...concrete implementation...");
    // typeof(T) -> List<CallbackPair> 辞書に追加
}
```

- 具象型のみ登録可（基底 interface 直接は不可）
- 同一型に複数 handler 登録可（`List<CallbackPair>`）
- `INetGameService.RegisterMessageHandler<T>` から経由できる（`NMapDrawings.Initialize` 内で `_netService.RegisterMessageHandler<MapDrawingMessage>(HandleDrawingMessage)` 等）
- 解除は `Unregister`、画面 ExitTree で対称的に解除する作法

## 3. 未知 packet ID 受信時の挙動

`NetMessageBus.TryDeserializeMessage` L40-55:

```csharp
byte b = _reader.ReadByte();
if (!MessageTypes.TryGetMessageType(b, out Type type)) {
    Log.Error($"Received message with first byte {b} that is not a valid message ID!");
    return false;
}
```

- 未知 ID → **`Log.Error` のみで return false。connection 維持**
- `disconnect` / `kick` のロジックなし
- → vanilla peer に MOD packet を送りつけても **強制切断にはならない**（互換戦略に有利）

## 4. Packet ID 採番方式 ★ 互換性の核心

`decompiled/MegaCrit.Sts2.Core.Multiplayer.Serialization/MessageTypes.cs`:

```csharp
static MessageTypes() {
    var obj = new List<Type>();
    obj.AddRange(INetMessageSubtypes.All);                     // vanilla source-generated
    obj.AddRange(ReflectionHelper.GetSubtypesInMods<INetMessage>()); // MOD assemblies
    _cache = new NetTypeCache<INetMessage>(obj);
}
```

`NetTypeCache.cs` L18:

```csharp
types.Sort((t1, t2) => string.CompareOrdinal(t1.Name, t2.Name));
_idToType = types;
for (int n = 0; n < types.Count; n++) _typeToId[types[n]] = n;
```

→ **全型を `string.CompareOrdinal(Type.Name, ...)` で alphabetical sort、配列 index がそのまま byte ID**。

**致命的影響:**

- MOD が vanilla 既存型の途中に挿入される名前を持つと、**vanilla 既存型の ID が全シフト**
- vanilla peer は MOD 型を持たないので元 ID で sort → MOD peer と vanilla peer で **同一 vanilla 型に異なる ID** が割り振られ互換崩壊

**回避策:**

- MOD 独自型の名前を **vanilla 既存全 `INetMessage` 名より string ordinal が大** にする
- ASCII 文字コードで `~` (0x7E) は大文字 `Z` (0x5A) や小文字 `z` (0x7A) より大、underscore (0x5F) や `}` (0x7D) より大
- 例: `~EraserModLineStyleMessage`, `~EraserModStampPlaceMessage`, `~EraserModUndoMessage`
- これで **vanilla 既存型の ID は不変**、MOD 型は配列末尾に並ぶ → vanilla peer の packet を MOD peer も同じ ID で解釈可能（vanilla→MOD 方向は問題なし）
- MOD→vanilla 方向は vanilla が MOD ID を知らないので Log.Error 無視（前述 §3）

## 5. NetTransferMode 使い分け

vanilla 用例:

| Message | Mode | 理由（推測） |
|---------|------|-------------|
| `MapDrawingMessage` | Unreliable | 描画途中の中間点ロス許容（`maxEventCount=15` でバッチ送信） |
| `MapDrawingModeChangedMessage` | Reliable（推測） | モード切替は飛ばすと致命的 |
| `ClearMapDrawingsMessage` | Reliable（推測） | 不可逆操作 |

→ MOD 設計指針:
- Line style annotate (色・太さ宣言) = **Reliable** (描画前 1 回限り、欠落不可)
- Stamp place = **Reliable**（補完不可能な離散イベント）
- Undo = **Reliable**

## 6. ShouldBroadcast / senderId 経路

`NetMessageBus.SerializeMessage` L33-34:

```csharp
_writer.WriteByte((byte)message.ToId());
_writer.WriteULong(senderId);
```

→ 全 packet の先頭 1 byte が type ID、続く 8 byte が senderId。

`HandleDrawingMessage(MapDrawingMessage msg, ulong senderId)` のように handler に senderId が渡される。
- `senderId` で peer 識別可能。MOD 側でも自分以外 = peer 用処理に分岐
- `ShouldBroadcast = true` → host が中継して全 peer に転送（host-authority topology）
- `ShouldBroadcast = false` → 1:1 もしくは host 専用

MOD 独自 message も `ShouldBroadcast = true` で全 peer 配布される想定。

## 7. Player.Character.MapDrawingColor 決定

`NMapDrawings.CreateLineForPlayer` L673-682:

```csharp
Line2D val2 = val.Instantiate<Line2D>();
val2.DefaultColor = player.Character.MapDrawingColor;
```

- `Player` は `_playerCollection.GetPlayer(state.playerId)` で取得
- `MapDrawingColor` は Character ScriptableObject のフィールド（`MegaCrit.Sts2.Core.Entities.*` 配下、static）
- ホスト/クライアントの差異なし。**全 peer で同じプレイヤーは同じ色**になる ＝ peer 識別に使われる UI 規約
- **MOD で local override しても peer 側では override 効かない**（peer 側 `CreateLineForPlayer` が走るので）→ 色を peer に伝えるには独自 message 必須

## 8. Sprite2D / Node2D の drawViewport ローカル add

- `drawViewport` は `SubViewport`、Godot レベルで自動同期されない
- `MapDrawingMessage` 経由でしか peer 間に描画が伝わらない
- ローカル `AddChild` した Node は **peer に同期されない** (期待通り)
- → ローカル限定スタンプは確実に他プレイヤーへ漏れない（B 案で安全）。逆に MP 同期するには独自 message 必須

## 9. ModManager 経由のロード ★ Injector 方式の限界

`decompiled/MegaCrit.Sts2.Core.Modding/ModManager.cs`:

- 公式 MOD loader が存在: `mods/` ディレクトリと Steam Workshop から自動ロード
- `Mod.assembly` を `_mods: List<Mod>` に保持
- `ReflectionHelper.ModTypes` は **`ModManager.GetLoadedMods()` の `Mod.assembly.GetTypes()` のみ**を集める
- → **現行 `Injector` で `<Module>.cctor` に `Assembly.LoadFrom` を埋め込む方式は ModManager をバイパスしている**
- 結果: 我々の `EraserMod.dll` は `MessageTypes._cache` に登録されない → 独自 INetMessage は `ToId()` で例外、受信もできない

**選択肢:**

| 案 | 内容 | コスト | リスク |
|----|------|--------|--------|
| **(a) ModManager 経由に移行** | `ModManifest` 同梱して `mods/` に配置、`Injector` 廃止 | 中 | manifest 仕様調査・install スクリプト全面書換 |
| **(b) Reflection で `_mods` に手動 add** | `Bootstrap.Init` で `ModManager._mods` を private field 経由で操作 | 小 | private field 名変更で簡単に壊れる |
| **(c) MessageTypes 静的初期化前に登録** | `MessageTypes` 静的 ctor が走る前に `_mods` を仕込む必要 | 高 | timing 制御が脆い |

→ **本命は (a)**。(b) は MVP として暫定可、後で (a) に移行。

## 10. vanilla peer 互換戦略

シナリオ:
- 全員 MOD: 独自 message 全機能動作
- ホスト MOD + クライアント vanilla:
  - クライアント側は MOD packet を Log.Error で無視（disconnect なし）
  - **vanilla 既存型の ID 互換**を §4 の `~` プレフィックス命名で確保していれば通常プレイ継続
- ホスト vanilla + クライアント MOD:
  - クライアント側で MOD ID シフト発生せず通信成立（`~` プレフィックス前提）
  - 独自 message は `ShouldBroadcast=true` でも host 側が認識せず転送されない可能性 → 要実機検証

**戦略:**

1. **MOD 状態 handshake**: 接続初期に `~EraserModHelloMessage` を 1 回 broadcast。受信した peer のリストに記録
2. 独自描画 message は handshake 受信済 peer にのみ送信（`ShouldBroadcast=false` + 個別 send）か、`ShouldBroadcast=true` で送りつつ受信側 handler で振り分け
3. handshake 未受信 peer は vanilla 扱い → 独自描画スキップ（ローカル限定動作）
4. UI には「peer N 人中 M 人が MOD 同期可」表示

## 11. 既存枠への piggyback (案 C 検討)

参考までに評価したが **採用しない**:

- `NetMapDrawingEvent.position` は `QuantizeParams` で量子化 (16bit X / 24bit Y) → 余剰 bit ほぼなし
- `MapDrawingEventType` は 4 値中 3 値使用、未使用値 `None` を使えば追加情報 1 種程度は注入可
- `MapDrawingMessage._events` の listBits=4 (max 15 イベント) → 拡張余地小
- 量子化された X 軸 16bit のうち下位 1-2 bit を sub-channel にする等は可能だが座標精度劣化
- → **拡張性ゼロに近く、Undo / Stamp / 色 / 太さ の 4 軸を載せきれない**

## 12. 結論と後続アクション

### 結論

**A 案 (独自 INetMessage)** を採用。前提 3 件:
1. ModManager 経由 loader 移行 (またはリフレクション暫定)
2. 型名 `~` プレフィックスで命名
3. handshake で peer 互換性判定

### 後続 issue 提案

1. **#5 (chore): `Injector` を ModManager-compatible 化**
   - ModManifest 仕様調査
   - `mods/<id>/manifest.json` + dll 配置への install スクリプト改修
   - `Injector` 廃止判断（暫定維持か即廃止か）
   - acceptance: `ModManager.IsRunningModded() == true` & `ReflectionHelper.GetSubtypesInMods<INetMessage>` に EraserMod 型が出る

2. **#6 (feature): Phase 1-2 visual eraser toolbar + cursor preview** (plan.md Task 1-4)
   - #5 と並行可能（描画 UI のみで NetMessage 不要）

3. **#7 (feature): Phase 3-5 pencil width/color + stamps + undo (ローカル先行)** (plan.md Task 5-12)
   - #5 と並行可能

4. **#8 (feature): Phase 6 multiplayer sync** (plan.md Task 13-15)
   - #5 完了が前提
   - 独自 NetMessage 3 種 (`~EraserModLineStyleMessage`, `~EraserModStampPlaceMessage`, `~EraserModUndoMessage`) + handshake
   - 2 クライアント実機検証

### 実機検証項目（後続実装フェーズで実施）

- [ ] `Injector` のままで `ReflectionHelper.GetSubtypesInMods` が EraserMod 型を返すか確認（おそらく否）
- [ ] ModManager 経由ロードでの動作確認
- [ ] vanilla peer に MOD packet を投げた時の log を実機で確認
- [ ] `~` プレフィックスで vanilla 既存型 ID が不変かを再現

---

## Verification (Issue #4 の Deliverable)

- [x] INetMessage / IPacketSerializable 契約解析
- [x] RegisterMessageHandler の挙動
- [x] 未知 packet ID 受信挙動 (disconnect しない、Log.Error のみ)
- [x] packet ID 採番 = `string.CompareOrdinal(Type.Name)` sort + array index
- [x] NetTransferMode 使い分け基準
- [x] ShouldBroadcast / senderId 取得経路
- [x] MapDrawingColor 決定ロジック
- [x] Sprite2D ローカル add の peer 同期 = されない
- [x] vanilla peer 互換戦略
- [x] 同期方式確定: A
- [ ] 実機検証 (後続 issue で)

## Sources

- `decompiled/MegaCrit.Sts2.Core.Multiplayer.Serialization/INetMessage.cs`
- `decompiled/MegaCrit.Sts2.Core.Multiplayer.Serialization/IPacketSerializable.cs`
- `decompiled/MegaCrit.Sts2.Core.Multiplayer.Serialization/MessageTypes.cs`
- `decompiled/MegaCrit.Sts2.Core.Multiplayer.Serialization/NetTypeCache.cs`
- `decompiled/MegaCrit.Sts2.Core.Multiplayer/NetMessageBus.cs`
- `decompiled/MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor/MapDrawingMessage.cs`
- `decompiled/MegaCrit.Sts2.Core.Multiplayer.Game.PeerInput/NetMapDrawingEvent.cs`
- `decompiled/MegaCrit.Sts2.Core.Helpers/ReflectionHelper.cs`
- `decompiled/MegaCrit.Sts2.Core.Modding/Mod.cs`
- `decompiled/MegaCrit.Sts2.Core.Modding/ModManager.cs`
- `decompiled/MegaCrit.Sts2.Core.Nodes.Screens.Map/NMapDrawings.cs`
