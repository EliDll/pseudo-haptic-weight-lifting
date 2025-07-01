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

    public GameObject LeftDropZone;
    public GameObject RightDropZone;

    private GameObject? activeDropZone;

    public Material CompletionMaterial;

    private float shovelToBladeDist = 0;

    private float visualBetweenHandsDist = 0;

    private bool isLoaded = false;
    private bool stillInsidePile = false;

    private int loadCount = 0;

    private void LoadBlade()
    {
        loadCount++;

        isLoaded = true;

        DM.TryVibrate(grabAnchor);
        DM.TryVibrate(secondaryAnchor);

        //Slow down velocity to simulate impact with pile
        var percetage = 0.33f;
        grabObjectVelocity = new SpinTwistVelocity
        {
            linear = grabObjectVelocity.linear * percetage,
            spin = grabObjectVelocity.spin * percetage,
            twist = grabObjectVelocity.twist * percetage,
        };

        ShovelLoad.GetComponent<AudioSource>().Play();

        //Delay rendering for more plausible pile interaction
        Invoke("ShowStickyLoad", 0.25f);

        var delta = 0.05f;

        Pile.transform.localScale += new Vector3(delta, delta, delta);
        Pile.transform.position += new Vector3(0, -delta, 0);

        if (-Pile.transform.position.y >= delta * 20)
        {
            //Pile is fully shovelled, disable pile object
            Pile.SetActive(false);
        }
    }

    private void ShowStickyLoad()
    {
        ShovelLoad.GetComponent<Renderer>().enabled = true;
        ShovelLoad.GetComponent<Collider>().enabled = true;
    }

    private void HideStickyLoad()
    {
        ShovelLoad.GetComponent<Renderer>().enabled = false;
        ShovelLoad.GetComponent<Collider>().enabled = false;
    }

    private void UnloadBlade(SpinTwistVelocity bladeVelocity)
    {
        isLoaded = false;

        DM.TryVibrate(grabAnchor);
        DM.TryVibrate(secondaryAnchor);

        HideStickyLoad();

        //Replace sticky load with rigidbody loose load
        var newLooseLoad = GameObject.Instantiate(LooseLoad, ShovelLoad.transform.position, ShovelLoad.transform.rotation, parent: Parent.transform);
        newLooseLoad.SetActive(true);

        //Yeet new rigidbody based on current shovel orientation and blade velocity
        var newRigidBody = newLooseLoad.GetComponent<Rigidbody>();
        var velocityVec = GrabObject.transform.up * bladeVelocity.linear;
        newRigidBody.AddForce(velocityVec, ForceMode.VelocityChange);

        if (!Pile.activeSelf && activeDropZone != null)
        {
            //Pile is fully shovelled, mark drop zone as complete
            activeDropZone.GetComponent<Renderer>().material = CompletionMaterial;
            activeDropZone.GetComponent<AudioSource>().Play();
        }
    }

    protected override void OnStart()
    {
        LooseLoad.SetActive(false); //Inactive object from which to instantiate loose loads on unload

        HideStickyLoad();

        //Determine once at runtime to be geo-independent
        shovelToBladeDist = Vector3.Magnitude(ShovelBlade.transform.position - GrabObject.transform.position);
    }

    protected override void OnStartGrabbing()
    {
        LeftHandIdleVisual.SetActive(false);
        RightHandIdleVisual.SetActive(false);

        LeftHandGrabVisual.SetActive(true);
        RightHandGrabVisual.SetActive(true);

        LeftHandGrabVisual.transform.SetPositionAndRotation(LeftHandIdleVisual.transform.position, LeftHandIdleVisual.transform.rotation);
        RightHandGrabVisual.transform.SetPositionAndRotation(RightHandIdleVisual.transform.position, RightHandIdleVisual.transform.rotation);

        RightDropZone.SetActive(false);
        LeftDropZone.SetActive(false);

        if (primaryHand == PrimaryHand.Left)
        {
            LeftHandGrabVisual.GetComponent<AudioSource>().Play();
            activeDropZone = RightDropZone;
            activeDropZone.SetActive(true);
        }
        else if (primaryHand == PrimaryHand.Right)
        {
            RightHandGrabVisual.GetComponent<AudioSource>().Play();
            activeDropZone = LeftDropZone;
            activeDropZone.SetActive(true);
        }
    }

    protected override void OnStopGrabbing()
    {
        LeftHandIdleVisual.SetActive(true);
        RightHandIdleVisual.SetActive(true);

        if (primaryHand == PrimaryHand.Left)
        {
            LeftHandIdleVisual.GetComponent<AudioSource>().Play();
        }
        else if (primaryHand == PrimaryHand.Right)
        {
            RightHandIdleVisual.GetComponent<AudioSource>().Play();
        }

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

        var shiftedOrigin = new Pose(origin.position + headPosDiff, Quaternion.LookRotation(originForward, Vector3.up));
        //var shiftedOrigin = new Pose(origin.position + headPosDiff, origin.rotation);

        ///Target Pos
        var posDiff = current.position - shiftedOrigin.position;
        var scaledPosDiff = cd == null ? posDiff : Vector3.Scale(posDiff, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio));
        var targetPos = shiftedOrigin.position + scaledPosDiff;

        ///Target Forward (uses Lever Metaphor)
        var scaledBetweenHandsDist = cd == null ? betweenAnchorDist : betweenAnchorDist * cd.HorizontalRatio;

        var leverageRatio = scaledBetweenHandsDist / (GRIPPABLE_SHAFT_LEN * 0.66f); //"Full" leverage is achieved at fraction of total shaft length to promote exaggerated grip distance
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
        var diff = scaledBetweenHandsDist - visualBetweenHandsDist;
        var clampedDiff = Calc.Clamp(diff, min: -0.01f, max: 0.01f);
        visualBetweenHandsDist += clampedDiff;

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
            var combinedRoll = primaryRoll * 0.5f + secondaryRoll * 0.5f;

            var current = new Pose(primaryCurrent.position, Quaternion.LookRotation(controllersForward, primaryRoll));

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

        if (primaryHand == PrimaryHand.Left)
        {
            LeftHandGrabVisual.transform.SetPositionAndRotation(primaryPos, primaryCurrent.rotation);
            RightHandGrabVisual.transform.SetPositionAndRotation(secondaryPos, secondaryCurrent.rotation);
        }
        else if (primaryHand == PrimaryHand.Right)
        {
            RightHandGrabVisual.transform.SetPositionAndRotation(primaryPos, primaryCurrent.rotation);
            LeftHandGrabVisual.transform.SetPositionAndRotation(secondaryPos, secondaryCurrent.rotation);
        }

        //Update shovel load state
        if (isLoaded)
        {
            var bladeTiltedDown = ShovelBlade.transform.up.y < 0; //Blade normal pointing downward enables unload
            if (bladeTiltedDown && activeDropZone != null)
            {
                var insideDropZone = activeDropZone.GetComponent<Collider>().bounds.Contains(ShovelBlade.transform.position);
                if (insideDropZone)
                {
                    var bladeVelocity = Calc.CalculateVelocity(from: currentBlade, to: Calc.GetPose(ShovelBlade.transform));
                    UnloadBlade(bladeVelocity);
                }
            }
        }
        else
        {
            var bladeInsidePile = Pile.GetComponent<Collider>().bounds.Contains(ShovelBlade.transform.position); //Load when blade centre is inside pile

            if (stillInsidePile)
            {
                //Update and potentially reset flag (new angle of attack check only happens after exiting pile)
                stillInsidePile = bladeInsidePile;
            }
            else if (bladeInsidePile)
            {
                stillInsidePile = true;

                var bladeToPileCentre = new Vector3(x: Pile.transform.position.x, y: 0, z: Pile.transform.position.z) - ShovelBlade.transform.position;
                var bladeForward = ShovelBlade.transform.right * -1;

                var angleOfAttack = Vector3.Angle(bladeToPileCentre, bladeForward);

                if (angleOfAttack <= 45)
                {
                    LoadBlade();
                }
            }
        }
    }

    protected void FixedUpdate()
    {
        if (isGrabbing)
        {
            var leftVisible = LeftHandGrabVisual.transform.position;
            var rightVisible = RightHandGrabVisual.transform.position;
            var primaryVisible = primaryHand == PrimaryHand.Left ? leftVisible : rightVisible;
            var secondaryVisible = primaryHand == PrimaryHand.Left ? rightVisible : leftVisible;

            var log = new LogEntry
            {
                PrimaryMode = primaryHand,
                PrimaryTracked = DM.GetGrabAnchorPose(grabAnchor).position,
                SecondaryTracked = DM.GetGrabAnchorPose(secondaryAnchor).position,
                PrimaryVisible = primaryVisible,
                SecondaryVisible = secondaryVisible,
                HMD = Calc.GetHeadPose(CameraRig).position,
                EndEffectorVisible = ShovelBlade.transform.position,
                ShovelLoaded = isLoaded,
                CubeReachedTarget = 0, // n/a,
                GrabCount = grabCount,
                CollisionCount = loadCount //record load and unload events instead of collisions in this column
            };

            DM.Log(log);
        }
    }
}
