using Oculus.Interaction.GrabAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
#nullable enable

public class DungeonMasterBehaviour : MonoBehaviour
{
    public string LogDir;

    public GameObject BasicTask;
    public GameObject ShovellingTask;
    public OVRControllerHelper LeftController;
    public OVRControllerHelper RightController;

    public GameObject TrackedRightHand;
    public GameObject TrackedLeftHand;
    public GameObject TrackedShovelGeometry;
    public GameObject TrackedCube;
    public GameObject TrackedHMD;

    public OVRCameraRig CameraRig;
    public GameObject ConditionDisplay;

    public CDParams? NormalCD;
    public CDParams? LoadedCD;

    private GameObject? currentTask;
    private Condition currentCondition = Condition.C0;

    private OVRInput.Button currentPressedButton = OVRInput.Button.None;

    private bool showDebugObjects = false;

    private StreamWriter? logWriter = null;

    public bool IsTrackingEnabled()
    {
        return currentCondition == Condition.P0 || currentCondition == Condition.P1 || currentCondition == Condition.P2;
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

    private void InitLogger(string fileName)
    {

        var logFilePath = Path.Combine(LogDir, $"{fileName}.csv");

        if (File.Exists(logFilePath)) File.Delete(logFilePath);

        logWriter?.Dispose();

        logWriter = new StreamWriter(logFilePath, true);

        var logKeys = new string[] {
            "TS.DT",
            "TS.Unix",
            "PrimaryMode",
            "PT.X",
            "PT.Y",
            "PT.Z",
            "ST.X",
            "ST.Y",
            "ST.Z",
            "PV.X",
            "PV.Y",
            "PV.Z",
            "SV.X",
            "SV.Y",
            "SV.Z",
            "HMD.X",
            "HMD.Y",
            "HMD.Z",
            "EE.X",
            "EE.Y",
            "EE.Z",
            "Loaded (Shovel)",
            "Target Reached (Cube)",
            "Grab Count",
            "Collision Count"
        };
        logWriter.WriteLine(string.Join(",", logKeys));
    }

    public void Log(LogEntry e)
    {
        var values = new string[] {
        DateTime.Now.ToString(),
        DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString(),
        e.PrimaryMode.ToString(),
        e.PrimaryTracked.x.ToString(),
        e.PrimaryTracked.y.ToString(),
        e.PrimaryTracked.z.ToString(),
        e.SecondaryTracked.x.ToString(),
        e.SecondaryTracked.y.ToString(),
        e.SecondaryTracked.z.ToString(),
        e.PrimaryVisible.x.ToString(),
        e.PrimaryVisible.y.ToString(),
        e.PrimaryVisible.z.ToString(),
        e.SecondaryVisible.x.ToString(),
        e.SecondaryVisible.y.ToString(),
        e.SecondaryVisible.z.ToString(),
        e.HMD.x.ToString(),
        e.HMD.y.ToString(),
        e.HMD.z.ToString(),
        e.EndEffectorVisible.x.ToString(),
        e.EndEffectorVisible.y.ToString(),
        e.EndEffectorVisible.z.ToString(),
        e.ShovelLoaded.ToString(),
        e.CubeReachedTarget.ToString(),
        e.GrabCount.ToString(),
        e.CollisionCount.ToString(),
        };

        logWriter?.WriteLine(string.Join(",", values));
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

    public void ToggleDebugObjects(bool show)
    {
        showDebugObjects = show;

        if (showDebugObjects)
        {
            LeftController.m_showState = OVRInput.InputDeviceShowState.Always;
            RightController.m_showState = OVRInput.InputDeviceShowState.Always;

        }
        else
        {
            LeftController.m_showState = OVRInput.InputDeviceShowState.ControllerNotInHand;
            RightController.m_showState = OVRInput.InputDeviceShowState.ControllerNotInHand;
        }

        TrackedCube.GetComponent<Renderer>().enabled = showDebugObjects;
        TrackedLeftHand.GetComponent<Renderer>().enabled = showDebugObjects;
        TrackedRightHand.GetComponent<Renderer>().enabled = showDebugObjects;

        ConditionDisplay.SetActive(showDebugObjects);
        TrackedShovelGeometry.SetActive(showDebugObjects);
    }

    private void StartTask(GameObject task)
    {
        ToggleDebugObjects(false);

        if (currentTask != null) GameObject.Destroy(currentTask);
        currentTask = GameObject.Instantiate(task);
        currentTask.SetActive(true);

        var logFileName = $"{(task == ShovellingTask ? "Shovel" : "Cube")}_{currentCondition}";
        InitLogger(logFileName);
    }

    private void ChangeCondition()
    {
        currentCondition = currentCondition switch
        {
            Condition.C0 => Condition.C1,
            Condition.C1 => Condition.C2,
            Condition.C2 => Condition.P0,
            Condition.P0 => Condition.P1,
            Condition.P1 => Condition.P2,
            Condition.P2 => Condition.C0,
            _ => throw new System.NotImplementedException()
        };

        ConditionDisplay.GetComponent<TextMeshPro>().text = currentCondition.ToString();

        NormalCD = currentCondition switch
        {
            Condition.C0 => null,
            Condition.C1 => CDParams.Subtle,
            Condition.C2 => CDParams.Pronounced,
            Condition.P0 => null,
            Condition.P1 => CDParams.Subtle,
            Condition.P2 => CDParams.Pronounced,
            _ => throw new System.NotImplementedException()
        };

        LoadedCD = currentCondition switch
        {
            Condition.C0 => null,
            Condition.C1 => CDParams.Subtle_Loaded,
            Condition.C2 => CDParams.Pronounced_Loaded,
            Condition.P0 => null,
            Condition.P1 => CDParams.Subtle_Loaded,
            Condition.P2 => CDParams.Pronounced_Loaded,
            _ => throw new System.NotImplementedException()
        };


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

        ToggleDebugObjects(true);

        ConditionDisplay.GetComponent<TextMeshPro>().text = currentCondition.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (ConditionDisplay.activeSelf)
        {
            var head = Calc.GetHeadPose(CameraRig);
            ConditionDisplay.transform.position = head.position + head.forward * 2.0f;
            ConditionDisplay.transform.rotation = Quaternion.LookRotation(head.forward);
        }

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

        else if (CheckPress(Defs.LeftThumbstickPress)) ChangeCondition();

        else if (CheckPress(Defs.RightThumbstickPress)) ToggleDebugObjects(!showDebugObjects);
    }
}
