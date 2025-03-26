using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public record CubeVelocity
{
    /// <summary>
    /// m/s
    /// </summary>
    public float linear;
    /// <summary>
    /// deg/s
    /// </summary>
    public float spin;
    /// <summary>
    /// deg/s
    /// </summary>
    public float twist;
}

public class CubeBehaviour : MonoBehaviour
{
    public DungeonMasterBehaviour DM;

    public OVRCameraRig CameraRig;

    public GameObject Cube;
    public GameObject CubeBoundary;

    public GameObject Barrier;

    public GameObject Hand;

    private OVRInput.Button? grabbingButton;
    private OVRInput.Controller? grabbingController;

    private Pose cubeOrigin;
    private Pose headOrigin;
    private Pose controllerOrigin;

    private Rigidbody _cubeRigidbody;

    private Renderer _cubeBoundaryRenderer;

    private Collider _cubeCollider;
    private Collider _barrierCollider;

    private bool hasCollided = false;
    private bool ignoreCollision = false;

    private CubeVelocity currentVelocity = new CubeVelocity
    {
        linear = 0,
        twist = 0,
    };

    // Start is called before the first frame update
    void Start()
    {
        _cubeRigidbody = Cube.GetComponent<Rigidbody>();

        _cubeBoundaryRenderer = CubeBoundary.GetComponent<Renderer>();

        _cubeCollider = Cube.GetComponent<Collider>();
        _barrierCollider = Barrier.GetComponent<Collider>();

        _cubeBoundaryRenderer.enabled = false;
    }

    private void ResetVelocity()
    {
        currentVelocity = new CubeVelocity { linear = 0, twist = 0 };
    }

    private void ResetIgnoreCollision()
    {
        ignoreCollision = false;
    }

    private void Highlight()
    {
        if (!_cubeBoundaryRenderer.enabled) _cubeBoundaryRenderer.enabled = true;
    }

    private void Unhighlight()
    {
        if (_cubeBoundaryRenderer.enabled) _cubeBoundaryRenderer.enabled = false;
    }

    private void StartGrabbing(OVRInput.Controller primary, OVRInput.Button button)
    {
        ResetVelocity();
        Unhighlight();

        Hand.SetActive(true);

        _cubeRigidbody.isKinematic = true; //ignore physiccs for rigidbody whhile grabbing

        if (IsColliding())
        {
            //If already colliding at grab start, ignore collisions for first second of interaction
            ignoreCollision = true;
            Invoke("ResetIgnoreCollision", 1f);
        }

        grabbingButton = button;
        grabbingController = primary;

        cubeOrigin = new Pose(Cube.transform.position, Cube.transform.rotation);
        headOrigin = Calc.GetHeadPose(CameraRig);
        controllerOrigin = Calc.GetControllerPose(primary);

        //Align hand with controller
        Hand.transform.SetPositionAndRotation(controllerOrigin.position, controllerOrigin.rotation);

        DM.Vibrate(grabbingController);
    }

    private void ContinueGrabbing()
    {
        if (grabbingController != null)
        {
            var controllerCurrent = Calc.GetControllerPose(grabbingController.Value);
            var headCurrent = Calc.GetHeadPose(CameraRig);

            var headPosDiff = headCurrent.position - headOrigin.position;
            var originPos = controllerOrigin.position + headPosDiff;

            var cd = DM.NormalCD;

            var controllerPosDiff = controllerCurrent.position - originPos;
            var targetPosDiff = cd == null ? controllerPosDiff : Vector3.Scale(controllerPosDiff, new Vector3(x: cd.HorizontalRatio, y: cd.VerticalRatio, z: cd.HorizontalRatio));
            var targetPos = originPos + targetPosDiff;

            var targetForward = cd == null ? controllerCurrent.forward : Vector3.Slerp(controllerOrigin.forward, controllerCurrent.forward, cd.RotationalRatio);
            var targetUp = cd == null ? controllerCurrent.up : Vector3.Slerp(controllerOrigin.up, controllerCurrent.up, cd.RotationalRatio);

            var currentPos = Hand.transform.position;
            var currentForward = Hand.transform.forward;
            var currentUp = Hand.transform.up;

            var maxVelocity = cd != null ? new CubeVelocity
            {
                linear = currentVelocity.linear + cd.Acceleration * Time.deltaTime, // m/s
                twist = currentVelocity.twist + cd.TwistAcceleration * Time.deltaTime, // deg/s
                spin = currentVelocity.spin + cd.SpinAcceleration * Time.deltaTime, // deg/s
            } : null;

            var nextPos = maxVelocity == null ? targetPos : Vector3.MoveTowards(currentPos, targetPos, maxDistanceDelta: maxVelocity.linear * Time.deltaTime);
            var nextForward = maxVelocity == null ? targetForward : Vector3.RotateTowards(currentForward, targetForward, maxRadiansDelta: Mathf.Deg2Rad * maxVelocity.spin * Time.deltaTime, maxMagnitudeDelta: 0.0f);
            var nextUp = maxVelocity == null ? targetUp : Vector3.RotateTowards(currentUp, targetUp, maxRadiansDelta: Mathf.Deg2Rad * maxVelocity.twist * Time.deltaTime, maxMagnitudeDelta: 0.0f);

            var nextRot = Quaternion.LookRotation(nextForward, nextUp);
            Hand.transform.SetPositionAndRotation(nextPos, nextRot);

            var controllerRotDiff = nextRot * Quaternion.Inverse(controllerOrigin.rotation);

            var controllerToCube = cubeOrigin.position - controllerOrigin.position;
            Cube.transform.SetPositionAndRotation(nextPos + controllerRotDiff * controllerToCube, controllerRotDiff * cubeOrigin.rotation);

            currentVelocity = new CubeVelocity
            {
                linear = Vector3.Magnitude(nextPos - currentPos) / Time.deltaTime,
                spin = Vector3.Angle(nextForward, currentForward) / Time.deltaTime,  // deg/s
                twist = Vector3.Angle(nextUp, currentUp) / Time.deltaTime, // deg/s
            };
        }
    }

    private void StopGrabbing()
    {
        DM.Vibrate(grabbingController);

        Hand.SetActive(false);

        _cubeRigidbody.isKinematic = false; //reactivate physics for rigidbody

        grabbingController = null;
    }

    private bool IsColliding()
    {
        return _cubeCollider.bounds.Intersects(_barrierCollider.bounds);
    }


    private void Update()
    {
        if (grabbingButton != null)
        {
            //Cube active: Handle grab interaction

            var buttonReleased = !Calc.IsPressed(grabbingButton.Value);
            if (buttonReleased)
            {
                if (hasCollided)
                {
                    //Reenable grabbing after collision on button release
                    grabbingButton = null;
                    hasCollided = false;
                }
                else
                {
                    //Grab interaction terminated through voluntary button release
                    grabbingButton = null;
                    StopGrabbing();
                }
            }
            else
            {
                var isColliding = !ignoreCollision && IsColliding();
                if (isColliding)
                {
                    //Grab interaction terminated through collision
                    hasCollided = true;
                    StopGrabbing();
                }
                else
                {
                    ContinueGrabbing();
                }
            }
        }
        else
        {
            //Cube inactive: Check for highlighting and start of grab interaction

            var leftHandTransform = Calc.GetControllerPose(Defs.LeftHand);
            var leftTouch = _cubeCollider.bounds.Contains(leftHandTransform.position);

            if (leftTouch)
            {
                Highlight();

                var leftPressed = Calc.IsPressed(Defs.LeftGrabButton);
                if (leftPressed) StartGrabbing(Defs.LeftHand, Defs.LeftGrabButton);
            }
            else
            {
                var rightHandTransform = Calc.GetControllerPose(Defs.RightHand);
                var rightTouch = _cubeCollider.bounds.Contains(rightHandTransform.position);

                if (rightTouch)
                {
                    Highlight();

                    var rightPress = Calc.IsPressed(Defs.RightGrabButton);
                    if (rightPress) StartGrabbing(Defs.RightHand, Defs.RightGrabButton);
                }
                else
                {
                    Unhighlight();
                }
            }
        }
    }
}
