using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;


public interface IMasterSystem
{
    CollisionWorld CollisionWorld { get; }
    public MessageQueue MessageQueue { get; }

    void AddSpawnEdgeOrder(Entity Node0, Entity Node1);
    void AddSpawnNodeOrder(float3 nodePos);
    ITool GetActiveTool();
    void SetActiveTool(ITool tool);
    int GetVehicleCountLimit();
    void SetVehicleCountLimit(int count);
}