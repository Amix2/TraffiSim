using Unity.Physics;

public interface IMasterSystem
{
    CollisionWorld CollisionWorld { get; }
    public MessageQueue MessageQueue { get; }

    public ITool GetActiveTool();

    public void SetActiveTool(ITool tool);

    public int GetVehicleCountLimit();

    public void SetVehicleCountLimit(int count);
}