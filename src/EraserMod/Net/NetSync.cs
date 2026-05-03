using System;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace EraserMod;

internal static class NetSync
{
    private static INetGameService _svc;
    private static NMapDrawings _host;

    // Static delegates — stable references required for UnregisterMessageHandler.
    internal static readonly MessageHandlerDelegate<zEraserModHelloMessage> OnHello =
        (msg, id) =>
        {
            Bootstrap.Log($"[MP] Hello from peer {id} v{msg.Version}");
            PeerStyleCache.SetModPeer(id);
        };

    internal static readonly MessageHandlerDelegate<zEraserModLineStyleMessage> OnStyle =
        (msg, id) =>
        {
            // Hello must precede style — drop spoofed style from peers we never shook hands with.
            if (!PeerStyleCache.IsModPeer(id))
            {
                Bootstrap.Log($"[MP] Drop style from non-mod peer {id}");
                return;
            }
            Bootstrap.Log($"[MP] Style from {id}: pencil={msg.PencilMultiplier:F2} eraser={msg.EraserMultiplier:F2} color={msg.ColorHex}");
            PeerStyleCache.SetStyle(id, msg.PencilMultiplier, msg.EraserMultiplier, msg.ColorHex);
        };

    internal static readonly MessageHandlerDelegate<zEraserModUndoMessage> OnUndo =
        (msg, id) =>
        {
            // Undo deletes a Line2D from the peer's viewport — only allow it from peers
            // that completed the hello handshake, to mitigate spoofed undo storms.
            if (!PeerStyleCache.IsModPeer(id))
            {
                Bootstrap.Log($"[MP] Drop undo from non-mod peer {id}");
                return;
            }
            Bootstrap.Log($"[MP] Undo from peer {id}");
            if (_host != null) UndoStack.UndoPeer(_host, id);
        };

    public static bool IsActive => _svc?.IsConnected == true;

    public static void Attach(NMapDrawings host, INetGameService svc)
    {
        // Defensive: if a previous Initialize was not paired with _ExitTree,
        // the old service still holds our handlers. Clean it up before swapping.
        if (_svc != null && !ReferenceEquals(_svc, svc))
            TryUnregisterAll(_svc);

        _host = host;
        _svc = svc;
        PeerStyleCache.Clear();
    }

    public static void SendHello()
    {
        if (!IsActive) return;
        try { _svc.SendMessage(new zEraserModHelloMessage()); }
        catch (Exception e) { Bootstrap.Log("hello send err: " + e.Message); }
    }

    // Always returns the currently-attached service (if any) so the caller can
    // unregister handlers, regardless of whether host matches the stored one.
    public static INetGameService Detach(NMapDrawings host)
    {
        var svc = _svc;
        _svc = null;
        _host = null;
        PeerStyleCache.Clear();
        return svc;
    }

    private static void TryUnregisterAll(INetGameService svc)
    {
        try
        {
            svc.UnregisterMessageHandler(OnHello);
            svc.UnregisterMessageHandler(OnStyle);
            svc.UnregisterMessageHandler(OnUndo);
        }
        catch (Exception e) { Bootstrap.Log("re-attach unregister err: " + e.Message); }
    }

    public static void SendStyleAnnounce()
    {
        if (!IsActive) return;
        try { _svc.SendMessage(zEraserModLineStyleMessage.FromConfig()); }
        catch (Exception e) { Bootstrap.Log("style send err: " + e.Message); }
    }

    public static void SendUndo()
    {
        if (!IsActive) return;
        try { _svc.SendMessage(new zEraserModUndoMessage()); }
        catch (Exception e) { Bootstrap.Log("undo send err: " + e.Message); }
    }
}
