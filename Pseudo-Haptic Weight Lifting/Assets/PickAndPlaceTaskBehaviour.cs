using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public class PickAndPlaceTaskBehaviour : MonoBehaviour
{
    public DungeonMasterBehaviour DM;

    public GameObject FirstTarget;
    public GameObject SecondTarget;
    public GameObject ThirdTarget;

    public GameObject SecondBarrier;
    public GameObject ThirdBarrier;

    public Material DefaultBarrierMaterial;
    public Material ActiveBarrierMatieral;

    public Material CompletionMaterial;

    private GameObject? currentTarget;
    private GameObject? currentBarrier;

    private bool hasCollided = false;

    private int targetReached = 0;

    private int collisionCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        FirstTarget.GetComponent<Renderer>().enabled = true;

        currentTarget = FirstTarget;

        SecondTarget.GetComponent<Renderer>().enabled = false;
        SecondBarrier.SetActive(false);

        ThirdTarget.GetComponent<Renderer>().enabled = false;
        ThirdBarrier.SetActive(false);
    }

    public int GetTargetReached()
    {
        return targetReached;
    }

    public int GetCollisionCount()
    {
        return collisionCount;
    }

    public void UpdateTask(GameObject cube, GrabAnchor grabAnchor)
    {
        if (currentTarget != null)
        {
            var targetCollider = currentTarget.GetComponent<Collider>();
            var cubePos = cube.transform.position;

            //Target is considered reached when cube centre is inside its collider
            if (targetCollider.bounds.Contains(cubePos))
            {
                DM.TryVibrate(grabAnchor, 0.2f);
                HandleTargetReached();
            }
            else if (!hasCollided && currentBarrier != null)
            {
                var barrierCollider = currentBarrier.GetComponent<Collider>();
                var cubeCollider = cube.GetComponent<Collider>();

                if (barrierCollider.bounds.Intersects(cubeCollider.bounds))
                {
                    DM.TryVibrate(grabAnchor, 1.0f);
                    HandleBarrierCollision();
                }
            }
        }
    }

    private void HandleTargetReached()
    {
        //Disable current target and barrier
        if (currentTarget != null)
        {
            currentTarget.GetComponent<AudioSource>().Play();

            if(currentTarget == ThirdTarget)
            {
                currentTarget.GetComponent<Renderer>().material = CompletionMaterial;
}
            else
            {
                currentTarget.GetComponent<Renderer>().enabled = false;
            }
        }
        if (currentBarrier != null) currentBarrier.SetActive(false);

        if (currentTarget == FirstTarget)
        {
            currentTarget = SecondTarget;
            currentBarrier = SecondBarrier;
            targetReached = 1;
        }
        else if (currentTarget == SecondTarget)
        {
            currentTarget = ThirdTarget;
            currentBarrier = ThirdBarrier;
            targetReached = 2;
        }
        else if (currentTarget == ThirdTarget)
        {
            currentTarget = null;
            currentBarrier = null;
            targetReached = 3;
        }

        //Activate new target and barrier
        if (currentTarget != null) currentTarget.GetComponent<Renderer>().enabled = true;
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
        collisionCount++;

        //Deactivate current target
        if (currentTarget != null)currentTarget.GetComponent<Renderer>().enabled = false;

        //Set current barrier material to collided
        if (currentBarrier != null)
        {
            currentBarrier.GetComponent<AudioSource>().Play();
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
        if (currentTarget != null) currentTarget.GetComponent<Renderer>().enabled = true;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
