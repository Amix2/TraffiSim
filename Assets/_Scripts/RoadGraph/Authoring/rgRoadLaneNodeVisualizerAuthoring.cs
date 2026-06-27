using Unity.Entities;
using UnityEngine;

internal class rgRoadLaneNodeVisualizerAuthoring : MonoBehaviour
{
    private class Baker : Baker<rgRoadLaneNodeVisualizerAuthoring>
    {
        public override void Bake(rgRoadLaneNodeVisualizerAuthoring authoring)
        {
            var Entity = GetEntity(TransformUsageFlags.NonUniformScale);
            AddComponent<RoadLaneNodeVisualizer>(Entity);
        }
    }
}