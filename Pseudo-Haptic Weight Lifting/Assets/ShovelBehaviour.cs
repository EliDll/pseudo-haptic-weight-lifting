using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
#nullable enable


public class ShovelBehaviour : MonoBehaviour
{
    public DungeonMasterBehaviour DM;

    public GameObject Parent;
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

    public Collider PileMarginCollider;

    public Transform BladeTransform;

    private OVRInput.Button leftButton = OVRInput.Button.PrimaryHandTrigger;
    private OVRInput.Button rightButton = OVRInput.Button.SecondaryHandTrigger;

    private OVRInput.Controller leftHand = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightHand = OVRInput.Controller.RTouch;

    private bool isHighlighted = false;
    private bool isLoaded = false;

    private OVRInput.Button? grabbingButton = null;
    private OVRInput.Controller? grabbingController = null;
    private OVRInput.Controller? secondaryController = null;

    private Rigidbody _rigidbody;

    private Quaternion objInitRot;
    private Vector3 primaryInitPos;

    private record VirtualControllerTransform
    {
        public Quaternion primaryRot;
        public Quaternion secondaryRot;
        public Vector3 primaryPos;
        public Vector3 secondaryPos;
    }

    private VirtualControllerTransform? previous;

    private Vector3 ScaleByCD(Vector3 input, CDRatio cd)
    {
        return new Vector3(x: input.x * cd.Horizontal, y: input.y * cd.Vertical, z: input.z * cd.Horizontal);
    }

    private Quaternion ScaleByCD(Quaternion input, CDRatio cd)
    {
        return Quaternion.Slerp(Quaternion.identity, input, cd.Rotational);
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
        primaryInitPos = OVRInput.GetLocalControllerPosition(primary);
        objInitRot = obj.transform.rotation;

        _rigidbody.isKinematic = true; //ignore physiccs for rigidbody whhile grabbing
        HiltRenderer.material = DefaultHiltMaterial;

        grabbingButton = button;
        grabbingController = primary;
        secondaryController = primary == leftHand ? rightHand : leftHand;

        DM.Vibrate(grabbingController);
    }

    private void StopGrabbing()
    {
        DM.Vibrate(grabbingController);

        _rigidbody.isKinematic = false; //reactivate physics for rigidbody

        grabbingButton = null;
        grabbingController = null;
        secondaryController = null;
    }

    private void LoadBlade()
    {
        DM.Vibrate(grabbingController);
        DM.Vibrate(secondaryController);

        isLoaded = true;
        LoadRenderer.enabled = true;

        var loadScale = LoadTransform.localScale;
        var loadVolume = loadScale.x * loadScale.y * loadScale.z;

        var pileScale = PileTransform.localScale;

        var pileYDiff = loadVolume / (pileScale.x * pileScale.z);

        PileTransform.localScale = new Vector3(x: pileScale.x, y: Math.Max(pileScale.y - pileYDiff, 0), z: pileScale.z);
        PileTransform.position = new Vector3(x: PileTransform.position.x, y: PileTransform.position.y - (pileYDiff * 0.5f), z: PileTransform.position.z);
    }

    private void UnloadBlade()
    {
        DM.Vibrate(grabbingController);
        DM.Vibrate(secondaryController);

        isLoaded = false;
        LoadRenderer.enabled = false;

        //Replace sticky load with new rigidbody
        var looseLoad = GameObject.Instantiate(loadObject, LoadTransform.position, LoadTransform.rotation, Parent.transform);
        looseLoad.SetActive(true);
    }

    // Start is called before the first frame update
    void Start()
    {
        //Inactive object from which to instantiate loose loads on unload
        loadObject.SetActive(false);

        LoadRenderer.enabled = false;

        _rigidbody = obj.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (grabbingButton != null)
        {
            //Check for end of grab interaction

            var buttonReleased = !OVRInput.Get(grabbingButton.Value);

            if (buttonReleased)
            {
                StopGrabbing();
            }
            else
            {
                if(grabbingController != null && secondaryController != null)
                {
                    //Continue grab interaction and update transform

                    //var primaryCurrentPos = OVRInput.GetLocalControllerPosition(grabbingController.Value);
                    //var secondaryCurrentPos = OVRInput.GetLocalControllerPosition(secondaryController.Value);

                    ////var primaryControllerRotation = OVRInput.GetLocalControllerRotation(grabbingController.Value);
                    //var secondaryControllerRotation = OVRInput.GetLocalControllerRotation(secondaryController.Value);

                    //var controllerUp = secondaryControllerRotation * Vector3.up;
                    //var controllerDirection = secondaryCurrentPos - primaryCurrentPos;

                    //var betweenControllerRotation = Quaternion.LookRotation(controllerDirection, controllerUp);

                    //var CD = isLoaded ? DM.LoadedCDRatio : DM.NormalCDRatio;

                    ////Apply position diff during grab interaction with C/D
                    //var posDiff = primaryCurrentPos - primaryInitPos;
                    //var scaledPosDif = new Vector3(x: posDiff.x * CD.Horizontal, y: posDiff.y * CD.Vertical, z: posDiff.z * CD.Horizontal);

                    //obj.transform.position = primaryInitPos + scaledPosDif;

                    ////Apply rotation diff during grab interaction with C/D
                    //var rotDiff = betweenControllerRotation * Quaternion.Inverse(objInitRot);
                    //var scaledRotDiff = Quaternion.Slerp(Quaternion.identity, rotDiff, CD.Rotational);

                    //var shovelRotation = scaledRotDiff * objInitRot;
                    //obj.transform.rotation = shovelRotation;

                    var primaryCurrPos = OVRInput.GetLocalControllerPosition(grabbingController.Value);
                    var secondaryCurrPos = OVRInput.GetLocalControllerPosition(secondaryController.Value);

                    var primaryCurrRot = OVRInput.GetLocalControllerRotation(grabbingController.Value);
                    var secondaryCurrRot = OVRInput.GetLocalControllerRotation(secondaryController.Value);

                    if (previous != null)
                    {
                        var primaryPosDiff = primaryCurrPos - previous.primaryPos;
                        var secondaryPosDiff = secondaryCurrPos - previous.secondaryPos;

                        var primaryRotDiff = primaryCurrRot * Quaternion.Inverse(previous.primaryRot);
                        var secondaryRotDiff = secondaryCurrRot * Quaternion.Inverse(previous.secondaryRot);

                        var cd = isLoaded ? DM.LoadedCDRatio : DM.NormalCDRatio;

                        var current = new VirtualControllerTransform
                        {
                            primaryPos = previous.primaryPos + ScaleByCD(primaryPosDiff, cd),
                            secondaryPos = previous.secondaryPos + ScaleByCD(secondaryPosDiff, cd),
                            primaryRot = ScaleByCD(primaryRotDiff, cd) * previous.primaryRot,
                            secondaryRot = ScaleByCD(secondaryRotDiff, cd) * previous.secondaryRot,
                        };

                        var currentUp = current.secondaryRot * Vector3.up;
                        var currentDirection = current.secondaryPos - current.primaryPos;
                        var currentPose = Quaternion.LookRotation(currentDirection, currentUp);

                        obj.transform.SetPositionAndRotation(current.primaryPos, currentPose);

                        //Update virtual transform for next frame
                        previous = current;
                    }
                    else
                    {
                        //Initialize from actual controller transform
                        previous = new VirtualControllerTransform
                        {
                            primaryPos = primaryCurrPos,
                            secondaryPos = secondaryCurrPos,
                            primaryRot = primaryCurrRot,
                            secondaryRot = secondaryCurrRot,
                        };
                    }

                    //Update shovel load

                    if (isLoaded)
                    {
                        //Check for unload triggers

                        var shovelUp = obj.transform.rotation * Vector3.up;

                        //Cannot unload while inside pile margion
                        var canUnload = !PileMarginCollider.bounds.Contains(BladeTransform.position);

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
