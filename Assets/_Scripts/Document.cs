using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class Document : MonoBehaviour
{
    public GameObject VehiclePrefabGO;

    public Shader DefaultShader;
    public Mesh ArrowMesh;
    public Material ArrowMaterial;
    public float DeltaTime;
    public int VehicleCountLimit;
    public Scenario Scenario;

    [Serializable]
    public class ScenarioEntry
    {
        [SerializeField]
        public Scenario Scenario;

        [SerializeField]
        public TextAsset TextAsset;
    }

    public List<ScenarioEntry> Scenarios;

    public class Baker : Baker<Document>
    {
        public override void Bake(Document authoring)
        {
            ScenarioEntry selectedScenario = null;
            foreach (var scenario in authoring.Scenarios)
            {
                if (scenario.Scenario == authoring.Scenario)
                    selectedScenario = scenario;
            }
            if (selectedScenario != null)
            {
                TextAsset scenarioAsset = selectedScenario.TextAsset;
                string jsonText = scenarioAsset.text;
                MasterSystem masterSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MasterSystem>();
                masterSystem?.MessageQueue.Add(new LoadVehiclesFromJsonTextMsg(jsonText));
                authoring.VehicleCountLimit = 0;
            }

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new DocumentComponent
            {
                VehiclePrefab = GetEntity(authoring.VehiclePrefabGO, TransformUsageFlags.Renderable),
            });
            AddComponent(entity, new SimConfigComponent
            {
                DeltaTime = authoring.DeltaTime,
                VehicleCountLimit = authoring.VehicleCountLimit,
            });
            AddSharedComponentManaged(entity, new DocumentSharedComponent
            {
                DefaultShader = authoring.DefaultShader,
                ArrowMesh = authoring.ArrowMesh,
                ArrowMaterial = authoring.ArrowMaterial,
            });
            AddSharedComponentManaged(entity, new DocumentTool
            {
                Tool = null
            });
        }
    }
}

public struct DocumentComponent : IComponentData
{
    public Entity VehiclePrefab;
}

public struct SimConfigComponent : IComponentData
{
    public float DeltaTime;
    public int VehicleCountLimit;
}

public struct DocumentSharedComponent : ISharedComponentData, IEquatable<DocumentSharedComponent>
{
    public Shader DefaultShader;
    public Mesh ArrowMesh;
    public Material ArrowMaterial;

    public bool Equals(DocumentSharedComponent other)
    {
        if (!DefaultShader)
            return other.DefaultShader == null;
        if (!ArrowMesh)
            return other.ArrowMesh == null;
        if (!ArrowMaterial)
            return other.ArrowMaterial == null;

        return DefaultShader.Equals(other.DefaultShader) && ArrowMesh.Equals(other.ArrowMesh) && ArrowMaterial.Equals(other.ArrowMaterial);
    }

    public override int GetHashCode()
    {
        return DefaultShader.GetHashCode() ^ ArrowMesh.GetHashCode() ^ ArrowMaterial.GetHashCode();
    }
}

public struct DocumentTool : ISharedComponentData, IEquatable<DocumentTool>
{
    public ITool Tool;

    public bool Equals(DocumentTool other)
    {
        if (Tool == null)
            return false;
        return Tool.Equals(other.Tool);
    }

    public override int GetHashCode()
    {
        return Tool != null ? Tool.GetHashCode() : 0;
    }
}

public readonly partial struct DocumentAspect : IAspect
{
    public readonly Entity Entity;

    private readonly RefRW<DocumentComponent> DocumentComponent;
    private readonly RefRW<SimConfigComponent> SimConfigComponent;

    public Entity VehiclePrefab => DocumentComponent.ValueRO.VehiclePrefab;
    public float DeltaTime => SimConfigComponent.ValueRO.DeltaTime;
}