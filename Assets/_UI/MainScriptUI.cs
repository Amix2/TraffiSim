using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class MainScriptUI : MonoBehaviour
{
    Button LoadRoadJson;
    // Start is called before the first frame update
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        LoadRoadJson = root.Q<Button>("LoadRoadJson");
        LoadRoadJson.clicked += OnLoadRoadJsonClicked;
    }

    void OnLoadRoadJsonClicked()
    {
        MasterSystem masterSys = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MasterSystem>();
        masterSys.MessageQueue.Add(new LoadRoadFromJsonMsg());
    }
}
