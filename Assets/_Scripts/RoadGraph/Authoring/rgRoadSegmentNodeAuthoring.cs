using Unity.Entities;
using UnityEngine;

public class rgRoadSegmentNodeAuthoring : MonoBehaviour
{
    public class Baker : Baker<rgRoadSegmentNodeAuthoring>
    {
        public override void Bake(rgRoadSegmentNodeAuthoring authoring)
        {
            var Entity = GetEntity(TransformUsageFlags.ManualOverride);
            AddComponent<RoadSegmentNodeUpdateChildNodes>(Entity);
            AddBuffer<RoadSegmentNodeElements>(Entity);
        }
    }
}