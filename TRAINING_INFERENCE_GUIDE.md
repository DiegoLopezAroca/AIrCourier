# Training-Inference Consistency Guide

## Problem Statement
When training ML-Agents models, there can be a significant performance difference between training and inference modes. The model performs well during training but poorly when deployed in Unity using the ONNX model.

## Root Causes

### 1. **Tracking Camera**
- **Problem**: Using a tracking camera for training and inference
- **Impact**: It can break observations/frames during resets or due to update sequences, causing inference to fail in unusual ways
- **Solution**: Deactivating this camera and enabling one with a fixed position

### 2. **Time Scale Differences**
- **Training**: Often runs faster than real-time (especially with `--no-graphics`)
- **Inference**: Runs at real-time (Time.timeScale = 1.0)
- **Impact**: Physics calculations and timing-dependent behavior differ

### 3. **Observation Normalization Inconsistencies**
- **Problem**: Using episode-specific values (like `maxDistanceToTarget`) for normalization
- **Impact**: Observation distributions differ between training and inference
- **Solution**: Use fixed, consistent normalization values

### 4. **Physics Timestep Dependencies**
- **Problem**: Using `Time.fixedDeltaTime` directly in calculations
- **Impact**: Behavior changes with different time scales
- **Solution**: Use hardcoded fixed timestep (0.02s for 50Hz)

## Fixes Applied

### DroneAgent.cs Changes

1. **Fixed Missing Variable**
   - Added `currentMaxRadius` variable declaration

2. **Consistent Observation Normalization**
   - Changed from dynamic `maxDistanceToTarget` to fixed `fixedMaxDistance = 50f`
   - Ensures observation space remains consistent between training and inference

### SimpleDroneController.cs Changes

1. **Time-Scale Independent Physics**
   - Changed from `Time.fixedDeltaTime` to hardcoded `fixedDeltaTime = 0.02f`
   - Ensures rotation behavior is identical regardless of time scale

## Unity Project Settings for Inference

To ensure consistent behavior during inference, configure these Unity settings:

### 1. **Fixed Timestep** (Edit > Project Settings > Time)
```
Fixed Timestep: 0.02 (50 updates per second)
Maximum Allowed Timestep: 0.1
Time Scale: 1
```

### 2. **Physics Settings** (Edit > Project Settings > Physics)
```
Default Solver Iterations: 6
Default Solver Velocity Iterations: 1
Bounce Threshold: 2
Default Contact Offset: 0.01
```

### 3. **Quality Settings** (Edit > Project Settings > Quality)
- Ensure V-Sync is OFF during inference for consistent timing
- Set target frame rate if needed: `Application.targetFrameRate = 60;`

### 4. **ML-Agents Behavior Parameters**
In the Unity Inspector for your agent GameObject:
- **Behavior Type**: During inference, set to "Inference Only"
- **Model**: Assign your trained .onnx model
- **Inference Device**: Choose "CPU" or "GPU" based on your deployment target
- **Deterministic**: Enable if available (for consistent behavior)

## Training Best Practices

### 1. **Match Training Environment to Deployment**
```bash
# If deploying at 50 FPS, train with similar settings
mlagents-learn hyperparams.yaml \
  --env="Executable/AIr Courier.exe" \
  --run-id=DroneDeliveryRunId \
  --no-graphics \
  --torch-device=cuda \
  --num-envs=8 \
  --time-scale=1  # Keep at 1 for most accurate training
```

### 2. **Validate During Training**
- Periodically test the .onnx model in Unity during training
- Compare behavior in training vs inference modes
- Check that cumulative rewards are similar

### 3. **Hyperparameter Considerations**
The current `hyperparams.yaml` uses:
- `normalize: true` - This is crucial for consistent observations
- `time_horizon: 128` - Should match typical episode lengths
- `batch_size: 1024` - Adequate for stable learning

## Testing Checklist

After applying fixes, verify:

- [ ] Model trains successfully with consistent rewards
- [ ] ONNX export completes without errors
- [ ] Inference mode in Unity shows similar behavior to training
- [ ] Agent successfully reaches targets during inference
- [ ] No compilation errors in Unity
- [ ] Physics behavior is smooth and consistent
- [ ] Performance is acceptable (FPS remains stable)

## Debugging Tips

### If inference still differs from training:

1. **Check Observation Values**
   - Add debug logs to `CollectObservations()` during inference
   - Verify observations are in expected ranges
   - Compare with training observation statistics

2. **Verify Model Loading**
   - Ensure the correct .onnx model is assigned
   - Check Unity console for ML-Agents warnings
   - Verify model version matches ML-Agents package version

3. **Physics Consistency**
   - Verify `Fixed Timestep` in Project Settings matches code (0.02s)
   - Check that Rigidbody settings match between training and inference
   - Ensure no external forces or scripts interfere during inference

4. **Action Space**
   - Verify discrete action branches match the model
   - Check that action mapping in `OnActionReceived()` is correct
   - Confirm no input conflicts with other scripts

5. **Tracking camera**
   - Verify if you have a camera that follows the agent
   - If so, delete it and enable a steady camera.

## Additional Resources

- [ML-Agents Documentation](https://unity-technologies.github.io/ml-agents/)
- [Unity Time and Framerate Management](https://docs.unity3d.com/Manual/TimeFrameManagement.html)
- [Unity Physics Best Practices](https://docs.unity3d.com/Manual/PhysicsBestPractices.html)
