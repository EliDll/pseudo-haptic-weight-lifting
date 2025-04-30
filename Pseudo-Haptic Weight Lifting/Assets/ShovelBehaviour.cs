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

    public Material CompletionMaterial;

    private float shovelToBladeDist = 0;

    private float visualBetweenHandsDist = 0;

    private bool isLoaded = false;

    private void LoadBlade()
    {
        isLoaded = true;

        DM.TryVibrate(grabAnchor);
        DM.TryVibrate(secondaryAnchor);

        grabObjectVelocity = SpinTwistVelocity.Zero; //Reset velocity to simulate impact with pile

        ShovelLoad.GetComponent<Renderer>().enabled = true;
        ShovelLoad.GetComponent<AudioSource>().Play();

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

        DM.TryVibrate(grabAnchor);
        DM.TryVibrate(secondaryAnchor);

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
        LooseLoad.SetActive(false); //Inactive object from which to instantiate loose loads on unload

        ShovelLoad.GetComponent<Renderer>().enabled = false;

        shovelToBladeDist = Vector3.Magnitude(ShovelBlade.transform.position - GrabObject.transform.position);
    }

    protected override void OnStartGrabbing()
    {
        LeftHandIdleVisual.SetActive(false);
        RightHandIdleVisual.SetActive(false);

        LeftHandGrabVisual.SetActive(true);
        RightHandGrabVisual.SetActive(true);
    }

    protected override void OnStopGrabbing()
    {
        LeftHandIdleVisual.SetActive(true);
        RightHandIdleVisual.SetActive(true);

        LeftHandGrabVisual.SetActive(false);
        RightHandGrabVisual.SetActive(false);
    }

    protected override CDParams? GetCD()
    {
        return isLoaded ? DM.LoadedCD : DM.NormalCD;
    }

    private Pose GetScaledDiff(Pose origin, Pose current, CDParams? cd, float betweenAnchorDist)
    {
        var headCurrent = Calc.GetHeadPose(CameraRig);
        var headPosDiff = headCurrent.position - headOrigin.position;

        //Rotate horiz forward vec up to 45 degrees downwards for "natural resting" shovel position
        var originForwardHorizontal = Vector3.Normalize(new Vector3(x: origin.forward.x, y: 0, z: origin.forward.z));
        var originRightHorizontal = Vector3.Normalize(new Vector3(x: origin.forward.z, y: 0, z: -origin.forward.x));

        var heightRatio = Math.Min(current.position.y / GRIPPABLE_SHAFT_LEN, 1.0f); //Magic number!
        var degreesToRotate = heightRatio * 45;

        var originForward = Quaternion.AngleAxis(degreesToRotate, originRightHorizontal) * originForwardHorizontal;

        var shiftedOrigin = new Pose(origin.position + headPosDiff, Quaternion.LookRotation(originForward, origin.up));
        //var shiftedOrigin = new Pose(origin.position + headPosDiff, origin.rotation);

        ///Target Pos
        var posDiff = current.position - shiftedOrigin.position;
        var scaledPosDiff = cd == null ? posDiff : Vector3.Scale(posDiff, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio));
        var targetPos = shiftedOrigin.position + scaledPosDiff;

        ///Target Forward (uses Lever Metaphor)
        var scaledBetweenHandsDist = cd == null ? betweenAnchorDist : betweenAnchorDist * cd.HorizontalRatio;

        var leverageRatio = scaledBetweenHandsDist / (GRIPPABLE_SHAFT_LEN * 0.75f); //"Full" leverage is achieved at fraction of total shaft length to promote exaggerated grip distance
        var forwardCD = Math.Min(leverageRatio * (cd?.RotationalRatio ?? 1), 1.0f); //Clamp max CD ratio to one
        var targetForward = Vector3.Slerp(shiftedOrigin.forward, current.forward, forwardCD);

        ///Target Up
        var targetUp = cd == null ? current.up : Vector3.Slerp(shiftedOrigin.up, current.up, cd.RotationalRatio);

        //Add clipping offset so that shovel blade does not clip
        var targetBladePos = targetPos + targetForward * shovelToBladeDist;
        var clippingHeight = targetBladePos.y < 0 ? -targetBladePos.y : 0.0f;
        var clippingOffset = new Vector3(x: 0, y: clippingHeight, z: 0);

        //Add clipping height to visible hands distance to make second hand position when clipping less jarring (Note that this is not accurate when shovel is not pointing straight down)
        //visualBetweenHandsDist = scaledBetweenHandsDist + clippingHeight;
        visualBetweenHandsDist = scaledBetweenHandsDist;

        //return new Pose(targetPos + clippingOffset, Quaternion.LookRotation(targetForward, targetUp));

        return new Pose(targetPos, Quaternion.LookRotation(targetForward, targetUp));
    }

    protected override Pose GetTargetPose(CDParams? cd)
    {
        var primaryCurrent = DM.GetGrabAnchorPose(grabAnchor);
        var secondaryCurrent = DM.GetGrabAnchorPose(secondaryAnchor);

        var betweenAnchorDist = Vector3.Magnitude(primaryCurrent.position - secondaryCurrent.position);

        if (isTracking)
        {
            //Determine target pose based on tracked object

            var origin = grabObjectOrigin;
            var current = Calc.GetPose(TrackedObject.transform);

            var target = GetScaledDiff(origin: origin, current: current, cd, betweenAnchorDist);
            return target;
        }
        else
        {
            //Determine target pose based on controllers

            var origin = grabObjectOrigin;

            var controllersForward = Vector3.Normalize(secondaryCurrent.position - primaryCurrent.position);

            //Use right vector for roll to mitigate tilt influence
            var rightToUp = Quaternion.AngleAxis(90, controllersForward);
            var primaryRoll = rightToUp * primaryCurrent.right;
            var secondaryRoll = rightToUp * secondaryCurrent.right;
            var controllersRoll = primaryRoll * 0.5f + secondaryRoll * 0.5f;

            var current = new Pose(primaryCurrent.position, Quaternion.LookRotation(controllersForward, controllersRoll));

            var target = GetScaledDiff(origin: origin, current: current, cd, betweenAnchorDist);
            return target;
        }
    }

    protected override void OnContinueGrabbing(Pose next)
    {
        //Save blade position before applying next pose
        var currentBlade = Calc.GetPose(ShovelBlade.transform);

        GrabObject.transform.SetPositionAndRotation(next.position, next.rotation);

        var primaryPos = GrabObject.transform.position; //Place grabbing hand exactly on hilt
        var secondaryPos = GrabObject.transform.position + GrabObject.transform.forward * Math.Min(visualBetweenHandsDist, GRIPPABLE_SHAFT_LEN); //Clamp secondary hand position to shaft

        var primaryCurrent = DM.GetGrabAnchorPose(grabAnchor);
        var secondaryCurrent = DM.GetGrabAnchorPose(secondaryAnchor);

        if(primary == Primary.Left)
        {
            LeftHandGrabVisual.transform.SetPositionAndRotation(primaryPos, primaryCurrent.rotation);
            RightHandGrabVisual.transform.SetPositionAndRotation(secondaryPos, secondaryCurrent.rotation);
        }
        else if (primary == Primary.Right){
            RightHandGrabVisual.transform.SetPositionAndRotation(primaryPos, primaryCurrent.rotation);
            LeftHandGrabVisual.transform.SetPositionAndRotation(secondaryPos, secondaryCurrent.rotation);
        }

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
