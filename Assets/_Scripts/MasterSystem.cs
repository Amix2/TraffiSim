using Unity.Entities;

[UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class MasterSystem : SystemBase
{
    public MessageQueue MessageQueue;

    protected override void OnUpdate()
    {
        MessageQueue.Execute(this);
    }

    protected override void OnCreate()
    {
        MessageQueue = new MessageQueue();
    }
}