using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;


public interface IMasterSystem
{
    CollisionWorld CollisionWorld { get; }
    public MessageQueue MessageQueue { get; }

    public void AddSpawnEdgeOrder(Entity Node0, Entity Node1);
    public void AddSpawnNodeOrder(float3 nodePos);
    public ITool GetActiveTool();
    public void SetActiveTool(ITool tool);
    public int GetVehicleCountLimit();
    public void SetVehicleCountLimit(int count);
}