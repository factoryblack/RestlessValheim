using RestlessQoL.Storage;

namespace RestlessQoL.Building;

internal static class BuildingAccess
{
    internal static bool Allows(Piece? piece, long actor) =>
        piece != null && piece.IsPlacedByPlayer() && StorageAccess.Allows(piece.transform.position, actor);
}
