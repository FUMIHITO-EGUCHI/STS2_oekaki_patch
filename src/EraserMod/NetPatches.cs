using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace EraserMod;

[HarmonyPatch(typeof(NMapDrawings), "Initialize")]
public static class NMapDrawings_Initialize_Patch
{
    static void Postfix(NMapDrawings __instance, INetGameService netService)
    {
        try
        {
            netService.RegisterMessageHandler(NetSync.OnHello);
            netService.RegisterMessageHandler(NetSync.OnStyle);
            netService.RegisterMessageHandler(NetSync.OnUndo);
            NetSync.Attach(__instance, netService);
            Bootstrap.Log("[MP] EraserMod multiplayer handlers registered");
        }
        catch (Exception e)
        {
            Bootstrap.Log("Initialize patch err: " + e);
        }
    }
}

[HarmonyPatch(typeof(NMapDrawings), "_ExitTree")]
public static class NMapDrawings_ExitTree_Patch
{
    static void Prefix(NMapDrawings __instance)
    {
        var svc = NetSync.Detach(__instance);
        if (svc == null) return;
        try
        {
            svc.UnregisterMessageHandler(NetSync.OnHello);
            svc.UnregisterMessageHandler(NetSync.OnStyle);
            svc.UnregisterMessageHandler(NetSync.OnUndo);
            Bootstrap.Log("[MP] EraserMod multiplayer handlers unregistered");
        }
        catch (Exception e)
        {
            Bootstrap.Log("ExitTree patch err: " + e.Message);
        }
    }
}
