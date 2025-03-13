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

    public GameObject Shovel;
    public GameObject ShovelLoad;
    public GameObject ShovelBlade;
    public GameObject ShovelBoundary;

    public GameObject LooseLoad;

    public GameObject Pile;
    public GameObject PileBoundary;

    public GameObject PrimaryAnchor;
    public GameObject SecondaryAnchor;
    public GameObject TernaryAnchor;

    public Material ActiveMaterial;

    private OVRInput.Button leftButton = OVRInput.Button.PrimaryHandTrigger;
    private OVRInput.Button rightButton = OVRInput.Button.SecondaryHandTrigger;

    private OVRInput.Controller leftHand = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightHand = OVRInput.Controller.RTouch;

    private OVRInput.Button? grabbingButton;
    private OVRInput.Controller? primaryController;
    private OVRInput.Controller? secondaryController;

    private VirtualTransform? shovelOrigin;
    private VirtualTransform? shovelTarget;

    private VirtualTransform? controllersCurrent;

    private VirtualTransform? primaryCurrent;
    private VirtualTransform? secondaryCurrent;

    private Rigidbody _shovelRigidbody;

    private Renderer _shovelLoadRenderer;
    private Renderer _pileBoundaryRenderer;
    private Renderer _shovelBoundaryRenderer;

    private Collider _pileCollider;
    private Collider _pileBoundaryCollider;
    private Collider _shovelBoundaryCollider;

    private float shovelMomentum = 0.0f;
    private const float SPEEDUP_PER_FRAME = 0.01f;

    // Start is called before the first frame update
    void Start()
    {
        LooseLoad.SetActive(false); //Inactive object from which to instantiate loose loads on unload

        PrimaryAnchor.SetActive(false);
        SecondaryAnchor.SetActive(false);
        TernaryAnchor.SetActive(false);

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

    private void Highlight()
    {
        if (!_shovelBoundaryRenderer.enabled) _shovelBoundaryRenderer.enabled = true;
    }

    private void Unhighlight()
    {
        if (_shovelBoundaryRenderer.enabled) _shovelBoundaryRenderer.enabled = false;
    }

    private static VirtualTransform GetTwohandedControllerPose(OVRInput.Controller primary, OVRInput.Controller secondary)
    {
        var primaryCurrent = Calc.GetControllerTransform(primary);
        var secondaryCurrent = Calc.GetControllerTransform(secondary);

        var controllersForward = Vector3.Normalize(secondaryCurrent.pos - primaryCurrent.pos);
        var controllersUp = Vector3.Slerp(primaryCurrent.rot * Vector3.up, secondaryCurrent.rot * Vector3.up, 0.5f);

        return new VirtualTransform
        {
            pos = primaryCurrent.pos,
            rot = Quaternion.LookRotation(controllersForward, controllersUp)
        };
    }

    private void StartGrabbing(OVRInput.Controller primary, OVRInput.Button button)
    {
        Unhighlight();

        PrimaryAnchor.SetActive(true);
        SecondaryAnchor.SetActive(true);
        TernaryAnchor.SetActive(true);

        _shovelRigidbody.isKinematic = true; //ignore physiccs for rigidbody whhile grabbing

        var secondary = primary == leftHand ? rightHand : leftHand;

        grabbingButton = button;

        primaryController = primary;
        secondaryController = secondary;

        controllersCurrent = GetTwohandedControllerPose(primary, secondary);
        shovelOrigin = controllersCurrent;
        shovelTarget = controllersCurrent;

        DM.Vibrate(primaryController);
    }

    //private static VirtualTransform GetShovelTargetFromAnchors(VirtualTransform primary, VirtualTransform secondary)
    //{
    //    var shovelPos = primary.pos; //shovel pos origin is hilt, i.e. equal to primary anchor
    //    var shovelUp = secondary.rot * Vector3.up; //Currently, shovel up is only determined by secondary anchor rotation
    //    var shovelDirection = secondary.pos - primary.pos; //Shovel direction is vector between anchors
    //    var shovelRotation = Quaternion.LookRotation(shovelDirection, shovelUp);

    //    return new VirtualTransform { pos = shovelPos, rot = shovelRotation };
    //}

    private void MoveIsomorphic(OVRInput.Controller primary, OVRInput.Controller secondary)
    {
        if (shovelTarget != null && controllersCurrent != null)
        {
            var controllersNext = GetTwohandedControllerPose(primary, secondary);

            var controllersPosDiff = controllersNext.pos - controllersCurrent.pos;
            var controllersRotDiff = controllersNext.rot * Quaternion.Inverse(controllersCurrent.rot);

            controllersCurrent = controllersNext;

            //var cd = _shovelLoadRenderer.enabled ? DM.LoadedCDRatio : DM.NormalCDRatio;

            //shovelTarget = new VirtualTransform
            //{
            //    pos = shovelTarget.pos + Calc.ScaleByCD(controllersPosDiff, cd),
            //    rot = Quaternion.Slerp(shovelTarget.rot, controllersRotDiff * shovelTarget.rot, cd.Rotational),
            //};
            UpdateShovelTargetFromOrigin();

            PrimaryAnchor.transform.position = shovelTarget.pos;
            SecondaryAnchor.transform.position = PrimaryAnchor.transform.position + shovelTarget.rot * Vector3.forward * 0.3f;
            TernaryAnchor.transform.position = SecondaryAnchor.transform.position + shovelTarget.rot * Vector3.up * 0.1f;

            if (shovelMomentum < 1.0f) shovelMomentum = Math.Min(shovelMomentum + SPEEDUP_PER_FRAME, 1.0f);

            var nextShovelPosition = Vector3.Lerp(Shovel.transform.position, shovelTarget.pos, shovelMomentum);
            var nextShovelRotation = Quaternion.Slerp(Shovel.transform.rotation, shovelTarget.rot, shovelMomentum);

            Shovel.transform.SetPositionAndRotation(nextShovelPosition, nextShovelRotation);
        }
    }

    //private void MoveDifferential(OVRInput.Controller primary, OVRInput.Controller secondary)
    //{
    //    var primaryControllerNext = Calc.GetControllerTransform(primary);
    //    var secondaryControllerNext = Calc.GetControllerTransform(secondary);

    //    if (primaryControllerCurrent != null && secondaryControllerCurrent != null && primaryAnchorCurrent != null && secondaryAnchorCurrent != null)
    //    {
    //        //Determine the controller diff in this frame, scaled by CD
    //        var cd = _shovelLoadRenderer.enabled ? DM.LoadedCDRatio : DM.NormalCDRatio;
    //        var primaryDiff = Calc.GetScaledDiff(from: primaryControllerCurrent, to: primaryControllerNext, cd);
    //        var secondaryDiff = Calc.GetScaledDiff(from: secondaryControllerCurrent, to: secondaryControllerNext, cd);

    //        //Add scaled diff to anchors
    //        var primaryAnchorNext = Calc.AddDiff(current: primaryAnchorCurrent, diff: primaryDiff);
    //        var secondaryAnchorNext = Calc.AddDiff(current: secondaryAnchorCurrent, diff: secondaryDiff);

    //        //Determine shovel transform from anchors
    //        var nextShovelPos = primaryAnchorNext.pos; //shovel pos origin is hilt, i.e. equal to primary anchor
    //        var nextShovelUp = secondaryAnchorNext.rot * Vector3.up; //Currently, shovel up is only determined by secondary anchor rotation
    //        var nextShovelDirection = secondaryAnchorNext.pos - primaryAnchorNext.pos; //Shovel direction is vector between anchors
    //        var nextShovelRotation = Quaternion.LookRotation(nextShovelDirection, nextShovelUp);

    //        //Apply new shovel transform to object
    //        Shovel.transform.SetPositionAndRotation(nextShovelPos, nextShovelRotation);

    //        //Apply new anchor transforms to objects
    //        PrimaryAnchor.transform.SetPositionAndRotation(primaryAnchorNext.pos, primaryAnchorNext.rot);
    //        SecondaryAnchor.transform.SetPositionAndRotation(secondaryAnchorNext.pos, secondaryAnchorNext.rot);

    //        //Update virtual transforms
    //        primaryControllerCurrent = primaryControllerNext;
    //        secondaryControllerCurrent = secondaryControllerNext;
    //        primaryAnchorCurrent = primaryAnchorNext;
    //        secondaryAnchorCurrent = secondaryAnchorNext;
    //    }
    //}

    private void UpdateShovelTargetFromOrigin()
    {
        if(shovelOrigin != null && controllersCurrent != null)
        {
            var posDiff = controllersCurrent.pos - shovelOrigin.pos;
            var rotDiff =  controllersCurrent.rot * Quaternion.Inverse(shovelOrigin.rot);

            var cd = _shovelLoadRenderer.enabled ? DM.LoadedCDRatio : DM.NormalCDRatio;

            shovelTarget = new VirtualTransform
            {
                pos = shovelOrigin.pos + Calc.ScaleByCD(posDiff, cd),
                rot = Quaternion.Slerp(shovelOrigin.rot, rotDiff * shovelOrigin.rot, cd.Rotational),
            };

        }
    }

    private void StopGrabbing()
    {
        PrimaryAnchor.SetActive(false);
        SecondaryAnchor.SetActive(false);
        TernaryAnchor.SetActive(false);

        DM.Vibrate(primaryController);

        _shovelRigidbody.isKinematic = false; //reactivate physics for rigidbody

        grabbingButton = null;
        primaryController = null;
        secondaryController = null;
    }

    private void LoadBlade()
    {
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

        shovelMomentum = 0.0f;
        UpdateShovelTargetFromOrigin();
    }

    private void UnloadBlade()
    {
        _shovelLoadRenderer.enabled = false;

        DM.Vibrate(primaryController);
        DM.Vibrate(secondaryController);

        //Replace sticky load with new rigidbody
        var newLooseLoad = GameObject.Instantiate(LooseLoad, ShovelLoad.transform.position, ShovelLoad.transform.rotation, parent: Parent.transform);
        newLooseLoad.SetActive(true);

        if (Pile.transform.localScale.y == 0)
        {
            //Pile is fully shovelled, display as done
            _pileBoundaryRenderer.material = ActiveMaterial;
            Pile.SetActive(false);
        }

        shovelMomentum = 0.0f;
        UpdateShovelTargetFromOrigin();
    }

    // Update is called once per frame
    void Update()
    {
        if (grabbingButton != null)
        {
            //Shovel active: Handle grab interaction

            var buttonReleased = !OVRInput.Get(grabbingButton.Value);
            if (buttonReleased)
            {
                StopGrabbing();
            }
            else
            {
                //Move shovel
                if (primaryController != null && secondaryController != null)
                {
                    //MoveDifferential(primary: primaryController.Value, secondary: secondaryController.Value);
                    MoveIsomorphic(primary: primaryController.Value, secondary: secondaryController.Value);
                }

                //Update shovel load state
                if (_shovelLoadRenderer.enabled)
                {
                    var canUnload = !_pileBoundaryCollider.bounds.Contains(ShovelBlade.transform.position);  //Cannot unload while inside pile boundary collider
                    if (canUnload)
                    {
                        var shovelUp = Shovel.transform.rotation * Vector3.up;
                        var shovelTiltedDown = Vector3.Angle(Vector3.up, shovelUp) >= 90; //Shovel pointing downward triggers unload
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
