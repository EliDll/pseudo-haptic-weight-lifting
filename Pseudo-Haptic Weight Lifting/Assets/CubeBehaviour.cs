using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeBehaviour : MonoBehaviour
{
    public GameObject obj;
    public Rigidbody rb;
    public Collider coll;
    public Collider barrier;

    public Material defaultMat;
    public Material activeMat;
    public Material dragMaterial;
    public float CD = 1.0f;

    private bool dragging = false;
    private Vector3 dragOrigin;

    private OVRInput.Button draggingButton;
    private OVRInput.Controller draggingController;

    private Quaternion controllerInitRot;
    private Quaternion objInitRot;

    private bool hasCollided = false;

    private void StopDragging()
    {
        rb.isKinematic = false;
        dragging = false;
        obj.GetComponent<Renderer>().material = defaultMat;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (hasCollided)
        {
            //Await button release before allowing to drag again
            var released = !OVRInput.Get(draggingButton);

            if (released)
            {
                hasCollided = false;
            }
        }
        else
        {
            Vector3 handLeftPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            Vector3 handRightPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);


            if (!dragging)
            {
                var leftTouch = coll.bounds.Contains(handLeftPosition);
                var rightTouch = coll.bounds.Contains(handRightPosition);

                if (leftTouch || rightTouch)
                {
                    obj.GetComponent<Renderer>().material = activeMat;

                    var leftPress = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
                    var rightPress = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

                    if (leftPress || rightPress)
                    {
                        obj.GetComponent<Renderer>().material = dragMaterial;

                        dragging = true;
                        rb.isKinematic = true;

                        draggingButton = leftPress ? OVRInput.Button.PrimaryHandTrigger : OVRInput.Button.SecondaryHandTrigger;
                        draggingController = leftPress ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

                        dragOrigin = OVRInput.GetLocalControllerPosition(draggingController);
                        controllerInitRot = OVRInput.GetLocalControllerRotation(draggingController);
                        objInitRot = obj.transform.rotation;

                    }
                    else
                    {
                        obj.GetComponent<Renderer>().material = activeMat;
                    }
                }
                else
                {
                    obj.GetComponent<Renderer>().material = defaultMat;
                }
            }
            else
            {
                var released = !OVRInput.Get(draggingButton);
                var collided = coll.bounds.Intersects(barrier.bounds);

                if (released)
                {
                    StopDragging();
                }
                else if (collided)
                {
                    hasCollided = true;
                    StopDragging();
                }
                else
                {
                    //Continue dragging
                    var controllerPos = OVRInput.GetLocalControllerPosition(draggingController);
                    var controllerRot = OVRInput.GetLocalControllerRotation(draggingController);

                    var rotDiff = controllerRot * Quaternion.Inverse(controllerInitRot);

                    var distToOrigin = controllerPos - dragOrigin;

                    obj.transform.position = dragOrigin + distToOrigin * CD;
                    obj.transform.rotation = Quaternion.Slerp(Quaternion.identity, rotDiff, CD) * objInitRot;
                }
            }
        }


    }
}
