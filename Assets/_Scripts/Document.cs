using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class Document : MonoBehaviour
{
    public GameObject VehiclePrefabGO;
    public GameObject FactoryPrefabGO;

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
            MasterSystem masterSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<MasterSystem>();
            if (selectedScenario != null)
            {
                TextAsset scenarioAsset = selectedScenario.TextAsset;
                string jsonText = scenarioAsset.text;
                masterSystem?.MessageQueue.Add(new LoadVehiclesFromJsonTextMsg(jsonText));
                masterSystem?.MessageQueue.Add(new LoadRoadFromJsonTextMsg(jsonText));
                authoring.VehicleCountLimit = 0;
            }
            masterSystem?.MessageQueue.Add(new DebugInitializeMsg());

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new DocumentComponent
            {
                VehiclePrefab = GetEntity(authoring.VehiclePrefabGO, TransformUsageFlags.Renderable),
                FactoryPrefab = GetEntity(authoring.FactoryPrefabGO, TransformUsageFlags.Renderable),
                DefaultShader = authoring.DefaultShader,
                ArrowMesh = authoring.ArrowMesh,
                ArrowMaterial = authoring.ArrowMaterial,
            });
            AddComponent(entity, new SimConfigComponent
            {
                DeltaTime = authoring.DeltaTime,
                VehicleCountLimit = authoring.VehicleCountLimit,
                ReplaySpeed = 1.0f,
                StepsCount = -1,
            });
            AddComponent(entity, new DocumentTool
            {
                Tool = null
            });
        }
    }
}

public struct DocumentComponent : IComponentData
{
    public Entity VehiclePrefab;
    public Entity FactoryPrefab;
    public UnityObjectRef<Shader> DefaultShader;
    public UnityObjectRef<Mesh> ArrowMesh;
    public UnityObjectRef<Material> ArrowMaterial;
}

public struct SimConfigComponent : IComponentData
{
    public float DeltaTime;
    public int VehicleCountLimit;
    public float ReplaySpeed;
    public int StepsCount; // <1 -> infinite
}

public struct DocumentTool : IComponentData
{
    public UnityObjectRef<ToolBase> Tool;
}