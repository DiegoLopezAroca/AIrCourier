using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using PA_DronePack;

public class DroneAgent : Agent
{
    public SimpleDroneController controller;

    // GameObject target (correcto)
    public GameObject go_target;

    private Rigidbody rb;
    private Vector3 initialDronePosition;
    private Quaternion initialDroneRotation;
    private Vector3 initialTargetPosition;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        // Guardamos posición y rotación iniciales del dron
        initialDronePosition = transform.position;
        initialDroneRotation = transform.rotation;

        // Guardamos la posición inicial del GameObject target
        if (go_target != null)
        {
            initialTargetPosition = go_target.transform.position;
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset del dron
        transform.SetPositionAndRotation(initialDronePosition, initialDroneRotation);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset del target
        if (go_target != null)
        {
            go_target.transform.position = initialTargetPosition;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (go_target == null)
        {
            sensor.AddObservation(Vector3.zero); // relPos
            sensor.AddObservation(Vector3.zero); // velocity
            sensor.AddObservation(0f);           // altura
            return;
        }

        // Posición relativa al target
        Vector3 relPos = go_target.transform.position - transform.position;
        sensor.AddObservation(relPos / 50f);

        // Velocidad del dron
        sensor.AddObservation(rb.linearVelocity / 10f);

        // Altura
        sensor.AddObservation(transform.position.y / 20f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int forwardAction  = actions.DiscreteActions[0];
        int verticalAction = actions.DiscreteActions[2];
        int yawAction      = actions.DiscreteActions[3];

        float forward = 0f;
        if (forwardAction == 1) forward = 1f;
        else if (forwardAction == 2) forward = -1f;

        float up = 0f;
        if (verticalAction == 1) up = 1f;
        else if (verticalAction == 2) up = -1f;

        float yaw = 0f;
        if (yawAction == 1) yaw = 1f;
        else if (yawAction == 2) yaw = -1f;

        // Aplicar entradas al controlador del dron
        controller.SetInput(forward, up, yaw);

        // Penalización por step
        AddReward(-0.001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var a = actionsOut.DiscreteActions;

        // Forward/back (W/S)
        float v = Input.GetAxisRaw("Vertical");
        a[0] = v > 0.1f ? 1 : (v < -0.1f ? 2 : 0);

        // Up/down (Q/E)
        float up = 0f;
        if (Input.GetKey(KeyCode.Q)) up = 1f;
        else if (Input.GetKey(KeyCode.E)) up = -1f;
        a[2] = up > 0.1f ? 1 : (up < -0.1f ? 2 : 0);

        // Yaw (A/D)
        float yaw = 0f;
        if (Input.GetKey(KeyCode.A)) yaw = -1f;
        else if (Input.GetKey(KeyCode.D)) yaw = 1f;
        a[3] = yaw > 0.1f ? 1 : (yaw < -0.1f ? 2 : 0);
    }
}