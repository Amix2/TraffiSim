using SFB;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class MainScriptUI : MonoBehaviour
{
    MasterSystem MasterSystem => World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MasterSystem>();
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

        try
        {
            using var scopedCamLock = CameraController.GetScopedLock();

            var extensions = new[] {
            new ExtensionFilter("Road files", "json")
            };
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Choose file to load", "", extensions, false);

            string path = paths[0];
            if (path.Length != 0)
            {
                Debug.Log(path);
                MasterSystem.MessageQueue.Add(new LoadRoadFromJsonMsg(path));
            }
        }
        catch (System.Exception)
        {
        }

    }
}
