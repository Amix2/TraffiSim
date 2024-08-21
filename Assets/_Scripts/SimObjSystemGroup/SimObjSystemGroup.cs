using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation, WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(RoadGraphSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class SimObjSystemGroup : ComponentSystemGroup
{
    public SimObjSystemGroup()
    {
        RateManager = new RateUtils.FixedRateCatchUpManager(0.01f);
    }

    protected override void OnUpdate()
    {
        if (SystemAPI.TryGetSingleton<SimConfigComponent>(out SimConfigComponent simConfig))
        {
            RateManager.Timestep = simConfig.DeltaTime / simConfig.ReplaySpeed;
            if (simConfig.StepsCount >= 0)
            {
                if (simConfig.StepsCount == 0)
                {
                    while (RateManager.ShouldGroupUpdate(this)) { }
                    return;
                }
                if (simConfig.StepsCount > 0)
                {
                    simConfig.StepsCount--;
                    SystemAPI.SetSingleton(simConfig);
                }
            }
        }
        else
        {
            return;
        }

        base.OnUpdate();
    }
}

[UpdateInGroup(typeof(SimObjSystemGroup))]
public partial struct SimObjSystem : ISystem
{
    double prevTime;
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //ConsoleLogUI.Log(SystemAPI.Time.ElapsedTime - prevTime);
        prevTime = SystemAPI.Time.ElapsedTime;
    }

}
