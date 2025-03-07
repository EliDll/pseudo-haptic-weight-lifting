using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ShovelBehaviour : MonoBehaviour
{
    public GameObject obj;
    public Renderer HiltRenderer;

    public Material defaultMat;
    public Material activeMat;

    private bool isHighlighted;
    private bool isGrabbed;

    private OVRInput.Button leftButton = OVRInput.Button.PrimaryHandTrigger;
    private OVRInput.Button rightButton = OVRInput.Button.SecondaryHandTrigger;
    private OVRInput.Button grabbingButton;

    private OVRInput.Controller leftHand = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightHand = OVRInput.Controller.RTouch;

    private OVRInput.Controller grabbingController;
    private OVRInput.Controller secondaryController;

    private Collider _collider;
    private Rigidbody _rigidbody;

    private void Highlight()
    {
        if (!isHighlighted)
        {
            isHighlighted = true;
            HiltRenderer.material = activeMat;
        }
    }

    private void UnHighlight()
    {
        if (isHighlighted)
        {
            isHighlighted = false;
            HiltRenderer.material = defaultMat;
        }
    }

    private void StopGrabbing()
    {
        isGrabbed = false;
        _rigidbody.isKinematic = false; //reactivate physics for rigidbody
    }

    private void StartGrabbing(OVRInput.Controller primary, OVRInput.Button button)
    {
        isGrabbed = true;
        _rigidbody.isKinematic = true; //ignore physiccs for rigidbody whhile grabbing
        HiltRenderer.material = defaultMat;

        grabbingButton = button;

        grabbingController = primary;
        secondaryController = primary == leftHand ? rightHand : leftHand;
    }

    // Start is called before the first frame update
    void Start()
    {
        isHighlighted = false;
        isGrabbed = false;
        _collider = obj.GetComponent<Collider>();
        _rigidbody = obj.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isGrabbed)
        {
            //Check for end of grab interaction

            var buttonReleased = !OVRInput.Get(grabbingButton);

            if (buttonReleased)
            {
                StopGrabbing();
            }
            else
            {
                //Continue grab interaction and update transform

                var primaryCurrentPos = OVRInput.GetLocalControllerPosition(grabbingController);
                var secondaryCurrentPos = OVRInput.GetLocalControllerPosition(secondaryController);

                obj.transform.position = primaryCurrentPos;

                var primaryControllerRotation = OVRInput.GetLocalControllerRotation(grabbingController);
                var secondaryControllerRotation = OVRInput.GetLocalControllerRotation(secondaryController);

                var controllerUp = secondaryControllerRotation * Vector3.up;

                var controllerDirection = secondaryCurrentPos - primaryCurrentPos;

                var pitchVec = new Vector3(x: 0, y: controllerDirection.y, z: controllerDirection.z);
                var pitch = Vector3.SignedAngle(Vector3.forward, pitchVec, Vector3.right);

                var yawVec = new Vector3(x: controllerDirection.x, y: 0, z: controllerDirection.z);
                var yaw = Vector3.SignedAngle(Vector3.forward, yawVec, Vector3.up);

                var rollVec = new Vector3(x: controllerUp.x, y: controllerUp.y, z: 0);
                var roll = Vector3.SignedAngle(Vector3.up, rollVec, Vector3.forward);

                obj.transform.rotation = Quaternion.LookRotation(controllerDirection, controllerUp);
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
