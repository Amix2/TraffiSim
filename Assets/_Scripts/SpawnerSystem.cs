using Unity.Entities;
using Unity.Mathematics;

public partial struct SpawnerSystem : ISystem
{
    private EntityQuery entityQuery;

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<DocumentComponent>())
            return;

        int vehiclesCount = entityQuery.CalculateEntityCount();
        if (vehiclesCount >= 10)
            return;
        //state.Enabled = false;
        var Document = SystemAPI.GetAspect<DocumentAspect>(SystemAPI.GetSingletonEntity<DocumentComponent>());
        var vehiclePrefab = Document.VehiclePrefab;
        var manager = state.EntityManager;

        float range = 50;
        float2 position = new(UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-1.0f, 1.0f));
        // position = new(-1,-1);
        position = position.normsafe() * range;
        float2 targetPosition = new(UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-1.0f, 1.0f));
        //targetPosition = new(1,1);
        targetPosition = targetPosition.normsafe() * range;
        Spawner.SpawnVehicle(manager, vehiclePrefab, new float3(position.x, 0, position.y), new float3(targetPosition.x, 0, targetPosition.y));
    }

    private void OnCreate(ref SystemState state)
    {
        entityQuery = state.GetEntityQuery(typeof(Acceleration));
    }

    private void OnDestroy(ref SystemState state)
    { }
}