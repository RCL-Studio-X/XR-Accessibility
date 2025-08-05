using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaxToCrew : MonoBehaviour
{
    public string waxTag = "Wax";
    public bool requireFinalWax = true;

    // Start is called before the first frame update
    void Start()
    {
        Collider col = GetComponent<Collider>();    

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the correct tag
        if (!other.CompareTag(waxTag))
            return;
        
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaxToCrew(other.gameObject);
        }
        else
        {
            Debug.LogError("GameManager.Instance not found!");
        }
    }
}
