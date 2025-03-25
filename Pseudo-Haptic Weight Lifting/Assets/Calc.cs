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
}