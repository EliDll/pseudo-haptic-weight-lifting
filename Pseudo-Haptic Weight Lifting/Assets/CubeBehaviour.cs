using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public class CubeBehaviour : GrabBehaviour
{
    public BasicTaskBehaviour Task;

    private Pose controllerOrigin;
    private Vector3 controllerToCube;
    private Vector3 cubeToController;

    protected override void OnStart()
    {

    }

    protected override void OnStartGrabbing(OVRInput.Controller controller)
    {
        var controllerPose = Calc.GetControllerPose(controller);
        GrabbingHand.transform.SetPositionAndRotation(controllerPose.position, controllerPose.rotation); //Align hand visual with controller
        controllerOrigin = controllerPose;

        controllerToCube = GrabObject.transform.position - GrabbingHand.transform.position;
        cubeToController = controllerToCube * -1;
    }

    protected override void OnStopGrabbing()
    {

    }

    protected override CDParams? GetCD()
    {
        return DM.NormalCD;
    }

    protected override Pose GetTargetPose(CDParams? cd)
    {
        var current = Calc.GetControllerPose(grabbingController);

        var headCurrent = Calc.GetHeadPose(CameraRig);
        var headPosDiff = headCurrent.position - headOrigin.position;

        var origin = new Pose(controllerOrigin.position + headPosDiff, controllerOrigin.rotation);

        ///Target Pos
        var posDiff = current.position - origin.position;
        var scaledPosDiff = cd == null ? posDiff : Vector3.Scale(posDiff, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio));
        var targetPos = origin.position + scaledPosDiff;

        ///Target Forward
        var targetForward = cd == null ? current.forward : Vector3.Slerp(origin.forward, current.forward, cd.RotationalRatio);

        ///Target Up
        var targetUp = cd == null ? current.up : Vector3.Slerp(origin.up, current.up, cd.RotationalRatio);

        //Note: Since target pose is defined in terms of the controller (grabbing hand) here, we must transform back to the grab object (cube)

        var targetRot = Quaternion.LookRotation(targetForward, targetUp);
        var controllerRotDiff = targetRot * Quaternion.Inverse(controllerOrigin.rotation);

        return new Pose(targetPos + controllerRotDiff * controllerToCube, controllerRotDiff * grabObjectOrigin.rotation);
    }

    protected override void OnContinueGrabbing(Pose next)
    {
        GrabObject.transform.SetPositionAndRotation(next.position, next.rotation);

        //Note: Since the next pose is defined in terms of the grab object (cube), we must transform back to the controller (grabbing hand)

        var grabObjectRotDiff = next.rotation * Quaternion.Inverse(grabObjectOrigin.rotation);
        GrabbingHand.transform.SetPositionAndRotation(next.position + grabObjectRotDiff * cubeToController, grabObjectRotDiff * controllerOrigin.rotation);

        Task.UpdateTask(GrabObject, grabbingController);
    }
}
