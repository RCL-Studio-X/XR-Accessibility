using System.Collections.Generic;
using UnityEngine;
using HurricaneVR.Framework.Core.Stabbing;

public class WaxStateManager : MonoBehaviour
{
    [Header("Wax Models (Assign in Order)")]
    public List<GameObject> waxModels;

    [Header("Optional: Assign Stabbable Component (Auto-detected if left empty)")]
    public HVRStabbable stabbable;

    [Header("Regrabbing to \"mold\" into final model")]
    [Tooltip("Number of grabs required to mold into final model.")]
    public int numGrabsRequired = 5;

    [Tooltip("Final wax model GameObject")]
    public GameObject finalWaxModel;

    [Tooltip("MeshCollider to apply final mesh to")]
    public MeshCollider waxMeshCollider;
    public MeshCollider finalWaxMeshCollider;

    [Header("Stab Direction Restriction")]
    public bool requireDownwardStab = false;

    [Tooltip("Stabber GameObject (e.g. sword)")]
    public GameObject stabber;

    private int currentIndex = 0;
    private int currentNumGrabs = 0;
    private bool hasNotifiedCutting = false;      // Track if we've notified about cutting completion
    private bool hasNotifiedSoftening = false;
    private bool _isAtFinalWaxModel = false;

    void Start()
    {
        // Auto-assign stabbable if not provided
        if (stabbable == null)
        {
            stabbable = GetComponent<HVRStabbable>();
            if (stabbable == null)
            {
                Debug.LogError($"HVRStabbable not found on '{gameObject.name}' and not assigned in Inspector.");
            }
        }

        // Enable only the first wax model
        for (int i = 0; i < waxModels.Count; i++)
        {
            if (waxModels[i] != null)
            {
                waxModels[i].GetComponent<MeshRenderer>().enabled = (i == currentIndex);
            }
        }

        // Hide final model at start
        if (finalWaxModel != null)
        {
            finalWaxModel.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    public void SwitchToNextWax()
    {
        if (requireDownwardStab)
        {
            if (stabber == null)
            {
                Debug.LogWarning("Downward stab check enabled, but stabber not assigned.");
                return;
            }

            if (stabber.transform.position.y > transform.position.y)
            {
                // Not a downward stab
                return;
            }
        }

        if (currentIndex < waxModels.Count - 1)
        {
            waxModels[currentIndex].GetComponent<MeshRenderer>().enabled = false;
            currentIndex++;
            waxModels[currentIndex].GetComponent<MeshRenderer>().enabled = true;
        }

        //if (IsAtLastWax() && stabbable != null)
        //{
        //    stabbable.enabled = false;
        //}

        if (IsAtLastWax() && !hasNotifiedCutting)
        {
            hasNotifiedCutting = true;

            // disable stabbing once cutting is done
            if (stabbable != null)
            {
                stabbable.enabled = false;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnWaxCut();
            }
        }
    }

    public void OnGrabWax()
    {
        if (!IsAtLastWax())
            return;

        currentNumGrabs++;

        if (currentNumGrabs >= numGrabsRequired && !hasNotifiedSoftening)
        {
            hasNotifiedSoftening = true;

            waxModels[currentIndex].GetComponent<MeshRenderer>().enabled = false;

            _isAtFinalWaxModel = true;

            if (finalWaxModel != null)
            {
                finalWaxModel.GetComponent<MeshRenderer>().enabled = true;

                if (waxMeshCollider != null)
                {
                   waxMeshCollider.enabled = false;
                    if (finalWaxMeshCollider != null)
                    {
                        finalWaxMeshCollider.enabled = true;
                    }
                    else
                    {
                        Debug.LogWarning("Final wax model Mesh Collider not assigned.");
                    }
                }
                else
                {
                    Debug.LogWarning("Wax model Mesh Collider not assigned.");
                }
            }
            else
            {
                Debug.LogWarning("Final wax model GameObject not assigned.");
            }

            // notify game manager soften is completed
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnWaxSoften();
            }
        }
    }

    bool IsAtLastWax()
    {
        return currentIndex >= waxModels.Count - 1;
    }

    public bool IsAtFinalWaxModel()
    {
        return _isAtFinalWaxModel;
    }
}
