using UnityEditor;
using UnityEngine;
#nullable enable

public static class Calc
{
    public static bool IsPressed(OVRInput.Button button)
    {
        return OVRInput.Get(button);
    }

    public static Pose GetControllerPose(OVRInput.Controller controller)
    {
        var pos = OVRInput.GetLocalControllerPosition(controller);
        var rot = OVRInput.GetLocalControllerRotation(controller);

        return new Pose(pos, rot);
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

    public static Pose GetNextPose(Pose current, Pose target, SpinTwistVelocity currentVelocity, CDParams? cd)
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

    public static bool PoseAlike(Pose from, Pose to)
    {
        return Vector3.Distance(from.position, to.position) < 0.001f //1mm
            && Vector3.Angle(from.forward, to.forward) < 0.1f //0.1deg
            && Vector3.Angle(from.up, to.up) < 0.1f //0.1deg
            ;
    }
}