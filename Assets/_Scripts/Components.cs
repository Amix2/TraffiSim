using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct DocumentComponent : IComponentData
{
    public Entity VehiclePrefab;
}