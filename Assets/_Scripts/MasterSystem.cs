using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

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
