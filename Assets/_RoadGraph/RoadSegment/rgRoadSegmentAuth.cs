using Unity.Entities;
using UnityEngine;

public class rgRoadSegmentAuth : MonoBehaviour
{
    public class Baker : Baker<rgRoadSegmentAuth>
    {
        public override void Bake(rgRoadSegmentAuth authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<rgRoadSegmentData>(entity);
            AddBuffer<rgRoadSegmentNode>(entity);
            AddBuffer<rgRoadSegmentLane>(entity);
        }
    }
}