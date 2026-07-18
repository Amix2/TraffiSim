using Unity.Entities;
using UnityEngine;

internal class VisualizerAuthoring : MonoBehaviour
{
    private class Baker : Baker<VisualizerAuthoring>
    {
        public override void Bake(VisualizerAuthoring authoring)
        {
            var Entity = GetEntity(TransformUsageFlags.NonUniformScale);
            AddComponent<RoadVisualizerParent>(Entity);
        }
    }
}