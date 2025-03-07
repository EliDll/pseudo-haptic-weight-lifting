using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ShovelBehaviour : MonoBehaviour
{
    public GameObject obj;
    public GameObject loadObject;

    public Collider HiltCollider;
    public Renderer HiltRenderer;
    public Material DefaultHiltMaterial;
    public Material ActiveHiltMaterial;

    public Renderer LoadRenderer;
    public Transform LoadTransform;

    public Collider PileCollider;
    public Transform PileTransform;

    public Transform BladeTransform;

    private bool isHighlighted;
    private bool isGrabbed;
    private bool isLoaded;

    private OVRInput.Button leftButton = OVRInput.Button.PrimaryHandTrigger;
    private OVRInput.Button rightButton = OVRInput.Button.SecondaryHandTrigger;
    private OVRInput.Button grabbingButton;

    private OVRInput.Controller leftHand = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightHand = OVRInput.Controller.RTouch;

    private OVRInput.Controller grabbingController;
    private OVRInput.Controller secondaryController;

    private Rigidbody _rigidbody;

    private void VibrateMain()
    {
        startMainVib();
        Invoke("stopMainVib", .1f);
    }
    private void startMainVib()
    {
        OVRInput.SetControllerVibration(1, 1, grabbingController);
    }
    private void stopMainVib()
    {
        OVRInput.SetControllerVibration(0, 0, grabbingController);
    }

    private void VibrateSecondary()
    {
        startSecondaryVib();
        Invoke("stopSecondaryVib", .1f);
    }
    private void startSecondaryVib()
    {
        OVRInput.SetControllerVibration(1, 1, secondaryController);
    }
    private void stopSecondaryVib()
    {
        OVRInput.SetControllerVibration(0, 0, secondaryController);
    }

    private void Highlight()
    {
        if (!isHighlighted)
        {
            isHighlighted = true;
            HiltRenderer.material = ActiveHiltMaterial;
        }
    }

    private void UnHighlight()
    {
        if (isHighlighted)
        {
            isHighlighted = false;
            HiltRenderer.material = DefaultHiltMaterial;
        }
    }

    private void StartGrabbing(OVRInput.Controller primary, OVRInput.Button button)
    {
        isGrabbed = true;
        _rigidbody.isKinematic = true; //ignore physiccs for rigidbody whhile grabbing
        HiltRenderer.material = DefaultHiltMaterial;
        grabbingButton = button;

        grabbingController = primary;
        secondaryController = primary == leftHand ? rightHand : leftHand;

        VibrateMain();
    }

    private void StopGrabbing()
    {
        isGrabbed = false;
        _rigidbody.isKinematic = false; //reactivate physics for rigidbody

        VibrateMain();
    }

    private void LoadBlade()
    {
        isLoaded = true;
        LoadRenderer.enabled = true;

        var loadScale = LoadTransform.localScale;
        var loadVolume = loadScale.x * loadScale.y * loadScale.z;

        var pileScale = PileTransform.localScale;

        var pileYDiff = loadVolume / (pileScale.x * pileScale.z);

        PileTransform.localScale = new Vector3(x: pileScale.x, y: Math.Max(pileScale.y - pileYDiff, 0), z: pileScale.z);

        VibrateMain();
        VibrateSecondary();
    }

    private void UnloadBlade()
    {
        isLoaded = false;
        LoadRenderer.enabled = false;

        //Replace sticky load with new rigidbody
        var looseLoad = GameObject.Instantiate(loadObject, LoadTransform.position, LoadTransform.rotation);
        looseLoad.SetActive(true);

        VibrateMain();
        VibrateSecondary();
    }

    // Start is called before the first frame update
    void Start()
    {
        isHighlighted = false;
        isGrabbed = false;
        isLoaded = false;
        LoadRenderer.enabled = false;

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

                var shovelRotation = Quaternion.LookRotation(controllerDirection, controllerUp);

                obj.transform.rotation = shovelRotation;

                //Update shovel load

                if (isLoaded)
                {
                    //Check for unload triggers

                    var shovelUp = shovelRotation * Vector3.up;

                    //Cannot unload while inside pile
                    var canUnload = !PileCollider.bounds.Contains(BladeTransform.position);

                    if (canUnload)
                    {
                        //Shovel pointing downward
                        if (Vector3.Angle(Vector3.up, shovelUp) >= 90)
                        {
                            UnloadBlade();
                        }
                    }
                }
                else
                {
                    //Check for load material collision

                    var shouldLoad = PileCollider.bounds.Contains(BladeTransform.position);

                    if (shouldLoad)
                    {
                        LoadBlade();
                    }
                }
            }
        }
        else
        {
            //Check for highlighting and start of grab interaction

            var leftHandPos = OVRInput.GetLocalControllerPosition(leftHand);
            var rightHandPos = OVRInput.GetLocalControllerPosition(rightHand);

            var leftTouch = HiltCollider.bounds.Contains(leftHandPos);
            var rightTouch = HiltCollider.bounds.Contains(rightHandPos);

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
