using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public class LooseLoadBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //Ignore collisions at start of lifetime to not immediately spasm out on shovel

        var rigidBody = GetComponent<Rigidbody>();
        rigidBody.detectCollisions = false;
        Invoke("EnableCollision", 0.2f);
    }

    void EnableCollision()
    {
        var rigidBody = GetComponent<Rigidbody>();
        rigidBody.detectCollisions = true;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
