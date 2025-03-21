using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public class CubeBehaviour : MonoBehaviour
{
    public DungeonMasterBehaviour DM;

    public GameObject obj;
    public Collider barrier;

    public Material defaultMat;
    public Material activeMat;
    public Material grabbingMaterial;

    private Collider _collider;
    private Rigidbody _rigidbody;
    private Renderer _renderer;

    private OVRInput.Button leftButton = OVRInput.Button.PrimaryHandTrigger;
    private OVRInput.Button rightButton = OVRInput.Button.SecondaryHandTrigger;

    private OVRInput.Controller leftHand = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightHand = OVRInput.Controller.RTouch;

    private bool isHighlighted = false;
    private bool hasCollided = false;
    private bool ignoreCollision = false;

    private OVRInput.Button? grabbingButton;
    private OVRInput.Controller? grabbingController;

    private Quaternion controllerInitRot;
    private Quaternion objInitRot;
    private Vector3 controllerInitPos;
    private Vector3 objInitPos;

    private void Highlight()
    {
        if (!isHighlighted)
        {
            isHighlighted = true;
            _renderer.material = activeMat;
        }
    }

    private void UnHighlight()
    {
        if (isHighlighted)
        {
            isHighlighted = false;
            _renderer.material = defaultMat;
        }
    }

    private void StartGrabbing(OVRInput.Controller controller, OVRInput.Button button)
    {
        _rigidbody.isKinematic = true; //ignore physiccs for rigidbody whhile grabbing
        _renderer.material = grabbingMaterial;

        //Ignore collision at the start of grab collision (this is necessary to grab objects that are already colliding at interaction start)
        ignoreCollision = true;

        grabbingButton = button;

        grabbingController = controller;
        controllerInitPos = OVRInput.GetLocalControllerPosition(controller);
        controllerInitRot = OVRInput.GetLocalControllerRotation(controller);

        objInitPos = obj.transform.position;
        objInitRot = obj.transform.rotation;

        DM.Vibrate(grabbingController);
    }

    private void StopGrabbing()
    {
        _rigidbody.isKinematic = false; //reactivate physics for rigidbody
        _renderer.material = defaultMat;

        DM.Vibrate(grabbingController);
    }

    // Start is called before the first frame update
    void Start()
    {
        _collider = obj.GetComponent<Collider>();
        _rigidbody = obj.GetComponent<Rigidbody>();
        _renderer = obj.GetComponent<Renderer>();
    }

    private void Update()
    {
        if (hasCollided && grabbingButton != null)
        {
            //Await button release before allowing to grab again
            var released = !OVRInput.Get(grabbingButton.Value);

            if (released)
            {
                hasCollided = false;
                grabbingButton = null;
            }
        }
        else
        {
            if (grabbingButton != null)
            {
                //Check for end of grab interaction

                var buttonReleased = !OVRInput.Get(grabbingButton.Value);
                var colliding = _collider.bounds.Intersects(barrier.bounds);

                if (buttonReleased)
                {
                    StopGrabbing();
                    grabbingButton = null;
                }
                else if (colliding && !ignoreCollision)
                {
                    StopGrabbing();
                    hasCollided = true;
                }
                else
                {
                    if(grabbingController != null)
                    {
                        //Continue grab interaction and update transform

                        if (!colliding && ignoreCollision) ignoreCollision = false; //set flag to stop grab interaction on next collision as soon as object is unstuck

                        var CD = DM.NormalCDRatio;

                        //Apply position diff during grab interaction with C/D
                        var controllerCurrentPos = OVRInput.GetLocalControllerPosition(grabbingController.Value);
                        var posDiff = controllerCurrentPos - controllerInitPos;
                        var scaledPosDif = CD == null ? posDiff : new Vector3(x: posDiff.x * CD.HorizontalRatio, y: posDiff.y * CD.VerticalRatio, z: posDiff.z * CD.HorizontalRatio);

                        obj.transform.position = objInitPos + scaledPosDif;

                        //Apply rotation diff during grab interaction with C/D
                        var controllerCurrentRot = OVRInput.GetLocalControllerRotation(grabbingController.Value);
                        var rotDiff = controllerCurrentRot * Quaternion.Inverse(controllerInitRot);
                        var scaledRotDiff = CD == null ? rotDiff : Quaternion.Slerp(Quaternion.identity, rotDiff, CD.RotationalRatio);

                        obj.transform.rotation = scaledRotDiff * objInitRot;
                    }
                }
            }
            else
            {
                //Check for highlighting and start of grab interaction

                var leftHandPos = OVRInput.GetLocalControllerPosition(leftHand);
                var rightHandPos = OVRInput.GetLocalControllerPosition(rightHand);

                var leftTouch = _collider.bounds.Contains(leftHandPos);
                var rightTouch = _collider.bounds.Contains(rightHandPos);

                if (leftTouch)
                {
                    Highlight();

                    var leftPress = OVRInput.Get(leftButton);

                    if (leftPress) StartGrabbing(leftHand, leftButton);
                }
                else if (rightTouch)
                {
                    Highlight();

                    var rightPress = OVRInput.Get(rightButton);

                    if (rightPress) StartGrabbing(rightHand, rightButton);
                }
                else
                {
                    UnHighlight();
                }
            }
        }
    }
}
