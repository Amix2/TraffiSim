using Unity.Mathematics;



public struct OBB
{
    // based on https://github.com/juj/MathGeoLib/blob/master/src/Geometry/OBB.h

    /// The center position of this OBB.
    /** In the local space of the OBB, the center of this OBB is at (r.x,r.y,r.z), and the OBB is an AABB with
		size 2*r. */
    float3 pos;

    /// Stores half-sizes to x, y and z directions in the local space of this OBB. [similarOverload: pos]
    /** These members should be positive to represent a non-degenerate OBB. */
    float3 r;

    /// Specifies normalized direction vectors for the local axes. [noscript] [similarOverload: pos]
    /** axis[0] specifies the +X direction in the local space of this OBB, axis[1] the +Y direction and axis[2]
		the +Z direction.
		The scale of these vectors is always normalized. The half-length of the OBB along its local axes is
		specified by the vector r.
		The axis vectors must always be orthonormal. Be sure to guarantee that condition holds if you
		directly set to this member variable. */
    float3 axisX;
    float3 axisY;
    float3 axisZ;

    public OBB(float3 pos, float3 size, float3 axisX, float3 axisY, float3 axisZ)
    {
        this.pos = pos;
        this.r = size / 2;
        this.axisX = axisX;
        this.axisY = axisY;
        this.axisZ = axisZ;
    }

    public OBB(float3 pos, float3 size, quaternion quaternion)
    {
        this.pos = pos;
        this.r = size / 2;
        this.axisX = math.mul(quaternion, math.forward());
        this.axisY = math.mul(quaternion, math.up()) ;
        this.axisZ = math.mul(quaternion, math.left());
    }

    public readonly float4x4 GetMatrix() { return new float4x4(new float3x3(axisX * r.x * 2, axisY * r.y * 2, axisZ * r.z * 2), pos); }
}