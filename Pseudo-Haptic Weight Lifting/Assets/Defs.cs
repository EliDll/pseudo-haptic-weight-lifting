using UnityEditor;
using UnityEngine;
#nullable enable

public enum CDIntensity
{
    None,
    Subtle,
    Pronounced
}

public enum GrabAnchor
{
    LeftController,
    RightController,
    LeftHand,
    RightHand,
    None
}

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

    public static CDParams Subtle = new CDParams
    {
        HorizontalRatio = 0.9f,
        VerticalRatio = 0.8f,
        RotationalRatio = 0.9f,
        Acceleration = 7f,
        SpinAcceleration = 450f,
        TwistAcceleration = 450f,
    };

    public static CDParams Subtle_Loaded = new CDParams
    {
        HorizontalRatio = 0.85f,
        VerticalRatio = 0.75f,
        RotationalRatio = 0.85f,
        Acceleration = 6f,
        SpinAcceleration = 405f,
        TwistAcceleration = 405f,
    };

    public static CDParams Pronounced = new CDParams
    {
        HorizontalRatio = 0.8f,
        VerticalRatio = 0.7f,
        RotationalRatio = 0.8f,
        Acceleration = 5f,
        SpinAcceleration = 360f,
        TwistAcceleration = 360f,
    };

    public static CDParams Pronounced_Loaded = new CDParams
    {
        HorizontalRatio = 0.7f,
        VerticalRatio = 0.6f,
        RotationalRatio = 0.7f,
        Acceleration = 4f,
        SpinAcceleration = 315f,
        TwistAcceleration = 315f,
    };
}

public record SpinTwistVelocity
{
    /// <summary>
    /// m/s
    /// </summary>
    public float linear;
    /// <summary>
    /// deg/s
    /// </summary>
    public float spin;
    /// <summary>
    /// deg/s
    /// </summary>
    public float twist;

    public static SpinTwistVelocity Zero = new SpinTwistVelocity
    {
        linear = 0,
        spin = 0,
        twist = 0,
    };
}

public static class Defs
{
    public const OVRInput.Button LeftGrabButton = OVRInput.Button.PrimaryHandTrigger;
    public const OVRInput.Button RightGrabButton = OVRInput.Button.SecondaryHandTrigger;

    public const OVRInput.Controller LeftController = OVRInput.Controller.LTouch;
    public const OVRInput.Controller RightController = OVRInput.Controller.RTouch;

    public const OVRInput.Button ButtonA = OVRInput.Button.One;
    public const OVRInput.Button ButtonB = OVRInput.Button.Two;
    public const OVRInput.Button ButtonX = OVRInput.Button.Three;
    public const OVRInput.Button ButtonY = OVRInput.Button.Four;
}