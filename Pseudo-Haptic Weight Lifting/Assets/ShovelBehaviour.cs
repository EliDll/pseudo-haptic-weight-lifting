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

    private LRTransform? origin;

    private Rigidbody _shovelRigidbody;

    private Renderer _shovelLoadRenderer;
    private Renderer _pileBoundaryRenderer;
    private Renderer _shovelBoundaryRenderer;

    private Collider _pileCollider;
    private Collider _pileBoundaryCollider;
    private Collider _shovelBoundaryCollider;

    private const float MIN_ANGLE_DEG = 0.1f;
    private float nextMaxAngle = MIN_ANGLE_DEG;

    private const float MIN_DISTANCE_M = 0.001f;
    private float nextMaxDistance = MIN_DISTANCE_M;

    private const float FULL_LEVERAGE_M = 0.5f;

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

    private void StartGrabbing(OVRInput.Controller primary, OVRInput.Button button)
    {
        nextMaxDistance = MIN_DISTANCE_M;
        nextMaxAngle = MIN_ANGLE_DEG;

        Unhighlight();

        PrimaryAnchor.SetActive(true);
        SecondaryAnchor.SetActive(true);
        TernaryAnchor.SetActive(true);

        _shovelRigidbody.isKinematic = true; //ignore physiccs for rigidbody whhile grabbing

        var secondary = primary == leftHand ? rightHand : leftHand;

        grabbingButton = button;

        primaryController = primary;
        secondaryController = secondary;

        origin = new LRTransform
        {
            pos = Shovel.transform.position,
            forward = Shovel.transform.rotation * Vector3.forward,
            up = Shovel.transform.rotation * Vector3.up
        };

        DM.Vibrate(primaryController);
    }

    private static LRTransform GetTwohandedControllerPose(OVRInput.Controller primary, OVRInput.Controller secondary)
    {
        var primaryPos = OVRInput.GetLocalControllerPosition(primary);
        var primaryRot = OVRInput.GetLocalControllerRotation(primary);
        var secondaryPos = OVRInput.GetLocalControllerPosition(secondary);
        var secondaryRot = OVRInput.GetLocalControllerRotation(secondary);

        return new LRTransform
        {
            pos = primaryPos,
            forward = Vector3.Normalize(secondaryPos - primaryPos),
            up = Vector3.Slerp(primaryRot * Vector3.up, secondaryRot * Vector3.up, 0.5f)
        };
    }

    private static LRTransform GetShovelTarget(LRTransform origin, LRTransform controllers, CDRatio cd, float controllerDist)
    {
        var scaledControllerDist = controllerDist * cd.Horizontal;
        var leverageFraction = scaledControllerDist / FULL_LEVERAGE_M;

        var leverCD = cd.Rotational == 1.0f ? cd.Rotational : leverageFraction * cd.Rotational; //Only scale when cd ratio is not none (1)

        return new LRTransform
        {
            pos = origin.pos + Vector3.Scale(controllers.pos - origin.pos, new Vector3(x: cd.Horizontal, y: cd.Vertical, z: cd.Horizontal)),
            forward = Vector3.Slerp(origin.forward, controllers.forward, leverCD),
            up = Vector3.Slerp(origin.up, controllers.up, cd.Rotational)
        };
    }

    private void MoveFrame(OVRInput.Controller primary, OVRInput.Controller secondary)
    {
        if(origin != null)
        {
            var controllers = GetTwohandedControllerPose(primary: primary, secondary: secondary);

            var cd = _shovelLoadRenderer.enabled ? DM.LoadedCDRatio : DM.NormalCDRatio;

            var primaryPos = OVRInput.GetLocalControllerPosition(primary);
            var secondaryPos = OVRInput.GetLocalControllerPosition(secondary);
            var controllerDist = Vector3.Magnitude(secondaryPos - primaryPos);

            var target = GetShovelTarget(origin: origin, controllers: controllers, cd: cd, controllerDist: controllerDist);
            var targetRot = Quaternion.LookRotation(target.forward, target.up);

            PrimaryAnchor.transform.position = target.pos + origin.forward * controllerDist;
            SecondaryAnchor.transform.position = target.pos + controllers.forward * controllerDist;
            TernaryAnchor.transform.position = target.pos + target.forward * controllerDist;

            var nextPos = Vector3.MoveTowards(Shovel.transform.position, target.pos, nextMaxDistance);
            nextMaxDistance += MIN_DISTANCE_M;

            var nextRot = Quaternion.RotateTowards(Shovel.transform.rotation, targetRot, nextMaxAngle);
            nextMaxAngle += MIN_ANGLE_DEG;

            Shovel.transform.SetPositionAndRotation(nextPos, nextRot);
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
        nextMaxDistance = MIN_DISTANCE_M;
        nextMaxAngle = MIN_ANGLE_DEG;

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
    }

    private void UnloadBlade()
    {
        nextMaxDistance = MIN_DISTANCE_M;
        nextMaxAngle = MIN_ANGLE_DEG;

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
                    MoveFrame(primary: primaryController.Value, secondary: secondaryController.Value);
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
