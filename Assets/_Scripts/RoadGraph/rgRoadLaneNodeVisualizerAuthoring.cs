using Unity.Entities;
using UnityEngine;

internal class rgRoadLaneNodeVisualizerAuthoring : MonoBehaviour
{
    private class Baker : Baker<rgRoadLaneNodeVisualizerAuthoring>
    {
        public override void Bake(rgRoadLaneNodeVisualizerAuthoring authoring)
        {
            // set color in children
            foreach (MeshWithColorAuthoring childColor in authoring.GetComponentsInChildren<MeshWithColorAuthoring>(true))
                childColor.Color = Color.yellow;

            var Entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<RoadLaneNodeVisualizer>(Entity);
        }
    }
}