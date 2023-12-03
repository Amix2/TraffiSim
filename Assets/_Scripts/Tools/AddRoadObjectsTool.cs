using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class AddRoadObjectsTool : ToolBase
{
    private float PressedMouseScreenMove;
    private float2 LastMouseScreenPosition;
    private Entity StartNode;

    public override void OnUpdate(MasterSystem masterSystem)
    {
        if (Input.GetMouseButtonDown(0))
        {
            var hitPos = GetHitUnderMouse(masterSystem, LayerMask.RoadNode);
            StartNode = hitPos.Entity;
        }
        if (Input.GetMouseButtonUp(0))
        {
            var nodeHit = GetHitUnderMouse(masterSystem, LayerMask.RoadNode);
            if (nodeHit.Entity != Entity.Null && StartNode != Entity.Null && StartNode != nodeHit.Entity)
            {
                masterSystem.AddSpawnEdgeOrder(StartNode, nodeHit.Entity);
            }
            else if (PressedMouseScreenMove < 3)
            {
                var terrainHitPos = GetHitUnderMouse(masterSystem, LayerMask.Terrain);
                if (terrainHitPos.Entity == Entity.Null) return;
                masterSystem.AddSpawnNodeOrder(terrainHitPos.Position);
            }
        }
        if (Input.GetMouseButton(0))
        {
            PressedMouseScreenMove += (LastMouseScreenPosition - MouseScreenPosition).length();
        }
        else
        {
            PressedMouseScreenMove = 0;
        }
        LastMouseScreenPosition = MouseScreenPosition;
    }
}