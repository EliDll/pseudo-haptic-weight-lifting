using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public record CDRatio
{
    public float Horizontal;
    public float Vertical;
    public float Rotational;
}

public record VirtualTransform
{
    public Vector3 pos;
    public Quaternion rot;
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

    private int currentCDIntensity = 0;

    public VirtualTransform AddDiff(VirtualTransform current, VirtualTransform diff)
    {
        return new VirtualTransform
        {
            pos = current.pos + diff.pos,
            rot = diff.rot * current.rot,
        };
    }

    public VirtualTransform GetControllerTransform(OVRInput.Controller controller)
    {
        return new VirtualTransform
        {
            pos = OVRInput.GetLocalControllerPosition(controller),
            rot = OVRInput.GetLocalControllerRotation(controller),
        };
    }

    public VirtualTransform GetScaledDiff(VirtualTransform from, VirtualTransform to, CDRatio cd)
    {
        return new VirtualTransform
        {
            pos = ScaleByCD(to.pos - from.pos, cd),
            rot = ScaleByCD(to.rot * Quaternion.Inverse(from.rot), cd),
        };
    }

    public Vector3 ScaleByCD(Vector3 input, CDRatio cd)
    {
        return new Vector3(x: input.x * cd.Horizontal, y: input.y * cd.Vertical, z: input.z * cd.Horizontal);
    }

    public Quaternion ScaleByCD(Quaternion input, CDRatio cd)
    {
        return Quaternion.Slerp(Quaternion.identity, input, cd.Rotational);
    }

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
        currentCDIntensity = (currentCDIntensity + 1) % 3;

        NormalCDRatio = new CDRatio
        {
            Horizontal = 1.0f - (currentCDIntensity * 0.1f),
            Vertical = 1.0f - (currentCDIntensity * 0.15f),
            Rotational = 1.0f - (currentCDIntensity * 0.2f),
        };

        LoadedCDRatio = new CDRatio
        {
            Horizontal = NormalCDRatio.Horizontal - (currentCDIntensity * 0.1f),
            Vertical = NormalCDRatio.Vertical - (currentCDIntensity * 0.15f),
            Rotational = NormalCDRatio.Rotational, //Currently equal to normal, as rotational drift is unbearable
        };

        Vibrate(leftHand, time: 0.1f + currentCDIntensity * 0.5f);
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
