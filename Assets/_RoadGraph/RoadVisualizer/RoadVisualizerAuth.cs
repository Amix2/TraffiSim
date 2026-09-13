using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

class RoadVisualizerAuth : MonoBehaviour
{
    
}

class RoadVisualizerAuthBaker : Baker<RoadVisualizerAuth>
{
    public override void Bake(RoadVisualizerAuth authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Renderable);
        AddComponent<RoadVisualizerData>(entity);
        //AddComponent<RuntimeUpdateableMesh>(entity);  // add at runtime https://docs.unity3d.com/Packages/com.unity.entities%401.4/api/Unity.Entities.ICleanupComponentData.html
        AddComponent<RuntimeUpdateableMeshAliveTag>(entity);
    }
}
