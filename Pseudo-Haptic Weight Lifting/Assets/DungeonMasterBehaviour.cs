using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public class DungeonMasterBehaviour : MonoBehaviour
{
    public GameObject BasicTask;
    public GameObject ShovellingTask;
    public OVRControllerHelper LeftController;
    public OVRControllerHelper RightController;

    public GameObject TrackedRightHand;
    public GameObject TrackedLeftHand;
    public GameObject TrackedShovelGeometry;
    public GameObject TrackedCube;

    public CDParams? NormalCD;
    public CDParams? LoadedCD;

    private GameObject? currentTask;

    private OVRInput.Button currentPressedButton = OVRInput.Button.None;
    private CDIntensity currentIntensity = CDIntensity.None;

    private bool showRealWorldObjects = false;
    private bool trackingEnabled = false;

    public bool IsTrackingEnabled()
    {
        return trackingEnabled;
    }

    public Pose GetGrabAnchorPose(GrabAnchor anchor)
    {
        return anchor switch
        {
            GrabAnchor.LeftController => Calc.GetControllerPose(Defs.LeftController),
            GrabAnchor.RightController => Calc.GetControllerPose(Defs.RightController),
            GrabAnchor.LeftHand => Calc.GetPose(TrackedLeftHand.transform),
            GrabAnchor.RightHand => Calc.GetPose(TrackedRightHand.transform),
            _ => new Pose()
        };
    }

    public void TryVibrate(GrabAnchor anchor, float time = 0.1f)
    {
        if (anchor == GrabAnchor.LeftController)
        {
            StartVib(Defs.LeftController);
            Invoke("StopleftVib", time);
        }
        else if (anchor == GrabAnchor.RightController)
        {
            StartVib(Defs.RightController);
            Invoke("StopRightVib", time);
        }
    }

    private void StartVib(OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(1, 1, controller);
    }
    private void StopleftVib()
    {
        OVRInput.SetControllerVibration(0, 0, Defs.LeftController);
    }

    private void StopRightVib()
    {
        OVRInput.SetControllerVibration(0, 0, Defs.RightController);
    }

    public void ToggleTracking(bool enabled)
    {
        trackingEnabled = enabled;
    }

    public void ToggleRealWorldObjects(bool show)
    {
        showRealWorldObjects = show;

        if (showRealWorldObjects)
        {
            LeftController.m_showState = OVRInput.InputDeviceShowState.Always;
            RightController.m_showState = OVRInput.InputDeviceShowState.Always;

        }
        else
        {
            LeftController.m_showState = OVRInput.InputDeviceShowState.ControllerNotInHand;
            RightController.m_showState = OVRInput.InputDeviceShowState.ControllerNotInHand;
        }

        TrackedCube.GetComponent<Renderer>().enabled = showRealWorldObjects;
        TrackedLeftHand.GetComponent<Renderer>().enabled = showRealWorldObjects;
        TrackedRightHand.GetComponent<Renderer>().enabled = showRealWorldObjects;

        TrackedShovelGeometry.SetActive(showRealWorldObjects);
    }

    private void StartTask(GameObject task)
    {
        if (currentTask != null) GameObject.Destroy(currentTask);
        currentTask = GameObject.Instantiate(task);
        currentTask.SetActive(true);
    }

    private void ChangeCDRatio()
    {
        currentIntensity = currentIntensity switch
        {
            CDIntensity.None => CDIntensity.Subtle,
            CDIntensity.Subtle => CDIntensity.Pronounced,
            CDIntensity.Pronounced => CDIntensity.None,
            _ => throw new System.NotImplementedException(),
        };

        NormalCD = currentIntensity switch
        {
            CDIntensity.None => null,
            CDIntensity.Subtle => CDParams.Subtle,
            CDIntensity.Pronounced => CDParams.Pronounced,
            _ => throw new System.NotImplementedException()
        };

        LoadedCD = currentIntensity switch
        {
            CDIntensity.None => null,
            CDIntensity.Subtle => CDParams.Subtle_Loaded,
            CDIntensity.Pronounced => CDParams.Pronounced_Loaded,
            _ => throw new System.NotImplementedException()
        };

        var vibrateDuration = currentIntensity switch
        {
            CDIntensity.None => 0.1f,
            CDIntensity.Subtle => 0.5f,
            CDIntensity.Pronounced => 1.0f,
            _ => throw new System.NotImplementedException(),
        };

        TryVibrate(GrabAnchor.LeftController, time: vibrateDuration);
    }

    private bool CheckPress(OVRInput.Button button)
    {
        var isPressed = Calc.IsPressed(button);
        if (isPressed) currentPressedButton = button;
        return isPressed;
    }

    // Start is called before the first frame update
    void Start()
    {
        currentTask = null;
        BasicTask.SetActive(false);
        ShovellingTask.SetActive(false);

        ToggleRealWorldObjects(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentPressedButton != OVRInput.Button.None)
        {
            //Only register next press after pressed button is released
            if (!Calc.IsPressed(currentPressedButton))
            {
                currentPressedButton = OVRInput.Button.None;
            }
        }
        else if (CheckPress(Defs.ButtonA)) StartTask(BasicTask);

        else if (CheckPress(Defs.ButtonB)) StartTask(ShovellingTask);

        else if (CheckPress(Defs.ButtonX)) ChangeCDRatio();

        else if (CheckPress(Defs.ButtonY)) ToggleRealWorldObjects(!showRealWorldObjects);

        else if (CheckPress(Defs.LeftThumbstickPress)) ToggleTracking(!trackingEnabled);
    }
}
