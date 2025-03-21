using UnityEditor;
using UnityEngine;
#nullable enable

public record CDParams
{
    public float HorizontalRatio;
    public float VerticalRatio;
    public float RotationalRatio;
    /// <summary>
    /// m/s^2
    /// </summary>
    public float Acceleration;
    /// <summary>
    /// deg/s^2
    /// </summary>
    public float SpinAcceleration;
    /// <summary>
    /// deg/s^2
    /// </summary>
    public float TwistAcceleration;
}

public record VirtualTransform
{
    public Vector3 pos;
    public Quaternion rot;
}

public record LRTransform
{
    public Vector3 pos;
    public Vector3 forward;
    public Vector3 up;
}

public static class Calc
{
    public static VirtualTransform AddDiff(VirtualTransform current, VirtualTransform diff) => new VirtualTransform
    {
        pos = current.pos + diff.pos,
        rot = diff.rot * current.rot,
    };

    public static VirtualTransform GetControllerTransform(OVRInput.Controller controller) => new VirtualTransform
    {
        pos = OVRInput.GetLocalControllerPosition(controller),
        rot = OVRInput.GetLocalControllerRotation(controller),
    };

    public static VirtualTransform Interpolate(VirtualTransform from, VirtualTransform to, CDParams cd)
    {
        var posDiff = to.pos - from.pos;
        var scaledPosDiff = Vector3.Scale(posDiff, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio));

        return new VirtualTransform
        {
            pos = from.pos + scaledPosDiff,
            rot = Quaternion.Slerp(from.rot, to.rot, cd.RotationalRatio),
        };
    }

    public static Vector3 InterpolateByCD(Vector3 from, Vector3 to, CDParams cd)
    {
        var diff = to - from;
        var scaledDiff = Vector3.Scale(diff, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio));
        return from + scaledDiff;
    }

    public static VirtualTransform Interpolate(VirtualTransform from, VirtualTransform to, float fraction)
    {
        return new VirtualTransform
        {
            pos = Vector3.Lerp(from.pos, to.pos, fraction),
            rot = Quaternion.Slerp(from.rot, to.rot, fraction),
        };
    }

    public static VirtualTransform GetScaledDiff(VirtualTransform from, VirtualTransform to, CDParams cd) => ScaleBy(GetDiff(from, to), cd);

    public static VirtualTransform GetScaledDiff(VirtualTransform from, VirtualTransform to, float ratio) => ScaleBy(GetDiff(from, to), ratio);

    public static VirtualTransform GetDiff(VirtualTransform from, VirtualTransform to) => new VirtualTransform
    {
        pos = to.pos - from.pos,
        rot = to.rot * Quaternion.Inverse(from.rot),
    };

    private static VirtualTransform ScaleBy(VirtualTransform input, CDParams cd) => new VirtualTransform
    {
        pos = Vector3.Scale(input.pos, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio)),
        rot = Quaternion.Slerp(Quaternion.identity, input.rot, cd.RotationalRatio)
    };

    private static VirtualTransform ScaleBy(VirtualTransform input, float ratio) => new VirtualTransform
    {
        pos = Vector3.Scale(input.pos, new Vector3(ratio, ratio, ratio)),
        rot = Quaternion.Slerp(Quaternion.identity, input.rot, ratio)
    };

    public static Vector3 ScaleByCD(Vector3 input, CDParams cd) => Vector3.Scale(input, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio));
}