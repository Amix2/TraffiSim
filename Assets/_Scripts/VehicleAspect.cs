using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using System.Security.Cryptography;
using UnityEngine.Video;
using UnityEditor.ShaderGraph.Internal;
using Unity.Burst.CompilerServices;

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

    public OBB GetObb() { return new OBB(LocalTransform.ValueRO.Position, GetSize(), LocalTransform.ValueRO.Rotation); }
    public float3 GetSize() { return new float3(10, 5, 5); }

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

            float3Pair myEdgePath1 = GetEdgePath(myA, myB, GetSize().z, -1);
            float3Pair myEdgePath2 = GetEdgePath(myA, myB, GetSize().z, 1);

            float otherRangeLeft = range;
            for (int j = 0; j < other.PathBuffer.Length && otherRangeLeft > 0.01f; j++)
            {
                float3 otherA = j == 0 ? other.Position : other.PathBuffer[j - 1].Position;
                float3 otherB = other.PathBuffer[j].Position;

                if ((otherA - otherB).lengthsq() > myRangeLeft * myRangeLeft)
                    otherB = (otherB - otherA).norm() * myRangeLeft + otherA;

                float3Pair otherEdgePath1 = GetEdgePath(otherA, otherB, other.GetSize().z, -1);
                float3Pair otherEdgePath2 = GetEdgePath(otherA, otherB, other.GetSize().z, 1);

                NearestPointsOnLineSegmentsRes nearestHit = new();
                float nearestHitTravelSq = float.MaxValue;

                for (int q=0; q<4; q++)
                {
                    float3Pair myEdgePath = q % 2 == 0 ? myEdgePath1 : myEdgePath2;
                    float3Pair otherEdgePath = q < 2 ? otherEdgePath1 : otherEdgePath2;
                    var hit = MathHelper.NearestPointsOnLineSegments(myEdgePath, otherEdgePath);
                    float hitSeparation = math.distance(hit.PointOnA, hit.PointOnB);
                    if (hitSeparation > margin)
                        continue;

                    float travel = math.distancesq(hit.PointOnA, myEdgePath.A);
                    if(travel < nearestHitTravelSq)
                    {
                        nearestHit = hit;
                        nearestHitTravelSq = travel;
                    }
                }
                if(nearestHitTravelSq != float.MaxValue)
                {
                    float travel = math.sqrt(nearestHitTravelSq);
                    return new PathIntersection
                    {
                        MyPosition = nearestHit.PointOnA,
                        MyDistance = range - myRangeLeft + (myA - nearestHit.PointOnA).length(),
                        OtherPosition = nearestHit.PointOnB,
                        OtherDistance = range - otherRangeLeft + (otherA - nearestHit.PointOnB).length(),
                        IntersectionDistance = travel,
                        EdgeEnt = PathBuffer[i].EdgeEnt
                    };
                }

                otherRangeLeft -= (otherA - otherB).length();
            }
            myRangeLeft -= (myA - myB).length();
        }
        return PathIntersection.Null;

        static float3Pair GetEdgePath(float3 A, float3 B, float size, int side) // side = {-1, 1}
        {
            quaternion quaternion = quaternion.LookRotation(B - A, new float3(0, 1, 0));
            float3 offset = math.mul(quaternion, math.right()) * side * size;
            return new float3Pair()
            {  
                A = A + offset,
                B = B + offset
            };
        };
    }
}