using System.Collections.Generic;

namespace EraserMod;

internal static class PeerStyleCache
{
    private static readonly Dictionary<ulong, PeerStyle> _styles = new();
    private static readonly HashSet<ulong> _modPeers = new();

    public static void Clear()
    {
        _styles.Clear();
        _modPeers.Clear();
    }

    public static void SetModPeer(ulong netId) => _modPeers.Add(netId);
    public static bool IsModPeer(ulong netId) => _modPeers.Contains(netId);

    public static void SetStyle(ulong netId, float pencilMul, float eraserMul, string colorHex)
        => _styles[netId] = new PeerStyle(pencilMul, eraserMul, colorHex);

    public static bool TryGetStyle(ulong netId, out PeerStyle style)
        => _styles.TryGetValue(netId, out style);

    internal record PeerStyle(float PencilMultiplier, float EraserMultiplier, string ColorHex);
}
