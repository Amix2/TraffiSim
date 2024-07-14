using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

[UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class MasterSystem : SystemBase, IMasterSystem
{
    public MessageQueue MessageQueue;
    MessageQueue IMasterSystem.MessageQueue => MessageQueue;

    protected override void OnUpdate()
    {
        MessageQueue.Execute(this);

        ITool activeTool = GetActiveTool();
        activeTool?.OnUpdate(this);
    }

    public ITool GetActiveTool()
    {
        if (!SystemAPI.HasSingleton<DocumentComponent>())
            return null;  // just some trash

        var Document = SystemAPI.GetSingletonEntity<DocumentComponent>();
        return EntityManager.GetSharedComponentManaged<DocumentTool>(Document).Tool;
    }

    public void SetActiveTool(ITool tool)
    {
        var Document = SystemAPI.GetSingletonEntity<DocumentComponent>();
        EntityManager.SetSharedComponentManaged(Document, new DocumentTool { Tool = tool });
    }

    public CollisionWorld CollisionWorld => SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

    public void AddSpawnNodeOrder(float3 nodePos)
    {
        var orderEnt = EntityManager.CreateEntity(typeof(rgSpawnNodeOrder));
        EntityManager.SetComponentData(orderEnt, new rgSpawnNodeOrder { position = nodePos });
    }

    public void AddSpawnEdgeOrder(Entity Node0, Entity Node1)
    {
        Assert.AreNotEqual(Node0, Node1);
        Assert.AreNotEqual(Node0, Entity.Null);
        Assert.AreNotEqual(Node1, Entity.Null);
        var orderEnt = EntityManager.CreateEntity(typeof(rgSpawnEdgeOrder));
        EntityManager.SetComponentData(orderEnt, new rgSpawnEdgeOrder { Node0 = Node0, Node1 = Node1 });
    }

    protected override void OnCreate()
    {
        MessageQueue = new MessageQueue();
    }

    public int GetVehicleCountLimit()
    {
        if (SystemAPI.HasSingleton<SimConfigComponent>())
            return SystemAPI.GetSingleton<SimConfigComponent>().VehicleCountLimit;
        return -1;
    }

    public void SetVehicleCountLimit(int count)
    {
        var simConfig = SystemAPI.GetSingleton<SimConfigComponent>();
        simConfig.VehicleCountLimit = count;
        SystemAPI.SetSingleton(simConfig);
    }
}