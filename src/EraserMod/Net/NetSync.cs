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
            Bootstrap.Log($"[MP] Style from {id}: pencil={msg.PencilMultiplier:F2} eraser={msg.EraserMultiplier:F2} color={msg.ColorHex}");
            PeerStyleCache.SetStyle(id, msg.PencilMultiplier, msg.EraserMultiplier, msg.ColorHex);
        };

    internal static readonly MessageHandlerDelegate<zEraserModUndoMessage> OnUndo =
        (msg, id) =>
        {
            Bootstrap.Log($"[MP] Undo from peer {id}");
            if (_host != null) UndoStack.UndoPeer(_host, id);
        };

    public static bool IsActive => _svc?.IsConnected == true;

    public static void Attach(NMapDrawings host, INetGameService svc)
    {
        _host = host;
        _svc = svc;
        try { svc.SendMessage(new zEraserModHelloMessage()); }
        catch (Exception e) { Bootstrap.Log("hello send err: " + e.Message); }
    }

    // Returns the service so the caller can unregister handlers.
    public static INetGameService Detach(NMapDrawings host)
    {
        if (_host != host) return null;
        var svc = _svc;
        _svc = null;
        _host = null;
        PeerStyleCache.Clear();
        return svc;
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
