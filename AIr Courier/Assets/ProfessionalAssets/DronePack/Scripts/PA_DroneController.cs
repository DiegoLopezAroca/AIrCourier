using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PA_DronePack
{
    public class PA_DroneController : DroneController {
        // ✚ Get the Full version of this DronePack! ✚
        // • FULL SourceCode Access!
        // • Build & Modify Your Own Custom Drones!

        // Estos valores vendrán del Agent
        float inputForward = 0f;
        float inputRight = 0f;
        float inputUp = 0f;
        float inputYaw = 0f;

        // Llamado por DroneAgent
        public void SetInput(float forward, float right, float up, float yaw)
        {
            inputForward = Mathf.Clamp(forward, -1f, 1f);
            inputRight   = Mathf.Clamp(right, -1f, 1f);
            inputUp      = Mathf.Clamp(up, -1f, 1f);
            inputYaw     = Mathf.Clamp(yaw, -1f, 1f);
        }
    }
}

