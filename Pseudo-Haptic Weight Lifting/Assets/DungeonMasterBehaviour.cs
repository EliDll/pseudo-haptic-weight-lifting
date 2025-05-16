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

    public GameObject PickAndPlaceTask;
    public GameObject ShovellingTask;
    public OVRControllerHelper LeftController;
    public OVRControllerHelper RightController;

    public GameObject TrackedRightController;
    public GameObject TrackedLeftController;

    public GameObject TrackedRightHand;
    public GameObject TrackedLeftHand;
    public GameObject TrackedShovelGeometry;
    public GameObject TrackedCube;
    public GameObject TrackedHMD;

    public GameObject CameraRigObj;
    public OVRCameraRig CameraRig;
    public GameObject ConditionDisplay;

    public CDParams? NormalCD;
    public CDParams? LoadedCD;

    private GameObject? currentTask;
    private Condition currentCondition;

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
            GrabAnchor.LeftController => Calc.GetPose(TrackedLeftController.transform),
            GrabAnchor.RightController => Calc.GetPose(TrackedRightController.transform),
            GrabAnchor.LeftHand => Calc.GetPose(TrackedLeftHand.transform),
            GrabAnchor.RightHand => Calc.GetPose(TrackedRightHand.transform),
            _ => new Pose()
        };
    }

    private void InitLogger(string fileName)
    {

        var logFilePath = Path.Combine(LogDir, $"{fileName}_{DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss")}.csv");
        

        logWriter?.Dispose();

        logWriter = new StreamWriter(logFilePath, true);

        var logKeys = new string[] {
            "ts_datetime",
            "ts_unix_ms",
            "primary_grab_hand",
            "pt_x",
            "pt_y",
            "pt_z",
            "st_x",
            "st_y",
            "st_z",
            "pv_x",
            "pv_y",
            "pv_z",
            "sv_x",
            "sv_y",
            "sv_z",
            "hmd_x",
            "hmd_y",
            "hmd_z",
            "ee_x",
            "ee_y",
            "ee_z",
            "shovel_loaded_state",
            "cube_max_target_reached",
            "grab_count",
            "collision_or_load_count"
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

    private void ResetCamera(Vector3 targetPosition, float targetYRotation)
    {
        var centreEye = CameraRig.centerEyeAnchor.transform;

        float currentRotY = centreEye.eulerAngles.y;
        float difference = targetYRotation - currentRotY;
        CameraRigObj.transform.Rotate(0, difference, 0);

        Vector3 newPos = new Vector3(targetPosition.x - centreEye.position.x, targetPosition.y - centreEye.position.y, targetPosition.z - centreEye.position.z);
        CameraRigObj.transform.position += newPos;
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

    private void StartTask(GameObject? task)
    {
        if (currentTask != null) GameObject.Destroy(currentTask);

        if(task != null)
        {
            var targetPosition = TrackedHMD.transform.position;
            var targetYRotation = TrackedHMD.transform.eulerAngles.y;

            ResetCamera(targetPosition, targetYRotation);

            ToggleDebugObjects(false);

            currentTask = GameObject.Instantiate(task);
            currentTask.SetActive(true);

            var logFileName = $"{(task == ShovellingTask ? "Shovel" : "Cube")}_{currentCondition}";
            InitLogger(logFileName);
        }
        else
        {
            ToggleDebugObjects(true);
        }
    }

    private void SetCondition(Condition condition)
    {
        currentCondition = condition;

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

    private void CycleCondition()
    {
        var newCondition = currentCondition switch
        {
            Condition.C0 => Condition.C1,
            Condition.C1 => Condition.C2,
            Condition.C2 => Condition.P0,
            Condition.P0 => Condition.P1,
            Condition.P1 => Condition.P2,
            Condition.P2 => Condition.C0,
            _ => throw new System.NotImplementedException()
        };

        SetCondition(newCondition);
    }

    // Start is called before the first frame update
    void Start()
    {
        currentTask = null;
        PickAndPlaceTask.SetActive(false);
        ShovellingTask.SetActive(false);
        ToggleDebugObjects(true);

        ConditionDisplay.GetComponent<TextMeshPro>().text = currentCondition.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        //Billboard behaviour
        if (ConditionDisplay.activeSelf)
        {
            var head = Calc.GetHeadPose(CameraRig);
            ConditionDisplay.transform.position = head.position + head.forward * 2.0f;
            ConditionDisplay.transform.rotation = Quaternion.LookRotation(head.forward);
        }

        if (OVRInput.GetDown(Defs.ButtonA)) StartTask(PickAndPlaceTask);

        else if (OVRInput.GetDown(Defs.ButtonB)) StartTask(ShovellingTask);

        else if (OVRInput.GetDown(Defs.LeftThumbstickPress)) CycleCondition();

        else if (OVRInput.GetDown(Defs.RightThumbstickPress)) ToggleDebugObjects(!showDebugObjects);

        else if (Input.GetKeyDown(KeyCode.KeypadMinus)) StartTask(null);
        else if (Input.GetKeyDown(KeyCode.KeypadPlus)) StartTask(PickAndPlaceTask);
        else if (Input.GetKeyDown(KeyCode.KeypadEnter)) StartTask(ShovellingTask);

        else if (Input.GetKeyDown(KeyCode.Keypad1)) SetCondition(Condition.C0);
        else if (Input.GetKeyDown(KeyCode.Keypad2)) SetCondition(Condition.C1);
        else if (Input.GetKeyDown(KeyCode.Keypad3)) SetCondition(Condition.C2);

        else if (Input.GetKeyDown(KeyCode.Keypad4)) SetCondition(Condition.P0);
        else if (Input.GetKeyDown(KeyCode.Keypad5)) SetCondition(Condition.P1);
        else if (Input.GetKeyDown(KeyCode.Keypad6)) SetCondition(Condition.P2);
    }
}
