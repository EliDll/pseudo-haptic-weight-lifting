﻿using UnityEditor;
using UnityEngine;
#nullable enable

public abstract class GrabBehaviour : MonoBehaviour
{
    public DungeonMasterBehaviour DM;
    public OVRCameraRig CameraRig;
    public GameObject Parent;
    public GameObject GrabObject;
    public GameObject GrabBoundary;

    public GameObject GrabbingHandVisual;
    public GameObject SecondaryHandVisual;

    public GameObject RealGhost;
    public GameObject TargetGhost;

    public GameObject TrackedObject;

    protected OVRInput.Button grabButton = OVRInput.Button.None;

    protected GrabAnchor grabAnchor = GrabAnchor.None;
    protected GrabAnchor secondaryAnchor = GrabAnchor.None;

    protected Pose grabObjectOrigin;
    protected Pose grabAnchorOrigin;
    protected Pose secondaryAnchorOrigin;
    protected Pose headOrigin;

    protected Vector3 grabObjectTravelDirection = Vector3.zero;

    protected SpinTwistVelocity grabObjectVelocity = SpinTwistVelocity.Zero;

    private bool isGrabbing = false;
    private bool isHighlighted = false;

    protected bool isTracking = false;

    protected abstract void OnStart();
    protected void Start()
    {
        GrabbingHandVisual.SetActive(false);
        SecondaryHandVisual.SetActive(false);

        GrabBoundary.GetComponent<Renderer>().enabled = false;

        RealGhost.SetActive(false);
        TargetGhost.SetActive(false);

        //Only check for tracking mode at startup
        if (DM.IsTrackingEnabled())
        {
            isTracking = true;
            GrabObject.GetComponent<Rigidbody>().isKinematic = true; //ignore physiccs for rigidbody entirely
        }

        OnStart();
    }

    private void Highlight()
    {
        GrabBoundary.GetComponent<Renderer>().enabled = true;
        isHighlighted = true;
    }

    private void Unhighlight()
    {
        GrabBoundary.GetComponent<Renderer>().enabled = false;
        isHighlighted = false;
    }

    protected abstract void OnStartGrabbing();
    private void StartGrabbing(GrabAnchor anchor, OVRInput.Button button)
    {
        isGrabbing = true;

        GrabbingHandVisual.SetActive(true);
        SecondaryHandVisual.SetActive(true);

        grabObjectVelocity = SpinTwistVelocity.Zero;

        Unhighlight();

        grabButton = button;

        grabAnchor = anchor;
        secondaryAnchor = anchor switch {
            GrabAnchor.LeftController => GrabAnchor.RightController,
            GrabAnchor.RightController => GrabAnchor.LeftController,
            GrabAnchor.LeftHand => GrabAnchor.RightHand,
            GrabAnchor.RightHand => GrabAnchor.LeftHand,
            _ => GrabAnchor.None
        };

        grabAnchorOrigin = DM.GetGrabAnchorPose(grabAnchor);
        secondaryAnchorOrigin = DM.GetGrabAnchorPose(secondaryAnchor);

        grabObjectOrigin = Calc.GetPose(GrabObject.transform);
        headOrigin = Calc.GetHeadPose(CameraRig);

        if (!isTracking)
        {
            GrabObject.GetComponent<Rigidbody>().isKinematic = true; //ignore physiccs for rigidbody whhile grabbing
            DM.TryVibrate(anchor);
        }

        OnStartGrabbing();
    }

    protected abstract void OnStopGrabbing();
    private void StopGrabbing()
    {
        isGrabbing = false;

        GrabbingHandVisual.SetActive(false);
        SecondaryHandVisual.SetActive(false);

        if (!isTracking)
        {
            //Reactivate physics for grab object
            var grabObjectRigidbody = GrabObject.GetComponent<Rigidbody>();
            grabObjectRigidbody.isKinematic = false;

            //Yeet grab object based on current travel direction and velocity
            var velocityVec = grabObjectTravelDirection * grabObjectVelocity.linear * 0.5f;
            grabObjectRigidbody.AddForce(velocityVec, ForceMode.VelocityChange);

            DM.TryVibrate(grabAnchor);
        }

        grabButton = OVRInput.Button.None;

        grabAnchor = GrabAnchor.None;
        secondaryAnchor = GrabAnchor.None;

        OnStopGrabbing();
    }

    private void HandleGhost(GameObject ghost, Pose pose)
    {
        if (DM.ShowGhosts())
        {
            if (!ghost.activeSelf) ghost.SetActive(true);
            ghost.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
        else
        {
            if (ghost.activeSelf) ghost.SetActive(false);
        }
    }

    private Pose GetNextPose(Pose target, CDParams? cd)
    {
        var current = Calc.GetPose(GrabObject.transform);

        var next = Calc.CalculateNextPose(current: current, target: target, currentVelocity: grabObjectVelocity, cd);

        grabObjectVelocity = Calc.CalculateVelocity(from: current, to: next);

        grabObjectTravelDirection = Vector3.Normalize(next.position - current.position);

        //Real ghost displays real world pose (no CDParams applied), if different from target pose
        var realPose = GetTargetPose(null);
        HandleGhost(RealGhost, realPose);

        //Target ghost displays current target pose, if different from next pose
        HandleGhost(TargetGhost, target);

        return next;
    }

    protected abstract CDParams? GetCD();
    protected abstract Pose GetTargetPose(CDParams? cd);
    protected abstract void OnContinueGrabbing(Pose next);
    private void ContinueGrabbing()
    {
        var cd = GetCD();

        var target = GetTargetPose(cd);

        var next = GetNextPose(target, cd);

        OnContinueGrabbing(next);
    }

    private bool IsTouching(GrabAnchor anchor)
    {
        var grabBoundaryCollider = GrabBoundary.GetComponent<Collider>();

        var anchorPose = DM.GetGrabAnchorPose(anchor);
        return grabBoundaryCollider.bounds.Contains(anchorPose.position);
    }

    private bool IsGrabbing(GrabAnchor anchor)
    {
        var button = Calc.GetGrabButton(anchor);

        //If there is a grab button associated with given anchor, check if its pressed before starting grab interaction
        return button == OVRInput.Button.None || Calc.IsPressed(button);
    }

    private bool CheckEndGrabInteraction()
    {
        if (isTracking)
        {
            //Check tracked hand position against grab boundary

            var grabBoundaryCollider = GrabBoundary.GetComponent<Collider>();

            var anchorPose = DM.GetGrabAnchorPose(grabAnchor);

            var grabbingHandToTrackedObj = TrackedObject.transform.position - DM.GetGrabAnchorPose(grabAnchor).position;
            var noLongerTouching = grabbingHandToTrackedObj.magnitude > 0.5f; //m

            return noLongerTouching;
        }
        else
        {
            //Check button release

            var buttonReleased = !Calc.IsPressed(grabButton);

            return buttonReleased;
        }
    }

    protected void Update()
    {
        if (isGrabbing)
        {
            //Grab object active: Check for grab interaction end or continue interaction

            var ended = CheckEndGrabInteraction();
            if (ended)
            {
                StopGrabbing();
            }
            else
            {
                ContinueGrabbing();
            }
        }
        else
        {
            //Grab object inactive: Check for grab interaction start

            if (isTracking)
            {
                if(IsTouching(GrabAnchor.LeftHand))
                {
                    StartGrabbing(GrabAnchor.LeftHand, OVRInput.Button.None);
                }
                else if (IsTouching(GrabAnchor.RightHand))
                {
                    StartGrabbing(GrabAnchor.RightHand, OVRInput.Button.None);
                }
                else
                {
                    //In tracking mode, apply tracked position while inactive

                    var trackedPose = Calc.GetPose(TrackedObject.transform);
                    var cd = GetCD();
                    var next = GetNextPose(trackedPose, cd);

                    GrabObject.transform.SetPositionAndRotation(next.position, next.rotation);
                }
            }
            else
            {
                var leftTouching = IsTouching(GrabAnchor.LeftController);
                var rightTouching = IsTouching(GrabAnchor.RightController);
                if (leftTouching || rightTouching)
                {
                    if(!isHighlighted) Highlight();
                }
                else
                {
                    if (isHighlighted) Unhighlight();
                }

                if(leftTouching && IsGrabbing(GrabAnchor.LeftController))
                {
                    StartGrabbing(GrabAnchor.LeftController, Calc.GetGrabButton(GrabAnchor.LeftController));
                }else if(rightTouching && IsGrabbing(GrabAnchor.RightController))
                {
                    StartGrabbing(GrabAnchor.RightController, Calc.GetGrabButton(GrabAnchor.RightController));
                }
            }
        }
    }


}