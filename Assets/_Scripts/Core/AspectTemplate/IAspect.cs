using System;
using UnityEngine;

public interface IAspect
{
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class OptionalLookupAttribute : Attribute { }