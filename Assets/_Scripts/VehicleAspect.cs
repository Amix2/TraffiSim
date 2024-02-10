using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using System.Security.Cryptography;
using UnityEngine.Video;

public readonly partial struct VehicleAspect : IAspect
{
    public readonly Entity Entity;
    public readonly RefRO<VehicleTag> Tag;
    public readonly RefRW<Velocity> VelocityC;
    public readonly RefRW<DestinationPosition> DestinationPosition;
    public readonly RefRW<Acceleration> Acceleration;
    public readonly RefRW<MaxVelocity> MaxVelocity;
    public readonly RefRW<LastStepPosition> LastStepPosition;
    public readonly RefRW<LastStepOccupiedEdge> LastStepOccupiedEdge;
    public readonly RefRW<LocalTransform> LocalTransform;
    public readonly DynamicBuffer<PathBuffer> PathBuffer;

    public bool IsAtDestination(float rangeSq)
    { return (DestinationPosition.ValueRO.Value - LocalTransform.ValueRO.Position).lengthsq() < rangeSq; }

    public float3 Position => LocalTransform.ValueRO.Position;
    public float3 Destination => DestinationPosition.ValueRO;
    public float LinVelocity
    {
        get { return VelocityC.ValueRO.Value; }
        set { VelocityC.ValueRW.Value = value; }
    }

    public OBB GetObb()
    { return new OBB(LocalTransform.ValueRO.Position, new float3(10, 5, 5), LocalTransform.ValueRO.Rotation); }

    public NativeArray<float3> GetPath(Allocator allocator)
    {
        NativeArray<float3> arr = new NativeArray<float3>(PathBuffer.Length + 1, allocator);
        arr[0] = Position;
        for (int i = 0; i < PathBuffer.Length; i++)
            arr[i + 1] = PathBuffer[i].Position;
        return arr;
    }

    public struct PathIntersection
    {
        public float3 MyPosition;
        public float MyDistance;
        public float3 OtherPosition;
        public float OtherDistance;
        public float IntersectionDistance;
        public Entity EdgeEnt;

        public static PathIntersection Null => new PathIntersection { IntersectionDistance = -1 };
        public readonly bool IsNull => IntersectionDistance < 0;
    }

    public PathIntersection GetPathIntersection(VehicleAspect other, float margin, float range)
    {
        float myRangeLeft = range;
        for (int i = 0; i < PathBuffer.Length && myRangeLeft > 0.01f; i++)
        {
            float3 myA = i == 0 ? Position : PathBuffer[i - 1].Position;
            float3 myB = PathBuffer[i].Position;
            if((myA - myB).lengthsq() > myRangeLeft * myRangeLeft)
                myB = (myB - myA).norm() * myRangeLeft + myA;
            float otherRangeLeft = range;
            for (int j = 0; j < other.PathBuffer.Length && otherRangeLeft > 0.01f; j++)
            {
                float3 otherA = j == 0 ? other.Position : other.PathBuffer[j - 1].Position;
                float3 otherB = other.PathBuffer[j].Position;

                if ((otherA - otherB).lengthsq() > myRangeLeft * myRangeLeft)
                    otherB = (otherB - otherA).norm() * myRangeLeft + otherA;

                var closestHit = MathHelper.NearestPointsOnLineSegments(myA, myB, otherA, otherB);

                if (closestHit.Distance < margin)
                    return new PathIntersection 
                    { 
                        MyPosition = closestHit.PointOnA, 
                        MyDistance = range - myRangeLeft + (myA - closestHit.PointOnA).length(),
                        OtherPosition = closestHit.PointOnB, 
                        OtherDistance = range - otherRangeLeft + (otherA - closestHit.PointOnB).length(),
                        IntersectionDistance = closestHit.Distance,
                        EdgeEnt = PathBuffer[i].EdgeEnt };
                otherRangeLeft -= (otherA - otherB).length();
            }
            myRangeLeft -= (myA - myB).length();
        }
        return PathIntersection.Null;
    }
}