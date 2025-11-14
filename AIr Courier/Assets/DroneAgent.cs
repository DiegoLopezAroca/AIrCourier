using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using PA_DronePack;

public class DroneAgent : Agent
{
    public SimpleDroneController controller;
    public Transform target;                // punto de entrega/objetivo

    

    private Rigidbody rb;
    private Vector3 initialDronePosition;
    private Quaternion initialDroneRotation;
    private Vector3 initialTargetPosition;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        initialDronePosition = transform.position;
        initialDroneRotation = transform.rotation;
        if (target != null)
        {
            initialTargetPosition = target.position;
        }
    }

    public override void OnEpisodeBegin()
    {
        transform.SetPositionAndRotation(initialDronePosition, initialDroneRotation);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (target != null)
        {
            target.position = initialTargetPosition;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (target == null)
        {
            // Por si acaso aún no has asignado el target
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
            return;
        }

        // 1. Posición relativa al objetivo (3 floats)
        Vector3 relPos = target.position - transform.position;
        sensor.AddObservation(relPos / 50f);   // normalizar un poco

        // 2. Velocidad (3 floats)
        sensor.AddObservation(rb.linearVelocity / 10f);

        // 3. Altura (1 float)
        sensor.AddObservation(transform.position.y / 20f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 4 ramas discretas
        int forwardAction  = actions.DiscreteActions[0];
        int lateralAction  = actions.DiscreteActions[1];
        int verticalAction = actions.DiscreteActions[2];
        int yawAction      = actions.DiscreteActions[3];

        float forward = 0f;
        if (forwardAction == 1) forward = 1f;
        else if (forwardAction == 2) forward = -1f;

        float right = 0f;
        if (lateralAction == 1) right = 1f;
        else if (lateralAction == 2) right = -1f;

        float up = 0f;
        if (verticalAction == 1) up = 1f;
        else if (verticalAction == 2) up = -1f;

        float yaw = 0f;
        if (yawAction == 1) yaw = 1f;
        else if (yawAction == 2) yaw = -1f;

        // Enviar inputs al controlador del dron
        controller.SetInput(forward, right, up, yaw);

        // Pequeña penalización por tiempo
        AddReward(-0.001f);
    }

    // Control manual con teclado
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var a = actionsOut.DiscreteActions;

        // forward/back (W/S)
        float v = Input.GetAxisRaw("Vertical");
        a[0] = v > 0.1f ? 1 : (v < -0.1f ? 2 : 0);

        // right/left (A/D)
        float h = Input.GetAxisRaw("Horizontal");
        a[1] = h > 0.1f ? 1 : (h < -0.1f ? 2 : 0);

        // up/down (Q/E)
        float up = 0f;
        if (Input.GetKey(KeyCode.Q)) up = 1f;
        else if (Input.GetKey(KeyCode.E)) up = -1f;
        a[2] = up > 0.1f ? 1 : (up < -0.1f ? 2 : 0);

        // yaw (Z/X)
        float yaw = 0f;
        if (Input.GetKey(KeyCode.Z)) yaw = -1f;
        else if (Input.GetKey(KeyCode.X)) yaw = 1f;
        a[3] = yaw > 0.1f ? 1 : (yaw < -0.1f ? 2 : 0);
    }
}
