using Crosstales.FB;
using System.IO;
using Unity.Assertions;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class MainScriptUI : MonoBehaviour
{
    private IMasterSystem MasterSystem => World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MasterSystem>();
    private Button LoadRoadJson;
    private Button SaveRoadJson;
    private Button LoadVehiclesJson;
    private Button SaveVehiclesJson;
    private RadioButtonGroup ToolsRadioButtons;
    private IntegerField NumOfCarsInput;

    public UIFocusManager m_FocusManager;

    // Start is called before the first frame update
    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        LoadRoadJson = root.Q<Button>("LoadRoadJson");
        Assert.IsNotNull(LoadRoadJson);
        LoadRoadJson.clicked += OnLoadRoadJsonClicked;
        m_FocusManager.RegisterCallbacks(LoadRoadJson);

        SaveRoadJson = root.Q<Button>("SaveRoadJson");
        Assert.IsNotNull(SaveRoadJson);
        SaveRoadJson.clicked += OnSaveRoadJsonClicked;
        m_FocusManager.RegisterCallbacks(SaveRoadJson);

        LoadVehiclesJson = root.Q<Button>("LoadVehiclesJson");
        Assert.IsNotNull(LoadVehiclesJson);
        LoadVehiclesJson.clicked += OnLoadVehiclesJsonClicked;
        m_FocusManager.RegisterCallbacks(LoadVehiclesJson);

        SaveVehiclesJson = root.Q<Button>("SaveVehiclesJson");
        Assert.IsNotNull(SaveVehiclesJson);
        SaveVehiclesJson.clicked += OnSaveVehiclesJsonClicked;
        m_FocusManager.RegisterCallbacks(SaveVehiclesJson);

        ToolsRadioButtons = root.Q<RadioButtonGroup>("ToolRadioButtons");
        Assert.IsNotNull(ToolsRadioButtons);
        ToolsRadioButtons.RegisterValueChangedCallback(OnToolsRadioButtonsChanged);
        ToolsRadioButtons.SetValueWithoutNotify(0);
        m_FocusManager.RegisterCallbacks(ToolsRadioButtons);

        NumOfCarsInput = root.Q<IntegerField>("NumOfCarsInput");
        Assert.IsNotNull(NumOfCarsInput);
        NumOfCarsInput.value = MasterSystem.GetVehicleCountLimit();
        NumOfCarsInput.RegisterValueChangedCallback(OnNumOfCarsInputChanged);
        m_FocusManager.RegisterCallbacks(NumOfCarsInput);



    }

    private void Update()
    {
        NumOfCarsInput.value = MasterSystem.GetVehicleCountLimit();
    }

    private void OnLoadRoadJsonClicked()
    {
        try
        {
            var extensions = new[] 
            {
                new ExtensionFilter("Road files", "json")
            };
            string[] paths = FileBrowser.Instance.OpenFiles("Open file", "", "", extensions);

            string path = paths[0];
            if (path.Length != 0)
            {
                ConsoleLogUI.Log("Laod road from file: " + path);
                MasterSystem.MessageQueue.Add(new LoadRoadFromJsonMsg(path));
            }
        }
        catch (System.Exception)
        {
        }
    }
    private void OnSaveRoadJsonClicked()
    {
        try
        {
            string path = FileBrowser.Instance.SaveFile("Choose file to save", "", "", "json");

            if (path.Length != 0)
            {
                ConsoleLogUI.Log("Save road from file: " + path);
                MasterSystem.MessageQueue.Add(new SaveRoadFromJsonMsg(path));
            }
        }
        catch (System.Exception)
        {
        }
    }

    private void OnLoadVehiclesJsonClicked()
    {
        try
        {
            var extensions = new[] {
            new ExtensionFilter("Vehicles files", "json")
            };
            string[] paths = FileBrowser.Instance.OpenFiles("Open file", "", "", extensions);

            string path = paths[0];
            if (path.Length != 0)
            {
                ConsoleLogUI.Log("Laod Vehicles from file: " + path);
                MasterSystem.MessageQueue.Add(new LoadVehiclesFromJsonFileMsg(path));
            }
        }
        catch (System.Exception)
        {
        }
    }
    private void OnSaveVehiclesJsonClicked()
    {
        try
        {
            string path = FileBrowser.Instance.SaveFile("Choose file to save", "", "", "json");

            if (path.Length != 0)
            {
                ConsoleLogUI.Log("Save Vehicles from file: " + path);
                MasterSystem.MessageQueue.Add(new SaveVehiclesFromJsonMsg(path));
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

    private void OnNumOfCarsInputChanged(ChangeEvent<int> button)
    {
        int selected = button.newValue;
        ConsoleLogUI.Log($"Number of cars changed to {selected}");
        MasterSystem.SetVehicleCountLimit(selected);
    }
}