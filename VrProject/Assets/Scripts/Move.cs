using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour {

    public float speed_x = 2.5f;
    public float speed_y = 2.5f;
    public float speed_z = 2.5f;

    private float starting_X;
    private float starting_Y;
    private float starting_Z;

    void Start() {
        starting_X = transform.position.x;
        starting_Y = transform.position.y;
        starting_Z = transform.position.z;
    }

    // Update is called once per frame
    void Update () {
        transform.position = new Vector3(Mathf.PingPong(Time.time * speed_x, 8) + starting_X, transform.position.y, transform.position.z);
        transform.position = new Vector3(transform.position.x, Mathf.PingPong(Time.time * speed_y, 3) + starting_Y, transform.position.z);
        transform.position = new Vector3(transform.position.x, transform.position.y, Mathf.PingPong(Time.time * speed_z, 1) + starting_Z);

    }
}
