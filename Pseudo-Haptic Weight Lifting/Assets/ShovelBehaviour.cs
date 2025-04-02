using System;
using UnityEngine;
#nullable enable

public class ShovelBehaviour : GrabBehaviour
{
    private const float GRIPPABLE_SHAFT_LEN = 0.6f;

    public GameObject ShovelBlade;
    public GameObject ShovelLoad;
    public GameObject LooseLoad;
    public GameObject Pile;
    public GameObject PileBoundary;
    public GameObject SecondaryHand;

    public Material CompletionMaterial;

    private OVRInput.Controller secondaryController = OVRInput.Controller.None;

    private float shovelToBladeDist = 0;

    private float betweenHandsDist = 0;

    private bool isLoaded = false;

    private void LoadBlade()
    {
        isLoaded = true;

        DM.Vibrate(grabbingController);
        DM.Vibrate(secondaryController);

        grabObjectVelocity = SpinTwistVelocity.Zero; //Reset velocity to simulate impact with pile

        ShovelLoad.GetComponent<Renderer>().enabled = true;

        var loadScale = ShovelLoad.transform.localScale;
        var currentPileScale = Pile.transform.localScale;
        var currentPilePos = Pile.transform.position;

        var loadVolume = loadScale.x * loadScale.y * loadScale.z;

        var pileHeightDiff = loadVolume / (currentPileScale.x * currentPileScale.z);
        var nextPileHeight = currentPileScale.y - pileHeightDiff;

        var nextPileScale = new Vector3(x: currentPileScale.x, y: Math.Max(nextPileHeight, 0), z: currentPileScale.z);
        var nextPilePos = new Vector3(x: currentPilePos.x, y: Math.Max(nextPileHeight * 0.5f, 0), z: currentPilePos.z);

        Pile.transform.localScale = nextPileScale;
        Pile.transform.position = nextPilePos;

        if (Pile.transform.localScale.y < 0.05f)
        {
            //Pile is fully shovelled, disable pile object
            Pile.SetActive(false);
        }
    }

    private void UnloadBlade(SpinTwistVelocity bladeVelocity)
    {
        isLoaded = false;

        DM.Vibrate(grabbingController);
        DM.Vibrate(secondaryController);

        ShovelLoad.GetComponent<Renderer>().enabled = false;

        //Replace sticky load with rigidbody loose load
        var newLooseLoad = GameObject.Instantiate(LooseLoad, ShovelLoad.transform.position, ShovelLoad.transform.rotation, parent: Parent.transform);
        newLooseLoad.SetActive(true);

        //Yeet new rigidbody based on current shovel orientation and blade velocity
        var newRigidBody = newLooseLoad.GetComponent<Rigidbody>();
        var velocityVec = GrabObject.transform.up * bladeVelocity.linear;
        newRigidBody.AddForce(velocityVec, ForceMode.VelocityChange);

        if (!Pile.activeSelf)
        {
            //Pile is fully shovelled, mark as completed through pile boundary object
            PileBoundary.GetComponent<Renderer>().material = CompletionMaterial;
        }
    }

    protected override void OnStart()
    {
        SecondaryHand.SetActive(false);

        LooseLoad.SetActive(false); //Inactive object from which to instantiate loose loads on unload

        ShovelLoad.GetComponent<Renderer>().enabled = false;

        shovelToBladeDist = Vector3.Magnitude(ShovelBlade.transform.position - GrabObject.transform.position);
    }

    protected override void OnStartGrabbing(OVRInput.Controller primary)
    {
        SecondaryHand.SetActive(true);

        var secondary = primary == Defs.LeftHand ? Defs.RightHand : Defs.LeftHand;
        secondaryController = secondary;
    }

    protected override void OnStopGrabbing()
    {
        SecondaryHand.SetActive(false);

        secondaryController = OVRInput.Controller.None;
    }

    protected override CDParams? GetCD()
    {
        return isLoaded ? DM.LoadedCD : DM.NormalCD;
    }

    protected override Pose GetTargetPose(CDParams? cd)
    {
        var headCurrent = Calc.GetHeadPose(CameraRig);
        var headPosDiff = headCurrent.position - headOrigin.position;

        //Rotate horiz forward vec 45 degrees downwards for "natural resting" shovel position
        var originForwardHorizontal = Vector3.Normalize(new Vector3(x: grabObjectOrigin.forward.x, y: 0, z: grabObjectOrigin.forward.z));
        var originRightHorizontal = Vector3.Normalize(new Vector3(x: grabObjectOrigin.forward.z, y: 0, z: -grabObjectOrigin.forward.x));
        var originForward = Quaternion.AngleAxis(45, originRightHorizontal) * originForwardHorizontal;

        var origin = new Pose(grabObjectOrigin.position + headPosDiff, Quaternion.LookRotation(originForward, grabObjectOrigin.up));

        var primaryCurrent = Calc.GetControllerPose(grabbingController);
        var secondaryCurrent = Calc.GetControllerPose(secondaryController);

        var controllersForward = Vector3.Normalize(secondaryCurrent.position - primaryCurrent.position);

        //Use right vector for roll to mitigate tilt influence
        var rightToUp = Quaternion.AngleAxis(90, controllersForward);
        var primaryRoll = rightToUp * primaryCurrent.right;
        var secondaryRoll = rightToUp * secondaryCurrent.right;
        var controllersRoll = primaryRoll * 0.5f + secondaryRoll * 0.5f;

        var current = new Pose(primaryCurrent.position, Quaternion.LookRotation(controllersForward, controllersRoll));

        ///Target Pos
        var posDiff = current.position - origin.position;
        var scaledPosDiff = cd == null ? posDiff : Vector3.Scale(posDiff, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio));
        var targetPos = origin.position + scaledPosDiff;

        ///Target Forward
        var betweenControllersDist = Vector3.Magnitude(secondaryCurrent.position - primaryCurrent.position);
        var scaledBetweenControllersDist = cd == null ? betweenControllersDist : betweenControllersDist * cd.HorizontalRatio;

        var leverageRatio = scaledBetweenControllersDist / (GRIPPABLE_SHAFT_LEN * 0.75f); //"Full" leverage is achieved at fraction of total shaft length to promote exaggerated grip distance
        var forwardCD = Math.Min(leverageRatio * (cd?.RotationalRatio ?? 1), 1.0f); //Clamp max CD ratio to one
        var targetForward = Vector3.Slerp(origin.forward, current.forward, forwardCD);

        ///Target Up
        var targetUp = cd == null ? current.up : Vector3.Slerp(origin.up, current.up, cd.RotationalRatio);

        //Add clipping offset so that shovel blade does not clip
        var targetBladePos = targetPos + targetForward * shovelToBladeDist;
        var clippingHeight = targetBladePos.y < 0 ? -targetBladePos.y : 0.0f;
        var clippingOffset = new Vector3(x: 0, y: clippingHeight, z: 0);

        //Add clipping height to visible hands distance to make second hand position when clipping less jarring (Note that this is not accurate when shovel is not pointing straight down)
        betweenHandsDist = scaledBetweenControllersDist + clippingHeight;

        return new Pose(targetPos + clippingOffset, Quaternion.LookRotation(targetForward, targetUp));
    }

    protected override void OnContinueGrabbing(Pose next)
    {
        //Save blade position before applying next pose
        var currentBlade = Calc.GetPose(ShovelBlade.transform);

        GrabObject.transform.SetPositionAndRotation(next.position, next.rotation);

        GrabbingHand.transform.position = GrabObject.transform.position; //Place grabbing hand exactly on hilt
        SecondaryHand.transform.position = GrabObject.transform.position + GrabObject.transform.forward * Math.Min(betweenHandsDist, GRIPPABLE_SHAFT_LEN); //Clamp secondary hand position to shaft

        //Update shovel load state
        if (isLoaded)
        {
            var pileBoundaryCollider = PileBoundary.GetComponent<Collider>();
            var canUnload = !pileBoundaryCollider.bounds.Contains(ShovelBlade.transform.position); //Can only unload when blade centre is outside of pile boundary collider
            if (canUnload)
            {
                var shovelTiltedDown = GrabObject.transform.up.y < 0; //Shovel normal pointing downward triggers unload
                if (shovelTiltedDown)
                {
                    var bladeVelocity = Calc.CalculateVelocity(from: currentBlade, to: Calc.GetPose(ShovelBlade.transform));
                    UnloadBlade(bladeVelocity);
                }
            }
        }
        else
        {
            var pileCollider = Pile.GetComponent<Collider>();
            var bladeInsidePile = pileCollider.bounds.Contains(ShovelBlade.transform.position); //Load when blade centre is inside pile
            if (bladeInsidePile) LoadBlade();
        }
    }
}
