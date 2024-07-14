using Unity.Mathematics;

public struct OBB
{
    // based on https://github.com/juj/MathGeoLib/blob/master/src/Geometry/OBB.h

    /// The center position of this OBB.
    /** In the local space of the OBB, the center of this OBB is at (r.x,r.y,r.z), and the OBB is an AABB with
		size 2*r. */
    private float3 pos;
    public float3 Position => pos;

    /// Stores half-sizes to x, y and z directions in the local space of this OBB. [similarOverload: pos]
    /** These members should be positive to represent a non-degenerate OBB. */
    private float3 r;

    /// Specifies normalized direction vectors for the local axes. [noscript] [similarOverload: pos]
    /** axis[0] specifies the +X direction in the local space of this OBB, axis[1] the +Y direction and axis[2]
		the +Z direction.
		The scale of these vectors is always normalized. The half-length of the OBB along its local axes is
		specified by the vector r.
		The axis vectors must always be orthonormal. Be sure to guarantee that condition holds if you
		directly set to this member variable. */

    private float3x3 axis;

    public OBB(float3 pos, float3 size, float3 axisX, float3 axisY, float3 axisZ)
    {
        this.pos = pos;
        this.r = size / 2;
        axis = new float3x3();
        this.axis[0] = axisX;
        this.axis[1] = axisY;
        this.axis[2] = axisZ;
    }

    public OBB(float3 pos, float3 size, quaternion quaternion)
    {
        this.pos = pos;
        this.r = size / 2;
        axis = new float3x3();
        this.axis[0] = math.mul(quaternion, math.forward());
        this.axis[1] = math.mul(quaternion, math.up());
        this.axis[2] = math.mul(quaternion, math.left());
    }

    public readonly float4x4 GetMatrix()
    { return new float4x4(new float3x3(axis[0] * r.x * 2, axis[1] * r.y * 2, axis[2] * r.z * 2), pos); }

    public bool Intersects(OBB b, float epsilon)
    {
        //assume(pos.IsFinite());
        //assume(b.pos.IsFinite());
        //assume(vec::AreOrthogonal(axis[0], axis[1], axis[2]));
        //assume(vec::AreOrthogonal(b.axis[0], b.axis[1], b.axis[2]));

        // Benchmark 'OBBIntersectsOBB_Random': OBB::Intersects(OBB) Random
        //    Best: 100.830 nsecs / 171.37 ticks, Avg: 110.533 nsecs, Worst: 155.582 nsecs
        // Benchmark 'OBBIntersectsOBB_Positive': OBB::Intersects(OBB) Positive
        //    Best: 95.771 nsecs / 162.739 ticks, Avg: 107.935 nsecs, Worst: 173.110 nsecs

        // Generate a rotation matrix that transforms from world space to this OBB's coordinate space.
        float3x3 R = new();
        for (int i = 0; i < 3; ++i)
            for (int j = 0; j < 3; ++j)
                R[i][j] = math.dot(axis[i], b.axis[j]);

        float3 t = b.pos - pos;
        // Express the translation vector in a's coordinate frame.
        t = new float3(math.dot(t, axis[0]), math.dot(t, axis[1]), math.dot(t, axis[2]));

        float3x3 AbsR = new();
        for (int i = 0; i < 3; ++i)
            for (int j = 0; j < 3; ++j)
                AbsR[i][j] = math.abs(R[i][j]) + epsilon;

        float ra;
        float rb;
        // Test the three major axes of this OBB.
        for (int i = 0; i < 3; ++i)
        {
            ra = r[i];
            rb = b.r.x * AbsR[i][0] + b.r.y * AbsR[i][1] + b.r.z * AbsR[i][2];
            if (math.abs(t[i]) > ra + rb)
                return false;
        }

        // Test the three major axes of the OBB b.
        for (int i = 0; i < 3; ++i)
        {
            ra = r[0] * AbsR[0][i] + r[1] * AbsR[1][i] + r[2] * AbsR[2][i];
            rb = b.r[i];
            if (math.abs(t.x * R[0][i] + t.y * R[1][i] + t.z * R[2][i]) > ra + rb)
                return false;
        }

        // Test the 9 different cross-axes.

        // A.x <cross> B.x
        ra = r.y * AbsR[2][0] + r.z * AbsR[1][0];
        rb = b.r.y * AbsR[0][2] + b.r.z * AbsR[0][1];
        if (math.abs(t.z * R[1][0] - t.y * R[2][0]) > ra + rb)
            return false;

        // A.x < cross> B.y
        ra = r.y * AbsR[2][1] + r.z * AbsR[1][1];
        rb = b.r.x * AbsR[0][2] + b.r.z * AbsR[0][0];
        if (math.abs(t.z * R[1][1] - t.y * R[2][1]) > ra + rb)
            return false;

        // A.x <cross> B.z
        ra = r.y * AbsR[2][2] + r.z * AbsR[1][2];
        rb = b.r.x * AbsR[0][1] + b.r.y * AbsR[0][0];
        if (math.abs(t.z * R[1][2] - t.y * R[2][2]) > ra + rb)
            return false;

        // A.y <cross> B.x
        ra = r.x * AbsR[2][0] + r.z * AbsR[0][0];
        rb = b.r.y * AbsR[1][2] + b.r.z * AbsR[1][1];
        if (math.abs(t.x * R[2][0] - t.z * R[0][0]) > ra + rb)
            return false;

        // A.y <cross> B.y
        ra = r.x * AbsR[2][1] + r.z * AbsR[0][1];
        rb = b.r.x * AbsR[1][2] + b.r.z * AbsR[1][0];
        if (math.abs(t.x * R[2][1] - t.z * R[0][1]) > ra + rb)
            return false;

        // A.y <cross> B.z
        ra = r.x * AbsR[2][2] + r.z * AbsR[0][2];
        rb = b.r.x * AbsR[1][1] + b.r.y * AbsR[1][0];
        if (math.abs(t.x * R[2][2] - t.z * R[0][2]) > ra + rb)
            return false;

        // A.z <cross> B.x
        ra = r.x * AbsR[1][0] + r.y * AbsR[0][0];
        rb = b.r.y * AbsR[2][2] + b.r.z * AbsR[2][1];
        if (math.abs(t.y * R[0][0] - t.x * R[1][0]) > ra + rb)
            return false;

        // A.z <cross> B.y
        ra = r.x * AbsR[1][1] + r.y * AbsR[0][1];
        rb = b.r.x * AbsR[2][2] + b.r.z * AbsR[2][0];
        if (math.abs(t.y * R[0][1] - t.x * R[1][1]) > ra + rb)
            return false;

        // A.z <cross> B.z
        ra = r.x * AbsR[1][2] + r.y * AbsR[0][2];
        rb = b.r.x * AbsR[2][1] + b.r.y * AbsR[2][0];
        if (math.abs(t.y * R[0][2] - t.x * R[1][2]) > ra + rb)
            return false;

        // No separating axis exists, so the two OBB don't intersect.
        return true;
    }
}