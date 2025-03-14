using UnityEditor;
using UnityEngine;
#nullable enable

public record CDRatio
{
    public float Horizontal;
    public float Vertical;
    public float Rotational;
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

    public static VirtualTransform Interpolate(VirtualTransform from, VirtualTransform to, CDRatio cd)
    {
        var posDiff = to.pos - from.pos;
        var scaledPosDiff = Vector3.Scale(posDiff, new Vector3(x: cd.Horizontal, y: cd.Vertical, z: cd.Horizontal));

        return new VirtualTransform
        {
            pos = from.pos + scaledPosDiff,
            rot = Quaternion.Slerp(from.rot, to.rot, cd.Rotational),
        };
    }

    public static Vector3 InterpolateByCD(Vector3 from, Vector3 to, CDRatio cd)
    {
        var diff = to - from;
        var scaledDiff = Vector3.Scale(diff, new Vector3(x: cd.Horizontal, y: cd.Vertical, z: cd.Horizontal));
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

    public static VirtualTransform GetScaledDiff(VirtualTransform from, VirtualTransform to, CDRatio cd) => ScaleBy(GetDiff(from, to), cd);

    public static VirtualTransform GetScaledDiff(VirtualTransform from, VirtualTransform to, float ratio) => ScaleBy(GetDiff(from, to), ratio);

    public static VirtualTransform GetDiff(VirtualTransform from, VirtualTransform to) => new VirtualTransform
    {
        pos = to.pos - from.pos,
        rot = to.rot * Quaternion.Inverse(from.rot),
    };

    private static VirtualTransform ScaleBy(VirtualTransform input, CDRatio cd) => new VirtualTransform
    {
        pos = Vector3.Scale(input.pos, new Vector3(x: cd.Horizontal, y: cd.Vertical, z: cd.Horizontal)),
        rot = Quaternion.Slerp(Quaternion.identity, input.rot, cd.Rotational)
    };

    private static VirtualTransform ScaleBy(VirtualTransform input, float ratio) => new VirtualTransform
    {
        pos = Vector3.Scale(input.pos, new Vector3(ratio, ratio, ratio)),
        rot = Quaternion.Slerp(Quaternion.identity, input.rot, ratio)
    };

    public static Vector3 ScaleByCD(Vector3 input, CDRatio cd) => Vector3.Scale(input, new Vector3(x: cd.Horizontal, y: cd.Vertical, z: cd.Horizontal));
}