using UnityEngine;

public partial class FreeFlyCamera : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float lookSensitivity = 2f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // Para hindi lumalabas ang mouse cursor habang gumagalaw
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Rotation gamit ang Mouse
        rotationX += Input.GetAxis("Mouse X") * lookSensitivity;
        rotationY -= Input.GetAxis("Mouse Y") * lookSensitivity;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f);

        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);

        // Movement gamit ang WASD
        float moveForward = Input.GetAxis("Vertical");
        float moveSide = Input.GetAxis("Horizontal");

        Vector3 direction = (transform.forward * moveForward) + (transform.right * moveSide);
        transform.position += direction * movementSpeed * Time.deltaTime;

        // Unlock cursor pag pinindot ang Escape
        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;
    }
}