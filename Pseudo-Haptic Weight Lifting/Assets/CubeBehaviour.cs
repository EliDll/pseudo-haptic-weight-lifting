using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public class CubeBehaviour : GrabBehaviour
{
    public PickAndPlaceTaskBehaviour Task;
    public GameObject CubeGeometry;

    private Vector3 grabAnchorToCube;
    private Vector3 cubeToGrabAnchor;

    protected override void OnStart()
    {

    }

    protected override void OnStartGrabbing()
    {
        if (primary == Primary.Left)
        {
            LeftHandGrabVisual.SetActive(true);
            LeftHandGrabVisual.GetComponent<AudioSource>().Play();
            LeftHandGrabVisual.transform.SetPositionAndRotation(LeftHandIdleVisual.transform.position, LeftHandIdleVisual.transform.rotation);

            LeftHandIdleVisual.SetActive(false);
        }
        else if (primary == Primary.Right)
        {
            RightHandGrabVisual.SetActive(true);
            RightHandGrabVisual.GetComponent<AudioSource>().Play();
            RightHandGrabVisual.transform.SetPositionAndRotation(RightHandIdleVisual.transform.position, RightHandIdleVisual.transform.rotation);

            RightHandIdleVisual.SetActive(false);
        }

        grabAnchorToCube = grabObjectOrigin.position - grabAnchorOrigin.position;
        cubeToGrabAnchor = grabAnchorToCube * -1;
    }

    protected override void OnStopGrabbing()
    {
        LeftHandIdleVisual.SetActive(true);
        RightHandIdleVisual.SetActive(true);

        if (primary == Primary.Left)
        {
            LeftHandIdleVisual.GetComponent<AudioSource>().Play();
        }
        else if (primary == Primary.Right)
        {
            RightHandIdleVisual.GetComponent<AudioSource>().Play();
        }

        LeftHandGrabVisual.SetActive(false);
        RightHandGrabVisual.SetActive(false);
    }

    protected override CDParams? GetCD()
    {
        return DM.NormalCD;
    }

    private Pose GetScaledDiff(Pose origin, Pose current, CDParams? cd)
    {
        var headCurrent = Calc.GetHeadPose(CameraRig);
        var headPosDiff = headCurrent.position - headOrigin.position;
        var shiftedOrigin = new Pose(origin.position + headPosDiff, origin.rotation);

        ///Target Pos
        var posDiff = current.position - shiftedOrigin.position;
        var scaledPosDiff = cd == null ? posDiff : Vector3.Scale(posDiff, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio));
        var targetPos = shiftedOrigin.position + scaledPosDiff;

        ///Target Forward
        var targetForward = cd == null ? current.forward : Vector3.Slerp(shiftedOrigin.forward, current.forward, cd.RotationalRatio);

        ///Target Up
        var targetUp = cd == null ? current.up : Vector3.Slerp(shiftedOrigin.up, current.up, cd.RotationalRatio);

        return new Pose(targetPos, Quaternion.LookRotation(targetForward, targetUp));
    }

    protected override Pose GetTargetPose(CDParams? cd)
    {
        if (isTracking)
        {
            //Determine target pose based on tracked object

            var origin = grabObjectOrigin;
            var current = Calc.GetPose(TrackedObject.transform);
            var target = GetScaledDiff(origin: origin, current: current, cd);

            //Continuously update anchor position offset to not freeze it on initial grab collider collision
            var currentAnchor = DM.GetGrabAnchorPose(grabAnchor);
            grabAnchorToCube = current.position - currentAnchor.position;
            cubeToGrabAnchor = grabAnchorToCube * -1;

            return target;
        }
        else
        {
            //Determine target pose based on controller

            var origin = grabAnchorOrigin;
            var current = DM.GetGrabAnchorPose(grabAnchor);
            var target = GetScaledDiff(origin: origin, current: current, cd);

            //Note: Since target pose is defined in terms of the controller (grabbing hand) here, we must transform back to the grab object (cube)
            var anchorRotDiff = target.rotation * Quaternion.Inverse(grabAnchorOrigin.rotation);
            return new Pose(target.position + anchorRotDiff * grabAnchorToCube, anchorRotDiff * grabObjectOrigin.rotation);
        }

    }

    protected override void OnContinueGrabbing(Pose next)
    {
        GrabObject.transform.SetPositionAndRotation(next.position, next.rotation);

        //Note: Since the next pose is defined in terms of the grab object (cube), we must transform back to the controller (grabbing hand)
        var grabObjectRotDiff = next.rotation * Quaternion.Inverse(grabObjectOrigin.rotation);


        var handPos = next.position + (isTracking ? Quaternion.identity : grabObjectRotDiff) * cubeToGrabAnchor;
        var handRot = grabObjectRotDiff * grabAnchorOrigin.rotation;

        if(primary == Primary.Left)
        {
            LeftHandGrabVisual.transform.SetPositionAndRotation(handPos, handRot);
        }else if(primary == Primary.Right)
        {
            RightHandGrabVisual.transform.SetPositionAndRotation(handPos, handRot);
        }

        Task.UpdateTask(CubeGeometry, grabAnchor);
    }

    protected void FixedUpdate()
    {
        var targetReached = Task.GetTargetReached();
        var completed = targetReached == 3;

        if (isGrabbing && !completed)
        {
            var leftVisible = LeftHandGrabVisual.transform.position;
            var rightVisible = RightHandGrabVisual.transform.position;
            var primaryVisible = primary == Primary.Left ? leftVisible : rightVisible;
            var secondaryVisible = primary == Primary.Left ? rightVisible : leftVisible;

            var log = new LogEntry
            {
                PrimaryMode = primary,
                PrimaryTracked = DM.GetGrabAnchorPose(grabAnchor).position,
                SecondaryTracked = DM.GetGrabAnchorPose(secondaryAnchor).position,
                PrimaryVisible = primaryVisible,
                SecondaryVisible = secondaryVisible,
                HMD = Calc.GetHeadPose(CameraRig).position,
                EndEffectorVisible = GrabObject.transform.position,
                ShovelLoaded = false, // n/a
                CubeReachedTarget = targetReached,
                GrabCount = grabCount,
                CollisionCount = Task.GetCollisionCount()
            };

            DM.Log(log);
        }
    }
}
