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
            AddComponent<RoadLaneUpdateMesh>(Entity);

            RoadLaneVisualizerData visualizerData = new();

            foreach (GameObject child in GetChildren(true))
            {
                if (child.name == "Visualizer")
                {
                    visualizerData.VisualizerEnt = GetEntity(child, TransformUsageFlags.Dynamic);
                }
                if (child.name == "Background")
                {
                    visualizerData.BackgroundEnt = GetEntity(child, TransformUsageFlags.Dynamic);
                }
                if (child.name == "Markings")
                {
                    visualizerData.MarkingsEnt = GetEntity(child, TransformUsageFlags.Dynamic);
                }
            }
            Debug.Assert(visualizerData.VisualizerEnt != Entity.Null);
            Debug.Assert(visualizerData.BackgroundEnt != Entity.Null);
            Debug.Assert(visualizerData.MarkingsEnt != Entity.Null);

            AddComponent(Entity, visualizerData);
        }
    }
}