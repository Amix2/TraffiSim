using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class AddNodeTool : ToolBase
{
    public override void OnUpdate(MasterSystem masterSystem)
    {
        if(Input.GetMouseButtonDown(0))
        {
            var hitPos = GetHitUnderMouse(masterSystem, LayerMask.Terrain);
            if (hitPos.Entity == Entity.Null) return;
            ConsoleLogUI.Log(hitPos.Position);
            masterSystem.AddSpawnNodeOrder(hitPos.Position);
        }
    }
}