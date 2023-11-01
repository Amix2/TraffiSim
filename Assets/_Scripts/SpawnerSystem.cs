using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct SpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<DocumentComponent>())
            return;

        int vehiclesCount = state.GetEntityQuery(typeof(Acceleration)).CalculateEntityCount();
        if (vehiclesCount > 10)
            return;
        //state.Enabled = false;
        var Document = SystemAPI.GetAspect<DocumentAspect>(SystemAPI.GetSingletonEntity<DocumentComponent>());
        var vehiclePrefab = Document.VehiclePrefab;
        var manager = state.EntityManager;

        float range = 20;
        float2 position = new(UnityEngine.Random.Range(-range, range), UnityEngine.Random.Range(-range, range));
        float2 targetPosition = new(UnityEngine.Random.Range(-range, range), UnityEngine.Random.Range(-range, range));
        Spawner.SpawnVehicle(manager, vehiclePrefab, new float3(position.x, 0, position.y), new float3(targetPosition.x, 0, targetPosition.y));
    }

    private void OnCreate(ref SystemState state)
    { }

    private void OnDestroy(ref SystemState state)
    { }
}