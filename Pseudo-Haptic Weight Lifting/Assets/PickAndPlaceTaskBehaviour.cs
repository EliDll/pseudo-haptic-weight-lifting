using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
#nullable enable

public class PickAndPlaceTaskBehaviour : MonoBehaviour
{
    public DungeonMasterBehaviour DM;

    public GameObject FirstTarget;
    public GameObject SecondTarget;
    public GameObject ThirdTarget;
    public GameObject FourthTarget;

    public GameObject[] FirstBarrier;
    public GameObject[] SecondBarrier;
    public GameObject[] ThirdBarrier;
    public GameObject[] FourthBarrier;

    public Material DefaultBarrierMaterial;
    public Material ActiveBarrierMatieral;

    public Material CompletionMaterial;

    private int currentBarrier;
    private int currentTarget;

    private bool hasAlreadyCollided = false;

    private int collisionCount = 0;
    private int maxTargetReached = 0;

    private GameObject[] allTargets;
    private GameObject[] allBarriers;

    // Start is called before the first frame update
    void Start()
    {
        allTargets = new GameObject[] { FirstTarget, SecondTarget, ThirdTarget, FourthTarget };

        var barriers = new List<GameObject>();
        barriers.AddRange(FirstBarrier);
        barriers.AddRange(SecondBarrier);
        barriers.AddRange(ThirdBarrier);
        barriers.AddRange(FourthBarrier);

        allBarriers = barriers.ToArray();

        var next = 1;
        SetTarget(next);
        SetBarrier(next);
    }

    public int GetMaxTargetReached()
    {
        return maxTargetReached;
    }

    public int GetCollisionCount()
    {
        return collisionCount;
    }

    public void SetTarget(int targetNo)
    {
        var newTarget = GetTarget(targetNo);

        if (newTarget != null)
        {
            foreach (var obj in allTargets)
            {
                obj.GetComponent<Renderer>().enabled = false;
            }

            newTarget.GetComponent<Renderer>().enabled = true;

            if (maxTargetReached < targetNo) maxTargetReached = targetNo;
        }
        else
        {
            var target = GetTarget(currentTarget);
            if(target != null ) target.GetComponent<Renderer>().material = CompletionMaterial;
        }

        currentTarget = targetNo;
    }

    public void SetBarrier(int barrierNo)
    {
        foreach (var obj in allBarriers)
        {
            obj.SetActive(false);
        }

        var newBarrier = GetBarrier(barrierNo);

        foreach(var obj in newBarrier)
        {
            obj.SetActive(true);
            obj.GetComponent<Renderer>().material = DefaultBarrierMaterial;
        }

        currentBarrier = barrierNo;
    }

    public GameObject[] GetBarrier(int targetNo)
    {
        return targetNo switch
        {
            1 => FirstBarrier,
            2 => SecondBarrier,
            3 => ThirdBarrier,
            4 => FourthBarrier,
            _ => new GameObject[0]
        };
    }

    public GameObject? GetTarget(int barrierNo)
    {
        return barrierNo switch
        {
            1 => FirstTarget,
            2 => SecondTarget,
            3 => ThirdTarget,
            4 => FourthTarget,
            _ => null
        };
    }

    public void UpdateTask(GameObject cube, GrabAnchor grabAnchor)
    {
        var target = GetTarget(currentTarget);

        if (target != null)
        {
            var cubePos = cube.transform.position;

            var targetReached = target.GetComponent<Collider>().bounds.Contains(cubePos);

            if (targetReached)
            {
                DM.TryVibrate(grabAnchor, 0.2f);
                target.GetComponent<AudioSource>().Play();

                hasAlreadyCollided = false;
                var next = currentTarget + 1;
                SetTarget(next);
                SetBarrier(next);
            }
            else if (!hasAlreadyCollided)
            {
                var cubeCollider = cube.GetComponent<Collider>();
                var barrierObjects = GetBarrier(currentBarrier);

                foreach (var barrierObj in barrierObjects)
                {
                    if (barrierObj.GetComponent<Collider>().bounds.Intersects(cubeCollider.bounds))
                    {
                        DM.TryVibrate(grabAnchor, 1.0f);
                        barrierObj.GetComponent<AudioSource>().Play();

                        foreach(var barrierObj2 in barrierObjects)
                        {
                            barrierObj2.GetComponent<Renderer>().material = ActiveBarrierMatieral;
                        }

                        collisionCount++;
                        hasAlreadyCollided = true;

                        SetTarget(currentTarget - 1);

                        break;
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
