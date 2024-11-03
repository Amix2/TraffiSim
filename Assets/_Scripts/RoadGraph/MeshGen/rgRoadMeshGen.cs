using Unity.Burst;
using Unity.Collections;
using Unity.Entities;   
   
   
public partial class rgRoadMeshGen : SystemBase
{
    [BurstCompile]
    protected override void OnUpdate()
    {
        Entities.WithStructuralChanges().ForEach((Entity jsonEnt, in rgLoadRoadFromJson json) =>
        {

        }).Run();
    }

    [BurstCompile]
    protected override void OnCreate()
    {
    }

    [BurstCompile]
    protected override void OnDestroy()
    {
    }
}
