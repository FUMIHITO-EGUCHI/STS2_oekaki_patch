using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace EraserMod;

public static class UndoStack
{
    public static bool UndoLocal(NMapDrawings host)
    {
        if (host == null || MapReflection.IsLocalDrawing(host)) return false;
        var viewport = MapReflection.GetDrawViewport(MapReflection.GetLocalState(host));
        if (viewport == null) return false;

        for (int i = viewport.GetChildCount() - 1; i >= 0; i--)
        {
            var child = viewport.GetChild(i);
            if (child is Line2D)
            {
                child.QueueFree();
                Bootstrap.Log("undo local drawing");
                return true;
            }
        }
        return false;
    }

}
