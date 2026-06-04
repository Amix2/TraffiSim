using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SaveLoadVehiclesSystem : SystemBase
{
    [System.Serializable]
    private class VehicleJsonEntry
    {
        public List<float> position;
        public List<float> destination;

        public float3 PositionFl3()
        { return position.Count == 2 ? new float3(position[0], 0, position[1]) : new float3(position[0], position[1], position[2]); }

        public float3 DestinationFl3()
        { return destination.Count == 2 ? new float3(destination[0], 0, destination[1]) : new float3(destination[0], destination[1], destination[2]); }

        public VehicleJsonEntry(float3 pos, float3 dest)
        {
            position = new List<float>() { pos.x, pos.y, pos.z };
            destination = new List<float>() { dest.x, dest.y, dest.z };
        }

        public VehicleJsonEntry()
        { }
    }

    [System.Serializable]
    private class VehiclesJsonBlueprint
    {
        public List<VehicleJsonEntry> Vehicles = new List<VehicleJsonEntry>();

        public void AddVehicle(float3 pos, float3 dest)
        {
            Vehicles.Add(new VehicleJsonEntry(pos, dest));
        }
    }

    protected override void OnUpdate()
    {
        if (!SystemAPI.HasSingleton<DocumentComponent>())
            return;
        Dependency.Complete();

        ComponentLookup<LocalTransform> LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        ComponentLookup<DestinationPosition> DestinationLookup = SystemAPI.GetComponentLookup<DestinationPosition>(true);
        LocalTransformLookup.Update(this);
        DestinationLookup.Update(this);

        foreach (var (json, jsonEnt) in SystemAPI.Query<RefRO<SaveVehiclesFromJson>>().WithEntityAccess())
        {
            NativeArray<Entity> vehiclesEnt = EntityManager.CreateEntityQuery(typeof(VehicleTag)).ToEntityArray(Allocator.Temp);
            VehiclesJsonBlueprint vehiclesJsonBlueprint = new VehiclesJsonBlueprint();
            foreach (var vehicle in vehiclesEnt)
            {
                float3 vehPos = LocalTransformLookup[vehicle].Position;
                float3 vehDest = DestinationLookup[vehicle];
                vehiclesJsonBlueprint.AddVehicle(vehPos, vehDest);
            }
            string jsonText = JsonConvert.SerializeObject(vehiclesJsonBlueprint);
            ConsoleLogUI.Log(jsonText);
            File.WriteAllText(json.ValueRO.fileName.ToString(), jsonText);
            EntityManager.DestroyEntity(jsonEnt);
        }
        ;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        var Document = SystemAPI.GetSingleton<DocumentComponent>();
        var vehiclePrefab = Document.VehiclePrefab;
        foreach (var (json, jsonEnt) in SystemAPI.Query<RefRO<LoadVehiclesFromJsonFile>>().WithEntityAccess())
        {
            string jsonFile = File.ReadAllText(json.ValueRO.fileName.ToString());
            VehiclesJsonBlueprint vehicleBlueprint = JsonConvert.DeserializeObject<VehiclesJsonBlueprint>(jsonFile);
            foreach (var vehicle in vehicleBlueprint.Vehicles)
            {
                Spawner.SpawnVehicle(ecb, vehiclePrefab, vehicle.PositionFl3(), vehicle.DestinationFl3());
            }
            ecb.DestroyEntity(jsonEnt);
        }
        ;

        foreach (var (json, entity) in SystemAPI.Query<RefRO<LoadVehiclesFromTextJson>>().WithEntityAccess())
        {
            var jsonText = json.ValueRO.jsonText.Value.Json.ToString();

            VehiclesJsonBlueprint vehicleBlueprint =
                JsonConvert.DeserializeObject<VehiclesJsonBlueprint>(jsonText);

            foreach (var vehicle in vehicleBlueprint.Vehicles)
            {
                Spawner.SpawnVehicle(
                    ecb,
                    vehiclePrefab,
                    vehicle.PositionFl3(),
                    vehicle.DestinationFl3());
            }

            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    protected override void OnCreate()
    {
    }

    protected override void OnDestroy()
    {
    }
}