using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public enum eToolType : int
{
    Camera, AddNode, Debug
}

public class ChangeToolMsg : ISingleMessage
{
    eToolType m_nToolType;

    public ChangeToolMsg(eToolType nToolType)
    {
        m_nToolType = nToolType;
    }

    public void Execute(MasterSystem masterSystem)
    {
        switch (m_nToolType) 
        { 
        case eToolType.Camera:  masterSystem.SetActiveTool(null); break;
        case eToolType.AddNode: masterSystem.SetActiveTool(new AddRoadObjectsTool()); break;
        case eToolType.Debug:   masterSystem.SetActiveTool(new DebugTool()); break;

        }
    }
}