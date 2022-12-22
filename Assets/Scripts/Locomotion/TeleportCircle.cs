using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportCircle : MonoBehaviour
{
    private Material _material;

    public Color goodTeleportColor;
    public Color badTeleportColor;
    public LayerMask layerMask;

    private void Awake()
    {
        _material = GetComponent<MeshRenderer>().material;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (1 << other.gameObject.layer != layerMask)
        {
            _material.color = goodTeleportColor;
        }
        else
        {
            _material.color = badTeleportColor;
        }
    }
}
