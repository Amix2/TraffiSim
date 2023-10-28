using Unity.Entities;
using Unity.Mathematics;

public partial struct SpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<DocumentComponent>())
            return;
        state.Enabled = false;
        var Document = SystemAPI.GetAspect<DocumentAspect>(SystemAPI.GetSingletonEntity<DocumentComponent>());
        var vehiclePrefab = Document.VehiclePrefab;
        var manager = state.EntityManager;

        float2 position = new(UnityEngine.Random.Range(-10.0f, 10.0f), UnityEngine.Random.Range(-10.0f, 10.0f));
        Spawner.SpawnVehicle(manager, vehiclePrefab, new float3(position.x, 0, position.y));
    }

    private void OnCreate(ref SystemState state)
    { }

    private void OnDestroy(ref SystemState state)
    { }
}