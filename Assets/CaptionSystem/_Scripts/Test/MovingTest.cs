using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MovingTest : MonoBehaviour
{
    public float speed = 1f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        Vector3 movingDir = Vector3.forward;

        transform.Translate(movingDir * speed * Time.deltaTime);
        transform.Rotate(Vector3.up * Time.deltaTime * 15f);
    }

}
