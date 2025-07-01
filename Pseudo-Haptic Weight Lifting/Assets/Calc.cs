using UnityEditor;
using UnityEngine;
#nullable enable

public static class Calc
{
    public static OVRInput.Button GetGrabButton(this GrabAnchor anchor)
    {
        return anchor switch
        {
            GrabAnchor.RightController => Defs.RightGrabButton,
            GrabAnchor.LeftController => Defs.LeftGrabButton,
            _ => OVRInput.Button.None
        };
    }

    public static bool IsPressed(OVRInput.Button button)
    {
        return OVRInput.Get(button);
    }

    public static Pose GetHeadPose(OVRCameraRig camera)
    {
        var centerEye = camera.centerEyeAnchor.transform;
        return new Pose(centerEye.position, centerEye.rotation);
    }

    public static Pose GetPose(Transform transform)
    {
        return new Pose(transform.position, transform.rotation);
    }

    public static Pose CalculateNextPose(Pose current, Pose target, SpinTwistVelocity currentVelocity, CDParams? cd)
    {
        //If no CD, move directly to targeet
        if (cd == null) return target;

        //Determine max velocity in this frame based on current velocity and constant CD acceleration
        var maxVelocity = new SpinTwistVelocity
        {
            linear = currentVelocity.linear + cd.Acceleration * Time.deltaTime, // m/s
            twist = currentVelocity.twist + cd.TwistAcceleration * Time.deltaTime, // deg/s
            spin = currentVelocity.spin + cd.SpinAcceleration * Time.deltaTime, // deg/s
        };

        //Determine next pose based on max distance and angle deltas in this frame
        var maxLinearDelta = maxVelocity.linear * Time.deltaTime; // m
        var nextPos = Vector3.MoveTowards(current.position, target.position, maxDistanceDelta: maxLinearDelta);

        var maxSpinDelta = maxVelocity.spin * Time.deltaTime; //deg
        var nextForward = Vector3.RotateTowards(current.forward, target.forward, maxRadiansDelta: Mathf.Deg2Rad * maxSpinDelta, maxMagnitudeDelta: 0.0f);

        var maxTwistDelta = maxVelocity.twist * Time.deltaTime; //deg
        var nextUp = Vector3.RotateTowards(current.up, target.up, maxRadiansDelta: Mathf.Deg2Rad * maxTwistDelta, maxMagnitudeDelta: 0.0f);

        return new Pose(nextPos, Quaternion.LookRotation(nextForward, nextUp));
    }

    public static SpinTwistVelocity CalculateVelocity(Pose from, Pose to)
    {
        return new SpinTwistVelocity
        {
            linear = Vector3.Magnitude(to.position - from.position) / Time.deltaTime, // m/s
            spin = Vector3.Angle(to.forward, from.forward) / Time.deltaTime,  // deg/s
            twist = Vector3.Angle(to.up, from.up) / Time.deltaTime, // deg/s
        };
    }

    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}