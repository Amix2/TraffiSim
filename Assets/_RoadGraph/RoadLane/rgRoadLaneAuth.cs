using Unity.Entities;
using UnityEngine;

public class rgRoadLaneAuth : MonoBehaviour
{
    public class Baker : Baker<rgRoadLaneAuth>
    {
        public override void Bake(rgRoadLaneAuth authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<RoadLaneData>(entity);
            AddComponent<RoadLaneUpdatePoints>(entity);
            AddBuffer<RoadLanePoint>(entity);
        }
    }
}