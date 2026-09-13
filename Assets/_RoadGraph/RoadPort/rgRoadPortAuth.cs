using Unity.Entities;
using UnityEngine;

public class rgRoadPortAuth : MonoBehaviour
{
    public class Baker : Baker<rgRoadPortAuth>
    {
        public override void Bake(rgRoadPortAuth authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.ManualOverride);
            AddComponent<RoadPortData>(entity);
            AddComponent<RoadPortRemoveDuplicatesInOutBuffers>(entity);
            AddBuffer<RoadPortInput>(entity);
            AddBuffer<RoadPortOutput>(entity);
        }
    }
}