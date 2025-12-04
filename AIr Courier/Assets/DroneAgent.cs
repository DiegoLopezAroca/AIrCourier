using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using PA_DronePack;
using System.Collections.Generic;

public class DroneAgent : Agent
{
    public SimpleDroneController controller;

    // GameObject target (correcto)
    [Header("Training Elements")]
    public List<GameObject> possible_targets;
    public GameObject current_target;

    private Rigidbody rb;
    private Vector3 initialDronePosition;
    private Quaternion initialDroneRotation;
    private Vector3 initialTargetPosition;

    [SerializeField] Transform spawnPoint; 

    private float lastDistanceToTarget;
    
    [Header("Reward settings")]
    public float distanceRewardScale = 0.01f;
    public float reachTargetReward = 100.0f;
    public float crashPenalty = -10.0f;
    public float timePenalty = -0.001f;
    public float targetReachThreshold = 2.0f;
    
    private bool isColliding = false;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        // Guardamos posición y rotación iniciales del dron
        initialDronePosition = spawnPoint.position;
        initialDroneRotation = spawnPoint.rotation;
    }

    private void RandomTarget()
    {
        // Desactivamos todos los posibles targets
        foreach (GameObject target in possible_targets)
        {
            if (target != null)
            {
                target.SetActive(false);
            }
        }

        // Guardamos la posición inicial del GameObject target
        if (possible_targets.Count > 0)
        {
            current_target = possible_targets[Random.Range(0, possible_targets.Count)];
            initialTargetPosition = current_target.transform.position;
            current_target.SetActive(true);
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset del dron
        transform.SetPositionAndRotation(initialDronePosition, initialDroneRotation);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isColliding = false;

        // Reset del target
        if (current_target != null)
        {
            current_target.transform.position = initialTargetPosition;
        }

        if (current_target != null)
        {
            lastDistanceToTarget = Vector3.Distance(transform.position, current_target.transform.position);
        }

        RandomTarget();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (current_target == null)
        {
            sensor.AddObservation(Vector3.zero); // relPos
            sensor.AddObservation(Vector3.zero); // velocity
            sensor.AddObservation(0f);           // altura
            return;
        }

        // Posición relativa al target
        Vector3 relPos = current_target.transform.position - controller.transform.position;
        sensor.AddObservation(relPos / 50f);

        // Velocidad del dron
        sensor.AddObservation(rb.linearVelocity / 10f);

        // Altura
        sensor.AddObservation(controller.transform.position.y / 20f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var da = actions.DiscreteActions;

        int forwardAction  = da[0]; // Branch 0
        int verticalAction = da[1]; // Branch 1
        int yawAction      = da[2]; // Branch 2

        float forward = 0f;
        if (forwardAction == 1) forward = 1f;
        else if (forwardAction == 2) forward = -1f;

        float up = 0f;
        if (verticalAction == 1) up = 1f;
        else if (verticalAction == 2) up = -1f;

        float yaw = 0f;
        if (yawAction == 1) yaw = 1f;
        else if (yawAction == 2) yaw = -1f;

        controller.SetInput(forward, up, yaw);

        ComputeStepReward();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Verificamos si chocamos contra algo que en este caso hemos etiquetado como "Edificio"
        if (collision.gameObject.CompareTag("Edificio"))
        {
            isColliding = true;
        }
    }

    private void ComputeStepReward() 
    {
        if (current_target == null) { return; }

        // Distancia actual al objetivo
        float currentDistance = Vector3.Distance(controller.transform.position, current_target.transform.position);

        // Progreso (si es positivo, nos hemos acercado)
        float distanceDelta = lastDistanceToTarget - currentDistance;

        // Recompensa por progreso
        AddReward(distanceDelta * distanceRewardScale);

        // Penalización por tiempo
        AddReward(timePenalty);

        // Penalización por colisiones
        if (isColliding)
        {
            AddReward(crashPenalty);
            isColliding = false;
            EndEpisode();
        }

        // Guardar para el siguiente step
        lastDistanceToTarget = currentDistance;

        // Comprobar si hemos llegado
        if (currentDistance < targetReachThreshold)
        {
            AddReward(reachTargetReward);
            EndEpisode();
        }
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
        a[1] = up > 0.1f ? 1 : (up < -0.1f ? 2 : 0);

        // Yaw (A/D)
        float yaw = 0f;
        if (Input.GetKey(KeyCode.A)) yaw = -1f;
        else if (Input.GetKey(KeyCode.D)) yaw = 1f;
        a[2] = yaw > 0.1f ? 1 : (yaw < -0.1f ? 2 : 0);
    }
}