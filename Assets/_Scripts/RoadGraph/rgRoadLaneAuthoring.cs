using Unity.Entities;
using UnityEngine;

internal class rgRoadLaneAuthoring : MonoBehaviour
{
    private class Baker : Baker<rgRoadLaneAuthoring>
    {
        public override void Bake(rgRoadLaneAuthoring authoring)
        {
            var Entity = GetEntity(TransformUsageFlags.ManualOverride);
            AddComponent<RoadLaneData>(Entity);
        }
    }
}