using UnityEngine;

public class FollowDrone : MonoBehaviour
{
    public Transform target;                // arrastra aquí el Drone
    public Vector3 offset = new Vector3(0, 2, -4);
    public float smooth = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Offset en coordenadas locales del dron
        Vector3 desiredPos = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smooth * Time.deltaTime);

        transform.LookAt(target);
    }
}
