using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using PA_DronePack;
using System.Collections.Generic;
using System;

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
    public LayerMask obstacleLayers;


    [SerializeField] Transform spawnPoint;

    private float lastDistanceToTarget;

    [Header("Reward settings")]
    public float distanceRewardScale = 0.01f;
    public float reachTargetReward = 100.0f;
    public float crashPenalty = -0.005f; // penalización por choque
    public float timePenalty = -0.001f;
    public float targetReachThreshold = 2.0f;
    public float minDistanceRewardScale = 0.05f;   // recompensa por mejorar el récord
    public float moveAwayPenaltyScale = 0.008f;  // penaliza alejarse del récord

    [Header("Distance tracking")]
    private float minDistanceToTarget;
    private float maxAwayFromBest;
    private float maxDistanceToTarget;

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
            current_target = possible_targets[UnityEngine.Random.Range(0, possible_targets.Count)];
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

        RandomTarget();

        if (current_target != null)
        {
            float d = Vector3.Distance(
                controller.transform.position,
                current_target.transform.position
            );

            lastDistanceToTarget = d;
            minDistanceToTarget = d; // la distancia record a la que ha estado
            maxAwayFromBest = 0f;
            maxDistanceToTarget = lastDistanceToTarget;
        }
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
        sensor.AddObservation(relPos);

        // Velocidad del dron
        sensor.AddObservation(rb.linearVelocity);

        // Altura
        sensor.AddObservation(controller.transform.position.y);
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
        if (((1 << collision.gameObject.layer) & obstacleLayers.value) != 0)
        {
            AddReward(crashPenalty);
            EndEpisode();
        }
    }

    private void ComputeStepReward() 
    {
        if (current_target == null) { return; }

        // Distancia actual al objetivo
        float currentDistance = Vector3.Distance(controller.transform.position, current_target.transform.position);
        float improvement = minDistanceToTarget - currentDistance;

        if (improvement > 0f)
        {
            float extra = improvement * minDistanceRewardScale;
            //float reward = MathF.Tanh(extra * 5.0f); // suavizamos la recompensa usando tanh

            //print("Improvement, reward added: " + extra);
            AddReward(extra);

            minDistanceToTarget = currentDistance;
            maxAwayFromBest = 0f; // reiniciar alejamiento
        }
        else
        {
            // Se ha alejado del récord
            float remoteness = (currentDistance - minDistanceToTarget) - 1.0f; // le meto un metro de tolerancia para que pueda rodear objetos sin penalizar
            if (remoteness > maxAwayFromBest)
            {
                float penalty = (remoteness - maxAwayFromBest) * moveAwayPenaltyScale;
                //float penalty = MathF.Tanh(extra * moveAwayPenaltyScale * 5.0f); // provamos a usar tanh para suavizar y penalizar menos a lo bestia y penalizar mas cuando se aleja mucho

                AddReward(-penalty);
                
                maxAwayFromBest = remoteness;
                //print("Moved away from best, negative reward: " + -penalty);
            }            
        }

        float speed = rb.linearVelocity.magnitude;
        if (speed < 0.2f)
        {
            AddReward(-0.0002f);
        }
        // Penalización por tiempo
        AddReward(timePenalty);

        // Comprobar si hemos llegado
        if (currentDistance < targetReachThreshold)
        {
            //print("Target reached!");
            AddReward(reachTargetReward);
            EndEpisode();
            return;
        }

        // Guardar para el siguiente step
        lastDistanceToTarget = currentDistance;
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