using System;
using Unity.Entities;

public struct RoadNodeEnt : IEquatable<RoadNodeEnt>, IEquatable<Entity>
{
    public Entity Ent;

    public static implicit operator Entity(RoadNodeEnt val) => val.Ent;
    public static implicit operator RoadNodeEnt(Entity val) => new() { Ent = val };

    public static RoadNodeEnt Null => default;
    public readonly bool IsNull => Ent == Entity.Null;

    public readonly bool Equals(RoadNodeEnt other) => Ent == other.Ent;
    public readonly bool Equals(Entity other) => Ent == other;
    public readonly override bool Equals(object obj) => obj is RoadNodeEnt o && Ent == o.Ent;
    public readonly override int GetHashCode() => Ent.GetHashCode();
    public readonly override string ToString() => $"RoadNode({Ent.Index}:{Ent.Version})";

    public static bool operator ==(RoadNodeEnt a, RoadNodeEnt b) => a.Ent == b.Ent;
    public static bool operator !=(RoadNodeEnt a, RoadNodeEnt b) => a.Ent != b.Ent;
    public static bool operator ==(RoadNodeEnt a, Entity b) => a.Ent == b;
    public static bool operator !=(RoadNodeEnt a, Entity b) => a.Ent != b;
    public static bool operator ==(Entity a, RoadNodeEnt b) => a == b.Ent;
    public static bool operator !=(Entity a, RoadNodeEnt b) => a != b.Ent;
}

public struct RoadSegmentEnt : IEquatable<RoadSegmentEnt>, IEquatable<Entity>
{
    public Entity Ent;

    public static implicit operator Entity(RoadSegmentEnt val) => val.Ent;
    public static implicit operator RoadSegmentEnt(Entity val) => new() { Ent = val };

    public static RoadSegmentEnt Null => default;
    public readonly bool IsNull => Ent == Entity.Null;

    public readonly bool Equals(RoadSegmentEnt other) => Ent == other.Ent;
    public readonly bool Equals(Entity other) => Ent == other;
    public readonly override bool Equals(object obj) => obj is RoadSegmentEnt o && Ent == o.Ent;
    public readonly override int GetHashCode() => Ent.GetHashCode();
    public readonly override string ToString() => $"RoadSegment({Ent.Index}:{Ent.Version})";

    public static bool operator ==(RoadSegmentEnt a, RoadSegmentEnt b) => a.Ent == b.Ent;
    public static bool operator !=(RoadSegmentEnt a, RoadSegmentEnt b) => a.Ent != b.Ent;
    public static bool operator ==(RoadSegmentEnt a, Entity b) => a.Ent == b;
    public static bool operator !=(RoadSegmentEnt a, Entity b) => a.Ent != b;
    public static bool operator ==(Entity a, RoadSegmentEnt b) => a == b.Ent;
    public static bool operator !=(Entity a, RoadSegmentEnt b) => a != b.Ent;
}

public struct RoadPortEnt : IEquatable<RoadPortEnt>, IEquatable<Entity>
{
    public Entity Ent;

    public static implicit operator Entity(RoadPortEnt val) => val.Ent;
    public static implicit operator RoadPortEnt(Entity val) => new() { Ent = val };

    public static RoadPortEnt Null => default;
    public readonly bool IsNull => Ent == Entity.Null;

    public readonly bool Equals(RoadPortEnt other) => Ent == other.Ent;
    public readonly bool Equals(Entity other) => Ent == other;
    public readonly override bool Equals(object obj) => obj is RoadPortEnt o && Ent == o.Ent;
    public readonly override int GetHashCode() => Ent.GetHashCode();
    public readonly override string ToString() => $"RoadPort({Ent.Index}:{Ent.Version})";

    public static bool operator ==(RoadPortEnt a, RoadPortEnt b) => a.Ent == b.Ent;
    public static bool operator !=(RoadPortEnt a, RoadPortEnt b) => a.Ent != b.Ent;
    public static bool operator ==(RoadPortEnt a, Entity b) => a.Ent == b;
    public static bool operator !=(RoadPortEnt a, Entity b) => a.Ent != b;
    public static bool operator ==(Entity a, RoadPortEnt b) => a == b.Ent;
    public static bool operator !=(Entity a, RoadPortEnt b) => a != b.Ent;
}

public struct RoadLaneEnt : IEquatable<RoadLaneEnt>, IEquatable<Entity>
{
    public Entity Ent;

    public static implicit operator Entity(RoadLaneEnt val) => val.Ent;
    public static implicit operator RoadLaneEnt(Entity val) => new() { Ent = val };

    public static RoadLaneEnt Null => default;
    public readonly bool IsNull => Ent == Entity.Null;

    public readonly bool Equals(RoadLaneEnt other) => Ent == other.Ent;
    public readonly bool Equals(Entity other) => Ent == other;
    public readonly override bool Equals(object obj) => obj is RoadLaneEnt o && Ent == o.Ent;
    public readonly override int GetHashCode() => Ent.GetHashCode();
    public readonly override string ToString() => $"RoadLane({Ent.Index}:{Ent.Version})";

    public static bool operator ==(RoadLaneEnt a, RoadLaneEnt b) => a.Ent == b.Ent;
    public static bool operator !=(RoadLaneEnt a, RoadLaneEnt b) => a.Ent != b.Ent;
    public static bool operator ==(RoadLaneEnt a, Entity b) => a.Ent == b;
    public static bool operator !=(RoadLaneEnt a, Entity b) => a.Ent != b;
    public static bool operator ==(Entity a, RoadLaneEnt b) => a == b.Ent;
    public static bool operator !=(Entity a, RoadLaneEnt b) => a != b.Ent;
}