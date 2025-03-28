using UnityEditor;
using UnityEngine;
#nullable enable

public abstract class GrabBehaviour : MonoBehaviour
{
    public DungeonMasterBehaviour DM;
    public OVRCameraRig CameraRig;
    public GameObject Parent;
    public GameObject GrabObject;
    public GameObject GrabBoundary;
    public GameObject GrabbingHand;

    protected OVRInput.Button grabbingButton = OVRInput.Button.None;
    protected OVRInput.Controller grabbingController = OVRInput.Controller.None;

    protected Pose grabObjectOrigin;
    protected Pose headOrigin;

    protected Vector3 grabObjectTravelDirection = Vector3.zero;

    protected SpinTwistVelocity grabObjectVelocity = SpinTwistVelocity.Zero;

    private bool isGrabbing = false;
    private bool isHighlighted = false;

    protected abstract void OnStart();
    protected void Start()
    {
        OnStart();

        GrabBoundary.GetComponent<Renderer>().enabled = false;
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

    protected abstract void OnStartGrabbing(OVRInput.Controller controller);
    private void StartGrabbing(OVRInput.Controller controller, OVRInput.Button button)
    {
        isGrabbing = true;

        GrabbingHand.SetActive(true);

        grabObjectVelocity = SpinTwistVelocity.Zero;

        Unhighlight();

        GrabObject.GetComponent<Rigidbody>().isKinematic = true; //ignore physiccs for rigidbody whhile grabbing

        grabbingButton = button;
        grabbingController = controller;

        grabObjectOrigin = Calc.GetPose(GrabObject.transform);
        headOrigin = Calc.GetHeadPose(CameraRig);

        DM.Vibrate(controller);

        OnStartGrabbing(controller);
    }

    protected abstract void OnStopGrabbing();
    private void StopGrabbing()
    {
        isGrabbing = false;

        DM.Vibrate(grabbingController);

        GrabbingHand.SetActive(false);

        //Reactivate physics for grab object
        var grabObjectRigidbody = GrabObject.GetComponent<Rigidbody>();
        grabObjectRigidbody.isKinematic = false;

        //Yeet grab object based on current travel direction and velocity
        var velocityVec = grabObjectTravelDirection * grabObjectVelocity.linear * 0.5f;
        grabObjectRigidbody.AddForce(velocityVec, ForceMode.VelocityChange);

        grabbingButton = OVRInput.Button.None;
        grabbingController = OVRInput.Controller.None;

        OnStopGrabbing();
    }

    protected abstract CDParams? GetCD();
    protected abstract Pose GetTargetPose(CDParams? cd);
    protected abstract void OnContinueGrabbing(Pose next);
    private void ContinueGrabbing()
    {
        var cd = GetCD();

        var target = GetTargetPose(cd);

        var current = Calc.GetPose(GrabObject.transform);

        var next = Calc.GetNextPose(current: current, target: target, currentVelocity: grabObjectVelocity, cd);

        grabObjectVelocity = Calc.CalculateVelocity(from: current, to: next);

        grabObjectTravelDirection = Vector3.Normalize(next.position - current.position);

        OnContinueGrabbing(next);
    }

    protected void Update()
    {
        if (isGrabbing)
        {
            //Grab object active: Handle grab interaction

            var buttonReleased = !Calc.IsPressed(grabbingButton);
            if (buttonReleased)
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
            //Grab object inactive: Check for highlighting and start of grab interaction

            var grabBoundaryCollider = GrabBoundary.GetComponent<Collider>();

            var leftPose = Calc.GetControllerPose(Defs.LeftHand);
            var leftTouch = grabBoundaryCollider.bounds.Contains(leftPose.position);

            if (leftTouch)
            {
                if (!isHighlighted) Highlight();

                var leftPressed = Calc.IsPressed(Defs.LeftGrabButton);
                if (leftPressed) StartGrabbing(Defs.LeftHand, Defs.LeftGrabButton);
            }
            else
            {
                var rightPose = Calc.GetControllerPose(Defs.RightHand);
                var rightTouch = grabBoundaryCollider.bounds.Contains(rightPose.position);

                if (rightTouch)
                {
                    if (!isHighlighted) Highlight();

                    var rightPress = Calc.IsPressed(Defs.RightGrabButton);
                    if (rightPress) StartGrabbing(Defs.RightHand, Defs.RightGrabButton);
                }
                else
                {
                    if (isHighlighted) Unhighlight();
                }
            }
        }
    }


}