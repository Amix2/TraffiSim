using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

[UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class MasterSystem : SystemBase
{
    public MessageQueue MessageQueue;

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

    protected override void OnCreate()
    {
        MessageQueue = new MessageQueue();
    }
}