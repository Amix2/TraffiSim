using System;

public interface IAspect
{
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class OptionalLookupAttribute : Attribute
{ }