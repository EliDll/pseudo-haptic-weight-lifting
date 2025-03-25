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

public static class Defs
{
    public const OVRInput.Button LeftGrabButton = OVRInput.Button.PrimaryHandTrigger;
    public const OVRInput.Button RightGrabButton = OVRInput.Button.SecondaryHandTrigger;

    public const OVRInput.Controller LeftHand = OVRInput.Controller.LTouch;
    public const  OVRInput.Controller RightHand = OVRInput.Controller.RTouch;
}