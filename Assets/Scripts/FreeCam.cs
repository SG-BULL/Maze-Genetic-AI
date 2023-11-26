using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// van ChatGPT, prompt: how to make it so you can move your camera around in game view, like a free cam
public class FreeCam : MonoBehaviour
{
    public float movementSpeed = 300f;
    public float rotationSpeed = 2f;

    void Update()
    {
        // Handle camera movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        transform.Translate(moveDirection * movementSpeed * Time.deltaTime, Space.Self);

        // Handle camera rotation
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        transform.Rotate(Vector3.up, mouseX * rotationSpeed);
        transform.Rotate(Vector3.left, mouseY * rotationSpeed);

        // Clamp camera rotation on X-axis to avoid flipping
        Vector3 currentRotation = transform.rotation.eulerAngles;
        currentRotation.x = Mathf.Clamp(currentRotation.x, -90f, 90f);
        transform.rotation = Quaternion.Euler(currentRotation);
    }
}