using SFB;
using System.IO;
using Unity.Assertions;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class MainScriptUI : MonoBehaviour
{
    private MasterSystem MasterSystem => World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MasterSystem>();
    private Button LoadRoadJson;
    private RadioButtonGroup ToolsRadioButtons;

    public UIFocusManager m_FocusManager;

    // Start is called before the first frame update
    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        LoadRoadJson = root.Q<Button>("LoadRoadJson");
        Assert.IsNotNull(LoadRoadJson);
        LoadRoadJson.clicked += OnLoadRoadJsonClicked;
        m_FocusManager.RegisterCallbacks(LoadRoadJson);

        ToolsRadioButtons = root.Q<RadioButtonGroup>("ToolRadioButtons");
        Assert.IsNotNull(ToolsRadioButtons);
        ToolsRadioButtons.RegisterValueChangedCallback(OnToolsRadioButtonsChanged);
        m_FocusManager.RegisterCallbacks(ToolsRadioButtons);
        ConsoleLogUI.Log(MasterSystem);
    }

    private void OnLoadRoadJsonClicked()
    {
        try
        {
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

    private void OnToolsRadioButtonsChanged(ChangeEvent<int> button)
    {
        int selected = button.newValue;
        MasterSystem.MessageQueue.Add(new ChangeToolMsg((eToolType)selected));
    }
}