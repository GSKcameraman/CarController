using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    private Rigidbody playerRB;
    public float speed = 10;
    public Vector3 offset = new Vector3(0,2,0);
    // Start is called before the first frame update
    void Start()
    {
        playerRB = player.GetComponent<Rigidbody>();

    }

    void Awake(){
        player = GameObject.FindGameObjectWithTag("Player");
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        Follow();
    }

    void Follow(){
        Vector3 playerForward = (playerRB.velocity + player.transform.forward).normalized;
        transform.position = Vector3.Lerp(transform.position, 
                            player.transform.position + player.transform.TransformVector(offset) + playerForward * (-8f),
                                speed * Time.deltaTime);
        transform.LookAt(player.transform);
    }
}
