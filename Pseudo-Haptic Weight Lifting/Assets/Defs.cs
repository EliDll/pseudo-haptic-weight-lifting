using System;
using UnityEditor;
using UnityEngine;
#nullable enable

public enum GrabAnchor
{
    LeftController,
    RightController,
    LeftHand,
    RightHand,
    None
}

public enum PrimaryHand
{
    Left,
    Right,
    None
}

public enum Condition
{
    C0,
    C1,
    C2,
    P0,
    P1,
    P2
}

public enum Experiment
{
    PickAndPlace,
    Shovel,
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
        Acceleration = 7f,
        RotationalRatio = 0.9f,
        SpinAcceleration = 630f,
        TwistAcceleration = 720f,
    };

    public static CDParams Subtle_Loaded = new CDParams
    {
        HorizontalRatio = 0.85f,
        VerticalRatio = 0.75f,
        Acceleration = 6f,
        RotationalRatio = 0.85f,
        SpinAcceleration = 540f,
        TwistAcceleration = 630f,
    };

    public static CDParams Pronounced = new CDParams
    {
        HorizontalRatio = 0.8f,
        VerticalRatio = 0.7f,
        Acceleration = 5f,
        RotationalRatio = 0.8f,
        SpinAcceleration = 450f,
        TwistAcceleration = 540f,
    };

    public static CDParams Pronounced_Loaded = new CDParams
    {
        HorizontalRatio = 0.75f,
        VerticalRatio = 0.65f,
        Acceleration = 4f,
        RotationalRatio = 0.75f,
        SpinAcceleration = 360f,
        TwistAcceleration = 450f,
    };
}

public record LogEntry
{
    public PrimaryHand PrimaryMode;
    public Vector3 PrimaryTracked;
    public Vector3 SecondaryTracked;
    public Vector3 PrimaryVisible;
    public Vector3 SecondaryVisible;
    public Vector3 HMD;
    public Vector3 EndEffectorVisible;
    public bool ShovelLoaded;
    public int CubeReachedTarget;
    public int GrabCount;
    public int CollisionCount;
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

    public const OVRInput.Button LeftMenuButton = OVRInput.Button.Start;
    public const OVRInput.Button LeftThumbstickPress = OVRInput.Button.PrimaryThumbstick;
    public const OVRInput.Button RightThumbstickPress = OVRInput.Button.SecondaryThumbstick;
}
