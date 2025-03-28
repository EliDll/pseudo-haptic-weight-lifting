using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public class BasicTaskBehaviour : MonoBehaviour
{
    public DungeonMasterBehaviour DM;

    public GameObject FirstTarget;
    public GameObject SecondTarget;
    public GameObject ThirdTarget;

    public GameObject SecondBarrier;
    public GameObject ThirdBarrier;

    public Material DefaultBarrierMaterial;
    public Material ActiveBarrierMatieral;

    private GameObject? currentTarget;
    private GameObject? currentBarrier;

    private bool hasCollided = false;

    // Start is called before the first frame update
    void Start()
    {
        currentTarget = FirstTarget;
        FirstTarget.SetActive(true);

        SecondTarget.SetActive(false);
        SecondBarrier.SetActive(false);

        ThirdTarget.SetActive(false);
        ThirdBarrier.SetActive(false);
    }

    public void UpdateTask(GameObject cube, OVRInput.Controller grabbingController)
    {
        if (currentTarget != null)
        {
            var targetCollider = currentTarget.GetComponent<Collider>();
            var cubePos = cube.transform.position;

            //Target is considered reached when cube centre is inside its collider
            if (targetCollider.bounds.Contains(cubePos))
            {
                DM.Vibrate(grabbingController, 0.2f);
                HandleTargetReached();
            }
            else if (!hasCollided && currentBarrier != null)
            {
                var barrierCollider = currentBarrier.GetComponent<Collider>();
                var cubeCollider = cube.GetComponent<Collider>();

                if (barrierCollider.bounds.Intersects(cubeCollider.bounds))
                {
                    DM.Vibrate(grabbingController, 1.0f);
                    HandleBarrierCollision();
                }
            }
        }
    }

    private void HandleTargetReached()
    {
        //Disable current target and barrier
        if (currentTarget != null) currentTarget.SetActive(false);
        if (currentBarrier != null) currentBarrier.SetActive(false);

        if (currentTarget == FirstTarget)
        {
            currentTarget = SecondTarget;
            currentBarrier = SecondBarrier;
        }
        else if (currentTarget == SecondTarget)
        {
            currentTarget = ThirdTarget;
            currentBarrier = ThirdBarrier;
        }
        else if (currentTarget == ThirdTarget)
        {
            currentTarget = null;
            currentBarrier = null;
        }

        //Activate new target and barrier
        if (currentTarget != null) currentTarget.SetActive(true);
        if (currentBarrier != null)
        {
            currentBarrier.SetActive(true);
            //Reset barrier material to normal
            currentBarrier.GetComponent<Renderer>().material = DefaultBarrierMaterial;
            hasCollided = false;
        }
    }

    private void HandleBarrierCollision()
    {
        //Deactivate current target
        if (currentTarget != null) currentTarget.SetActive(false);

        //Set current barrier material to collided
        if (currentBarrier != null)
        {
            currentBarrier.GetComponent<Renderer>().material = ActiveBarrierMatieral;
            hasCollided = true;
        }

        if (currentTarget == SecondTarget)
        {
            currentTarget = FirstTarget;
        }
        else if (currentTarget == ThirdTarget)
        {
            currentTarget = SecondTarget;
        }

        //Activate new target
        if (currentTarget != null) currentTarget.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
