using Unity.Entities;
using UnityEngine;

internal class rgRoadLaneNodeAuthoring : MonoBehaviour
{
    private class Baker : Baker<rgRoadLaneNodeAuthoring>
    {
        public override void Bake(rgRoadLaneNodeAuthoring authoring)
        {
            var Entity = GetEntity(TransformUsageFlags.ManualOverride);
            AddComponent<RoadLaneNodeData>(Entity);
            AddComponent<RoadLaneNodeUpdateInOutBuffers>(Entity);
            SetComponentEnabled<RoadLaneNodeUpdateInOutBuffers>(Entity, false);
            AddBuffer<RoadLaneNodeInput>(Entity);
            AddBuffer<RoadLaneNodeOutput>(Entity);
        }
    }
}