using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuCamera : MonoBehaviour
{
    [Header("Camera Rotate Parameters")]
    [SerializeField] private GameObject position;
    [SerializeField] private float speed;

    private void Update()
    {
        transform.LookAt(position.transform);
        transform.Translate(speed * Time.deltaTime * Vector3.right);
    }
}
