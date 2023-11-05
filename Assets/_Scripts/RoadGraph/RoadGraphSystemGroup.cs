using Unity.Entities;
using Unity.Transforms;

[UpdateBefore(typeof(TransformSystemGroup))]
public partial class RoadGraphSystemGroup : ComponentSystemGroup
{
}
