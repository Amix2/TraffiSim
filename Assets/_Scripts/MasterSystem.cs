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
        DocumentTool DocumentTool = EntityManager.GetSharedComponentManaged<DocumentTool>(Document);
        DocumentTool.Tool = tool;
        EntityManager.SetSharedComponentManaged(Document, DocumentTool);
    }

    public CollisionWorld CollisionWorld => SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
    public EntityCommandBuffer CreateBeginSimulationEntityCommandBufferSystem() { return World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer(); }

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

    public T GetSingletonComponent<T>() where T : unmanaged, IComponentData
    {
        return SystemAPI.GetSingleton<T>();
    }

    public void SetSingletonComponent<T>(T value) where T : unmanaged, IComponentData
    {
        SystemAPI.SetSingleton(value);
    }
}