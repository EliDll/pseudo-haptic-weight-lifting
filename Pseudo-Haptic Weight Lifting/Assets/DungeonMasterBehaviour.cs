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

    public CDRatio NormalCDRatio = new CDRatio
    {
        Horizontal = 1.0f,
        Vertical = 1.0f,
        Rotational = 1.0f,
    };

    public CDRatio LoadedCDRatio = new CDRatio
    {
        Horizontal = 1.0f,
        Vertical = 1.0f,
        Rotational = 1.0f,
    };

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

    private void ChangeCDRatio()
    {
        currentIntensity = currentIntensity switch
        {
            CDIntensity.None => CDIntensity.Subtle,
            CDIntensity.Subtle => CDIntensity.Pronounced,
            CDIntensity.Pronounced => CDIntensity.None,
            _ => throw new System.NotImplementedException(),
        };

        NormalCDRatio = currentIntensity switch
        {
            CDIntensity.None => new CDRatio
            {
                Horizontal = 1.0f,
                Vertical = 1.0f,
                Rotational = 1.0f
            },
            CDIntensity.Subtle => new CDRatio
            {
                Horizontal = 0.9f,
                Vertical = 0.8f,
                Rotational = 0.9f
            },
            CDIntensity.Pronounced => new CDRatio
            {
                Horizontal = 0.8f,
                Vertical = 0.7f,
                Rotational = 0.8f
            },
            _ => throw new System.NotImplementedException()
        };

        LoadedCDRatio = currentIntensity switch
        {
            CDIntensity.None => new CDRatio
            {
                Horizontal = 1.0f,
                Vertical = 1.0f,
                Rotational = 1.0f
            },
            CDIntensity.Subtle => new CDRatio
            {
                Horizontal = 0.85f,
                Vertical = 0.75f,
                Rotational = 0.85f
            },
            CDIntensity.Pronounced => new CDRatio
            {
                Horizontal = 0.7f,
                Vertical = 0.6f,
                Rotational = 0.7f
            },
            _ => throw new System.NotImplementedException()
        };

        var vibrateDuration = currentIntensity switch
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
