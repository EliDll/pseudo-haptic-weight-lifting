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

    public CDParams? NormalCD;
    public CDParams? LoadedCD;

    private GameObject? currentTask;

    private OVRInput.Button? pressedButton;

    private bool showControllers = false;

    private CDIntensity currentIntensity = CDIntensity.None;

    public void Vibrate(OVRInput.Controller? controller, float time = 0.1f)
    {
        if (controller == Defs.LeftHand)
        {
            startVib(controller.Value);
            Invoke("stopleftVib", time);
        }
        else if (controller == Defs.RightHand)
        {
            startVib(controller.Value);
            Invoke("stopRightVib", time);
        }
    }

    private void startVib(OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(1, 1, controller);
    }
    private void stopleftVib()
    {
        OVRInput.SetControllerVibration(0, 0, Defs.LeftHand);
    }

    private void stopRightVib()
    {
        OVRInput.SetControllerVibration(0, 0, Defs.RightHand);
    }

    public void ToggleControllerVisibility()
    {
        showControllers = !showControllers;

        if (showControllers)
        {
            LeftController.m_showState = OVRInput.InputDeviceShowState.Always;
            RightController.m_showState = OVRInput.InputDeviceShowState.Always;
        }
        else
        {
            LeftController.m_showState = OVRInput.InputDeviceShowState.ControllerNotInHand;
            RightController.m_showState = OVRInput.InputDeviceShowState.ControllerNotInHand;
        }
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

        Vibrate(Defs.LeftHand, time: vibrateDuration);
    }

    // Start is called before the first frame update
    void Start()
    {
        currentTask = null;
        pressedButton = null;
        BasicTask.SetActive(false);
        ShovellingTask.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (pressedButton != null)
        {
            //Only allow next press after pressed button is released
            if (!Calc.IsPressed(pressedButton.Value))
            {
                pressedButton = null;
            }
        }
        else if (Calc.IsPressed(Defs.ButtonA))
        {
            pressedButton = Defs.ButtonA;
            StartTask(BasicTask);
        }
        else if (Calc.IsPressed(Defs.ButtonB))
        {
            pressedButton = Defs.ButtonB;
            StartTask(ShovellingTask);
        }
        else if (Calc.IsPressed(Defs.ButtonX))
        {
            pressedButton = Defs.ButtonX;
            ChangeCDRatio();
        }
        else if (Calc.IsPressed(Defs.ButtonY))
        {
            pressedButton = Defs.ButtonY;
            ToggleControllerVisibility();
        }
    }
}
