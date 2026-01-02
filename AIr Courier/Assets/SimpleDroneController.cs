using UnityEngine;

public class SimpleDroneController : MonoBehaviour
{
    public float forwardSpeed = 7f;
    // public float strafeSpeed = 5f;
    public float verticalSpeed = 5f;
    public float turnSpeed = 90f;   // grados por segundo

    // Fixed timestep for consistent physics across different time scales
    private const float FIXED_DELTA_TIME = 0.02f; // Standard Unity physics timestep (50Hz)

    public Rigidbody rb;

    float inputForward;
    // float inputRight;
    float inputUp;
    float inputYaw;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    // Lo llamará el DroneAgent
    public void SetInput(float forward, /* float right, */ float up, float yaw)
    {
        inputForward = Mathf.Clamp(forward, -1f, 1f);
        // inputRight   = Mathf.Clamp(right,   -1f, 1f);
        inputUp      = Mathf.Clamp(up,      -1f, 1f);
        inputYaw     = Mathf.Clamp(yaw,     -1f, 1f);
    }

    void FixedUpdate()
    {
        if (rb == null) return;
    
        // Movimiento traslacional
        Vector3 vel =
            transform.forward * (inputForward * forwardSpeed) +
            // transform.right   * (inputRight   * strafeSpeed) +
            Vector3.up        * (inputUp      * verticalSpeed);

        rb.linearVelocity = vel;

        // Rotación en yaw - use fixed timestep to ensure consistent behavior
        // regardless of Time.timeScale changes between training and inference
        float yawDegrees = inputYaw * turnSpeed * FIXED_DELTA_TIME;
        rb.MoveRotation(Quaternion.Euler(0f, yawDegrees, 0f) * rb.rotation);
    }
}
