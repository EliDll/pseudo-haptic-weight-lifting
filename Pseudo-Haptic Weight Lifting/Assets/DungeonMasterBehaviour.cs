using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public enum CDIntensity{
    None,
    Subtle,
    Pronounced
}

public class DungeonMasterBehaviour : MonoBehaviour
{
    public GameObject BasicTask;
    public GameObject ShovellingTask;
    public GameObject LeftHandModel;
    public GameObject RightHandModel;

    public CDParams? NormalCDRatio;

    public CDParams? LoadedCDRatio;

    private OVRInput.Button buttonA = OVRInput.Button.One;
    private OVRInput.Button buttonB = OVRInput.Button.Two;
    private OVRInput.Button buttonX = OVRInput.Button.Three;
    private OVRInput.Button buttonY = OVRInput.Button.Four;

    private OVRInput.Controller leftHand = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightHand = OVRInput.Controller.RTouch;

    private GameObject? currentTask;

    private OVRInput.Button? pressedButton;

    private bool handsVisible = false;

    private CDIntensity currentIntensity = CDIntensity.None;

    public void Vibrate(OVRInput.Controller? controller, float time = 0.1f)
    {
        if (controller == leftHand)
        {
            startVib(leftHand);
            Invoke("stopleftVib", time);
        }
        else if (controller == rightHand)
        {
            startVib(rightHand);
            Invoke("stopRightVib", time);
        }
    }

    public void ToggleHandVisibility()
    {
        handsVisible = !handsVisible;

        LeftHandModel.SetActive(handsVisible);
        RightHandModel.SetActive(handsVisible);
    }

    private void startVib(OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(1, 1, controller);
    }
    private void stopleftVib()
    {
        OVRInput.SetControllerVibration(0, 0, leftHand);
    }

    private void stopRightVib()
    {
        OVRInput.SetControllerVibration(0, 0, rightHand);
    }

    private void StartTask(GameObject task)
    {
        if (currentTask != null) GameObject.Destroy(currentTask);
        currentTask = GameObject.Instantiate(task);
        currentTask.SetActive(true);
    }

    private void SwitchCDRatio(CDIntensity intensity)
    {
        currentIntensity = intensity;

        NormalCDRatio = intensity switch
        {
            CDIntensity.None => null,
            CDIntensity.Subtle => new CDParams
            {
                HorizontalRatio = 0.9f,
                VerticalRatio = 0.8f,
                RotationalRatio = 0.9f,
                Acceleration = 9f,
                SpinAcceleration = 360f,
                TwistAcceleration = 360f,
            },
            CDIntensity.Pronounced => new CDParams
            {
                HorizontalRatio = 0.8f,
                VerticalRatio = 0.7f,
                RotationalRatio = 0.8f,
                Acceleration = 6f,
                SpinAcceleration = 315f,
                TwistAcceleration = 315f,
            },
            _ => throw new System.NotImplementedException()
        };

        LoadedCDRatio = intensity switch
        {
            CDIntensity.None => null,
            CDIntensity.Subtle => new CDParams
            {
                HorizontalRatio = 0.85f,
                VerticalRatio = 0.75f,
                RotationalRatio = 0.85f,
                Acceleration = 7f,
                SpinAcceleration = 270f,
                TwistAcceleration = 270f,
            },
            CDIntensity.Pronounced => new CDParams
            {
                HorizontalRatio = 0.7f,
                VerticalRatio = 0.6f,
                RotationalRatio = 0.7f,
                Acceleration = 4f,
                SpinAcceleration = 225f,
                TwistAcceleration = 225f,
            },
            _ => throw new System.NotImplementedException()
        };
    }

    private void ChangeCDRatio()
    {
        var newIntensity = currentIntensity switch
        {
            CDIntensity.None => CDIntensity.Subtle,
            CDIntensity.Subtle => CDIntensity.Pronounced,
            CDIntensity.Pronounced => CDIntensity.None,
            _ => throw new System.NotImplementedException(),
        };

        SwitchCDRatio(newIntensity);

        var vibrateDuration = newIntensity switch
        {
            CDIntensity.None => 0.1f,
            CDIntensity.Subtle => 0.5f,
            CDIntensity.Pronounced => 1.0f,
            _ => throw new System.NotImplementedException(),
        };

        Vibrate(leftHand, time: vibrateDuration);
    }

    // Start is called before the first frame update
    void Start()
    {
        SwitchCDRatio(CDIntensity.None);

        currentTask = null;
        pressedButton = null;
        BasicTask.SetActive(false);
        ShovellingTask.SetActive(false);
        LeftHandModel.SetActive(false);
        RightHandModel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (pressedButton != null)
        {
            //Only allow next press after pressed button is released
            if (!OVRInput.Get(pressedButton.Value))
            {
                pressedButton = null;
            }
        }
        else if (OVRInput.Get(buttonA))
        {
            pressedButton = buttonA;
            StartTask(BasicTask);
        }
        else if (OVRInput.Get(buttonB))
        {
            pressedButton = buttonB;
            StartTask(ShovellingTask);
        }
        else if (OVRInput.Get(buttonX))
        {
            pressedButton = buttonX;
            ChangeCDRatio();
        }
        else if (OVRInput.Get(buttonY))
        {
            pressedButton = buttonY;
            ToggleHandVisibility();
        }
    }
}
