using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SaveLoadVehiclesSystem : SystemBase
{
    [System.Serializable]
    class VehicleJsonEntry
    {
        public List<float> position;
        public List<float> destination;
        public float3 PositionFl3() {  return position.Count == 2 ? new float3(position[0], 0, position[1]) :  new float3(position[0], position[1], position[2]); }
        public float3 DestinationFl3() { return destination.Count == 2 ? new float3(destination[0], 0, destination[1]) : new float3(destination[0], destination[1], destination[2]); }

        public VehicleJsonEntry(VehicleAspect vehicle)
        {
            float3 pos = vehicle.Position;
            position = new List<float>() { pos.x, pos.y, pos.z };

            float3 dest = vehicle.Destination;
            destination = new List<float>() { dest.x, dest.y, dest.z };
        }
        public VehicleJsonEntry()
        { }
    }
    [System.Serializable]
    class VehiclesJsonBlueprint
    {
        public List<VehicleJsonEntry> Vehicles = new List<VehicleJsonEntry>();

        public void AddVehicle(VehicleAspect vehicle)
        {
            Vehicles.Add(new VehicleJsonEntry(vehicle));
        }
    }


    protected override void OnUpdate()
    {
        if (!SystemAPI.HasSingleton<DocumentComponent>())
            return;
        Dependency.Complete();

        Entities.WithStructuralChanges().WithoutBurst()
            .ForEach((ref Entity jsonEnt, ref SaveVehiclesFromJson json) =>
        {
            NativeArray<Entity> vehiclesEnt = EntityManager.CreateEntityQuery(typeof(VehicleTag)).ToEntityArray(Allocator.Temp);
            VehiclesJsonBlueprint vehiclesJsonBlueprint = new VehiclesJsonBlueprint();
            foreach (var vehicle in vehiclesEnt)
            {
                var vehAspect = EntityManager.GetAspect<VehicleAspect>(vehicle);
                vehiclesJsonBlueprint.AddVehicle(vehAspect);
            }
            string jsonText = JsonConvert.SerializeObject(vehiclesJsonBlueprint);
            ConsoleLogUI.Log(jsonText);
            File.WriteAllText(json.fileName.ToString(), jsonText);
            EntityManager.DestroyEntity(jsonEnt);
        }).Run();


        var Document = SystemAPI.GetAspect<DocumentAspect>(SystemAPI.GetSingletonEntity<DocumentComponent>());
        var vehiclePrefab = Document.VehiclePrefab;
        Entities.WithStructuralChanges().WithoutBurst()
            .ForEach((ref Entity jsonEnt, ref LoadVehiclesFromJsonFile json) =>
            {
                string jsonFile = File.ReadAllText(json.fileName.ToString());
                VehiclesJsonBlueprint vehicleBlueprint = JsonConvert.DeserializeObject<VehiclesJsonBlueprint>(jsonFile);
                foreach(var vehicle in vehicleBlueprint.Vehicles)
                {
                    Spawner.SpawnVehicle(EntityManager, vehiclePrefab, vehicle.PositionFl3(), vehicle.DestinationFl3());
                }
                EntityManager.DestroyEntity(jsonEnt);

            }).Run();

        Entities.WithStructuralChanges().WithoutBurst()
            .ForEach((ref Entity jsonEnt, in LoadVehiclesFromTextJson json) =>
            {
                VehiclesJsonBlueprint vehicleBlueprint = JsonConvert.DeserializeObject<VehiclesJsonBlueprint>(json.jsonText);
                foreach (var vehicle in vehicleBlueprint.Vehicles)
                {
                    Spawner.SpawnVehicle(EntityManager, vehiclePrefab, vehicle.PositionFl3(), vehicle.DestinationFl3());
                }
                EntityManager.DestroyEntity(jsonEnt);

            }).Run();
    }

    protected override void OnCreate()
    {
    }

    protected override void OnDestroy()
    {
    }
}
