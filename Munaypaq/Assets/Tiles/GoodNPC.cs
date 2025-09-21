public class GoodNPC : NPCBase
{
    protected override void PerformAction()
    {
        // Limpiar tile actual
        GridManager.Instance.SetTileState(currentGridPosition, TileState.Clean);
    }
}