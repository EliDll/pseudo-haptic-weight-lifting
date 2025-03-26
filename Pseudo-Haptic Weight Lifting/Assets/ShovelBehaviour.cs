using System;
using UnityEngine;
#nullable enable

public record ShovelVelocity
{
    /// <summary>
    /// m/s
    /// </summary>
    public float hilt;
    /// <summary>
    /// m/s
    /// </summary>
    public float blade;
    /// <summary>
    /// deg/s
    /// </summary>
    public float spin;
    /// <summary>
    /// deg/s
    /// </summary>
    public float twist;
}

public class ShovelBehaviour : MonoBehaviour
{
    public DungeonMasterBehaviour DM;

    public OVRCameraRig CameraRig;

    public GameObject Parent;

    public GameObject Shovel;
    public GameObject ShovelLoad;
    public GameObject ShovelBlade;
    public GameObject ShovelBoundary;

    public GameObject LooseLoad;

    public GameObject Pile;
    public GameObject PileBoundary;

    public GameObject PrimaryHand;
    public GameObject SecondaryHand;

    public GameObject DebugGreen;
    public GameObject DebugYellow;
    public GameObject DebugRed;
    public GameObject DebugOrange;

    public Material ActiveMaterial;

    private OVRInput.Button leftButton = OVRInput.Button.PrimaryHandTrigger;
    private OVRInput.Button rightButton = OVRInput.Button.SecondaryHandTrigger;

    private OVRInput.Controller leftHand = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightHand = OVRInput.Controller.RTouch;

    private OVRInput.Button? grabbingButton;
    private OVRInput.Controller? primaryController;
    private OVRInput.Controller? secondaryController;

    private Transform? shovelOrigin;
    private Transform? headOrigin;

    private Rigidbody _shovelRigidbody;

    private Renderer _shovelLoadRenderer;
    private Renderer _pileBoundaryRenderer;
    private Renderer _shovelBoundaryRenderer;

    private Collider _pileCollider;
    private Collider _pileBoundaryCollider;
    private Collider _shovelBoundaryCollider;

    private const float SHAFT_LEN = 0.6f;

    private ShovelVelocity currentVelocity = new ShovelVelocity
    {
        hilt = 0,
        blade = 0,
        spin = 0,
        twist = 0
    };

    // Start is called before the first frame update
    void Start()
    {
        PrimaryHand.SetActive(false);
        SecondaryHand.SetActive(false);

        LooseLoad.SetActive(false); //Inactive object from which to instantiate loose loads on unload

        DebugGreen.SetActive(false);
        DebugYellow.SetActive(false);
        DebugRed.SetActive(false);
        DebugOrange.SetActive(false);

        _shovelRigidbody = Shovel.GetComponent<Rigidbody>();

        _shovelLoadRenderer = ShovelLoad.GetComponent<Renderer>();
        _pileBoundaryRenderer = PileBoundary.GetComponent<Renderer>();
        _shovelBoundaryRenderer = ShovelBoundary.GetComponent<Renderer>();

        _pileCollider = Pile.GetComponent<Collider>();
        _pileBoundaryCollider = PileBoundary.GetComponent<Collider>();
        _shovelBoundaryCollider = ShovelBoundary.GetComponent<Collider>();

        _shovelLoadRenderer.enabled = false;
        _shovelBoundaryRenderer.enabled = false;
    }

    private void ResetVelocity()
    {
        currentVelocity = new ShovelVelocity
        {
            hilt = 0,
            blade = 0,
            spin = 0,
            twist = 0
        };
    }

    private void Highlight()
    {
        if (!_shovelBoundaryRenderer.enabled) _shovelBoundaryRenderer.enabled = true;
    }

    private void Unhighlight()
    {
        if (_shovelBoundaryRenderer.enabled) _shovelBoundaryRenderer.enabled = false;
    }

    private void StartGrabbing(OVRInput.Controller primary, OVRInput.Button button)
    {
        ResetVelocity();
        Unhighlight();

        PrimaryHand.SetActive(true);
        SecondaryHand.SetActive(true);

        DebugGreen.SetActive(true);
        DebugYellow.SetActive(true);
        DebugRed.SetActive(true);
        DebugOrange.SetActive(true);

        _shovelRigidbody.isKinematic = true; //ignore physiccs for rigidbody whhile grabbing

        var secondary = primary == leftHand ? rightHand : leftHand;

        grabbingButton = button;
        primaryController = primary;
        secondaryController = secondary;

        shovelOrigin = Shovel.transform;
        headOrigin = CameraRig.centerEyeAnchor.transform;

        DM.Vibrate(primaryController);
    }

    private void StopGrabbing()
    {
        DM.Vibrate(primaryController);

        PrimaryHand.SetActive(false);
        SecondaryHand.SetActive(false);

        DebugGreen.SetActive(false);
        DebugYellow.SetActive(false);
        DebugRed.SetActive(false);
        DebugOrange.SetActive(false);

        _shovelRigidbody.isKinematic = false; //reactivate physics for rigidbody

        primaryController = null;
        secondaryController = null;
    }

    private void ContinueGrabbing()
    {
        if (shovelOrigin != null && headOrigin != null && primaryController != null && secondaryController != null)
        {
            var primaryControllerPos = OVRInput.GetLocalControllerPosition(primaryController.Value);
            var primaryControllerRot = OVRInput.GetLocalControllerRotation(primaryController.Value);

            var secondaryControllerPos = OVRInput.GetLocalControllerPosition(secondaryController.Value);
            var secondaryControllerRot = OVRInput.GetLocalControllerRotation(secondaryController.Value);

            var controllerForward = Vector3.Normalize(secondaryControllerPos - primaryControllerPos);
            var controllerUp = Vector3.Slerp(primaryControllerRot * Vector3.up, secondaryControllerRot * Vector3.up, 0.5f);

            var cameraPosDiff = CameraRig.centerEyeAnchor.transform.position - headOrigin.position;
            var originPos = shovelOrigin.position + cameraPosDiff;

            var cd = _shovelLoadRenderer.enabled ? DM.LoadedCD : DM.NormalCD;

            var controllerPosDiff = primaryControllerPos - originPos;
            var targetPosDiff = cd == null ? controllerPosDiff : Vector3.Scale(controllerPosDiff, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio));
            var targetHiltPos = originPos + targetPosDiff;

            //Rotate horiz forward vec 45 degrees downwards for "natural resting" shovel position
            var originForwardHorizontal = Vector3.Normalize(new Vector3(x: shovelOrigin.forward.x, y: 0, z: shovelOrigin.forward.z));
            var originRightHorizontal = Vector3.Normalize(new Vector3(x: shovelOrigin.forward.z, y: 0, z: -shovelOrigin.forward.x));
            var originForward = Quaternion.AngleAxis(45, originRightHorizontal) * originForwardHorizontal;

            var betweenControllersDist = Vector3.Magnitude(secondaryControllerPos - primaryControllerPos);
            var betweenHandsDist = cd == null ? betweenControllersDist : betweenControllersDist * cd.HorizontalRatio;

            var targetUp = cd == null ? controllerUp : Vector3.Slerp(shovelOrigin.up, controllerUp, cd.RotationalRatio);

            var leverageRatio = betweenHandsDist / (SHAFT_LEN * 0.5f); //"Full" leverage is achieved at fraction of total shaft length to promote exaggerated grip distance
            var forwardCD = Math.Min(leverageRatio * cd?.RotationalRatio ?? 1, 1.0f); //Only scale up to full C/D
            var targetForward = Vector3.Slerp(originForward, controllerForward, forwardCD);

            var distToBlade = Vector3.Magnitude(ShovelBlade.transform.position - Shovel.transform.position);
            var targetBladePos = targetHiltPos + targetForward * distToBlade;

            var clippingOffset = targetBladePos.y < 0 ? -targetBladePos.y : 0.0f;
            targetHiltPos += new Vector3(x: 0, y: clippingOffset, z: 0);

            DebugRed.transform.position = originPos;

            DebugOrange.transform.position = targetHiltPos + originForward * betweenControllersDist;
            DebugYellow.transform.position = targetHiltPos + controllerForward * betweenControllersDist;
            DebugGreen.transform.position = targetHiltPos + targetForward * betweenControllersDist;

            var currentBladePos = ShovelBlade.transform.position;
            var currentHiltPos = Shovel.transform.position;
            var currentForward = Shovel.transform.forward;
            var currentUp = Shovel.transform.up;

            var maxVelocity = cd != null ? new ShovelVelocity
            {
                blade = currentVelocity.blade + cd.Acceleration * Time.deltaTime, // m/s
                hilt = currentVelocity.hilt + cd.Acceleration * Time.deltaTime, // m/s
                spin = currentVelocity.spin + cd.SpinAcceleration * Time.deltaTime, // deg/s
                twist = currentVelocity.twist + cd.TwistAcceleration * Time.deltaTime, // deg/s
            } : null;

            var nextHiltPos = maxVelocity == null ? targetHiltPos : Vector3.MoveTowards(currentHiltPos, targetHiltPos, maxDistanceDelta: maxVelocity.hilt * Time.deltaTime);
            var nextForward = maxVelocity == null ? targetForward : Vector3.RotateTowards(currentForward, targetForward, maxRadiansDelta: Mathf.Deg2Rad * maxVelocity.spin * Time.deltaTime, maxMagnitudeDelta: 0.0f);
            var nextUp = maxVelocity == null ? targetUp : Vector3.RotateTowards(currentUp, targetUp, maxRadiansDelta: Mathf.Deg2Rad * maxVelocity.twist * Time.deltaTime, maxMagnitudeDelta: 0.0f);

            Shovel.transform.SetPositionAndRotation(nextHiltPos, Quaternion.LookRotation(nextForward, nextUp));

            PrimaryHand.transform.position = Shovel.transform.position;
            SecondaryHand.transform.position = Shovel.transform.position + Shovel.transform.forward * Math.Min(clippingOffset + betweenHandsDist, SHAFT_LEN); //Clamp secondary hand position to shaft

            currentVelocity = new ShovelVelocity
            {
                blade = Vector3.Magnitude(ShovelBlade.transform.position - currentBladePos) / Time.deltaTime, // m/s
                hilt = Vector3.Magnitude(nextHiltPos - currentHiltPos) / Time.deltaTime, // m/s
                spin = Vector3.Angle(nextForward, currentForward) / Time.deltaTime,  // deg/s
                twist = Vector3.Angle(nextUp, currentUp) / Time.deltaTime, // deg/s
            };

            //Update shovel load state
            if (_shovelLoadRenderer.enabled)
            {
                var canUnload = !_pileBoundaryCollider.bounds.Contains(ShovelBlade.transform.position);  //Cannot unload while inside pile boundary collider
                if (canUnload)
                {
                    var shovelTiltedDown = Shovel.transform.up.y < 0; //Shovel pointing downward triggers unload

                    if (shovelTiltedDown) UnloadBlade();
                }
            }
            else
            {
                var canLoad = _pileCollider.bounds.Contains(ShovelBlade.transform.position); //Blade is loaded when entering pile collider
                if (canLoad) LoadBlade();
            }
        }

    }

    private void LoadBlade()
    {
        ResetVelocity();

        _shovelLoadRenderer.enabled = true;

        DM.Vibrate(primaryController);
        DM.Vibrate(secondaryController);

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

    private void UnloadBlade()
    {

        _shovelLoadRenderer.enabled = false;

        DM.Vibrate(primaryController);
        DM.Vibrate(secondaryController);

        //Replace sticky load with new rigidbody
        var newLooseLoad = GameObject.Instantiate(LooseLoad, ShovelLoad.transform.position, ShovelLoad.transform.rotation, parent: Parent.transform);
        newLooseLoad.SetActive(true);

        //Yeet new rigidbody based on current shovel momentum
        var newRigidBody = newLooseLoad.GetComponent<Rigidbody>();
        newRigidBody.AddForce(Shovel.transform.up * currentVelocity.blade, ForceMode.VelocityChange);

        if (!Pile.activeSelf)
        {
            //Pile is fully shovelled, mark as done through boundary
            _pileBoundaryRenderer.material = ActiveMaterial;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (grabbingButton != null)
        {
            //Shovel active: Handle grab interaction

            var buttonReleased = !Calc.IsPressed(grabbingButton.Value);
            if (buttonReleased)
            {
                grabbingButton = null;
                StopGrabbing();
            }
            else
            {
                ContinueGrabbing();
            }
        }
        else
        {
            //Shovel inactive: Check for highlighting and start of grab interaction

            var leftHandPos = OVRInput.GetLocalControllerPosition(leftHand);
            var leftTouch = _shovelBoundaryCollider.bounds.Contains(leftHandPos);

            if (leftTouch)
            {
                Highlight();

                var leftPress = OVRInput.Get(leftButton);
                if (leftPress) StartGrabbing(leftHand, leftButton);
            }
            else
            {
                var rightHandPos = OVRInput.GetLocalControllerPosition(rightHand);
                var rightTouch = _shovelBoundaryCollider.bounds.Contains(rightHandPos);

                if (rightTouch)
                {
                    Highlight();

                    var rightPress = OVRInput.Get(rightButton);
                    if (rightPress) StartGrabbing(rightHand, rightButton);
                }
                else
                {
                    Unhighlight();
                }
            }
        }
    }
}
