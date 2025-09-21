public class BadNPC : NPCBase
{
    protected override void PerformAction()
    {
        // Ensuciar tile actual
        GridManager.Instance.SetTileState(currentGridPosition, TileState.Dirty);
    }
}