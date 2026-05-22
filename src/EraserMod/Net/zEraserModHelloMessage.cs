using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace EraserMod;

// Prefix 'z' (0x7A) puts this after all vanilla INetMessage types in NetTypeCache ordinal sort.
public class zEraserModHelloMessage : INetMessage
{
    public string Version { get; private set; } = "1";
    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => false;
    public void Serialize(PacketWriter writer) => writer.WriteString(Version);
    public void Deserialize(PacketReader reader) => Version = reader.ReadString();
}
