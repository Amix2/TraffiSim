using Unity.Entities;
using UnityEngine;

public class rgRoadNodeAuth : MonoBehaviour
{
    public class Baker : Baker<rgRoadNodeAuth>
    {
        public override void Bake(rgRoadNodeAuth authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<rgRoadNodeData>(entity);
            AddBuffer<rgRoadPortChild>(entity);
        }
    }
}