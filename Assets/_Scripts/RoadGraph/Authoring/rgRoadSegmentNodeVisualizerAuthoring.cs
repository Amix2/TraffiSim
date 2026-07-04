using Unity.Entities;
using UnityEngine;

internal class rgRoadSegmentNodeVisualizerAuthoring : MonoBehaviour
{
    private class Baker : Baker<rgRoadSegmentNodeVisualizerAuthoring>
    {
        public override void Bake(rgRoadSegmentNodeVisualizerAuthoring authoring)
        {
            var Entity = GetEntity(TransformUsageFlags.NonUniformScale);
            AddComponent<RoadVisualizerParent>(Entity);
        }
    }
}