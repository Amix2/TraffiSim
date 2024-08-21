using Unity.Entities;
using Unity.Transforms;

[UpdateBefore(typeof(TransformSystemGroup))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class RoadGraphSystemGroup : ComponentSystemGroup
{
}