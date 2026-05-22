using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace EraserMod;

// Announces the sender's pencil/eraser style before they begin a line.
public class zEraserModLineStyleMessage : INetMessage
{
    public float PencilMultiplier { get; private set; }
    public float EraserMultiplier { get; private set; }
    public string ColorHex { get; private set; } = "";

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteFloat(PencilMultiplier);
        writer.WriteFloat(EraserMultiplier);
        writer.WriteString(ColorHex);
    }

    public void Deserialize(PacketReader reader)
    {
        PencilMultiplier = reader.ReadFloat();
        EraserMultiplier = reader.ReadFloat();
        ColorHex = reader.ReadString();
    }

    public static zEraserModLineStyleMessage FromConfig() => new()
    {
        PencilMultiplier = Config.PencilMultiplier,
        EraserMultiplier = Config.EraserMultiplier,
        ColorHex = Config.PencilColorHex ?? "",
    };
}
