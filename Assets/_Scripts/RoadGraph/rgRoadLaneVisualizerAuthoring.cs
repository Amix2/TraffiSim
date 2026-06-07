using Unity.Entities;
using UnityEngine;

internal class rgRoadLaneVisualizerAuthoring : MonoBehaviour
{
    private class Baker : Baker<rgRoadLaneVisualizerAuthoring>
    {
        public override void Bake(rgRoadLaneVisualizerAuthoring authoring)
        {
            // set color in children
            foreach (MeshWithColorAuthoring childColor in authoring.GetComponentsInChildren<MeshWithColorAuthoring>(true))
                childColor.Color = Color.indianRed;

            var Entity = GetEntity(TransformUsageFlags.NonUniformScale);
            AddComponent<RoadLaneVisualizer>(Entity);
        }
    }
}