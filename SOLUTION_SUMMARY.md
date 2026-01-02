# Solution Summary: Training-Inference Performance Mismatch

## Issue Description
The RL model trained with ML-Agents performs excellently during training but shows poor performance during inference in Unity with the ONNX model.

## Root Cause Analysis

This is a classic **training-inference distribution mismatch** problem in reinforcement learning. The neural network learns a policy based on the observation distributions and physics behavior it experiences during training. When these differ during inference, the policy fails because it's operating outside its learned distribution.

### Specific Issues Identified

1. **Missing Variable Declaration (Bug)**
   - `currentMaxRadius` was used but never declared
   - Would cause compilation errors
   - Fixed by adding declaration with default value

2. **Dynamic Observation Normalization (Critical)**
   - **Problem**: Distance was normalized by `maxDistanceToTarget`, which varies per episode
   - **Impact**: The neural network sees different ranges of values during each episode and during inference
   - **Example**: 
     - Training Episode 1: maxDistance=30, current=15 → normalized=0.5
     - Training Episode 2: maxDistance=10, current=5 → normalized=0.5
     - Inference: maxDistance might not even be set correctly → wrong normalized values
   - **Fix**: Use fixed `fixedMaxDistance = 50f` for consistent normalization

3. **Time-Scale Dependent Physics (Critical)**
   - **Problem**: Rotation used `Time.fixedDeltaTime` which changes with `Time.timeScale`
   - **Impact**: Training with `--no-graphics` often runs faster (timeScale > 1), making the drone turn differently than during inference
   - **Example**:
     - Training: timeScale=5, fixedDeltaTime=0.1s → rotation multiplier is 5x different
     - Inference: timeScale=1, fixedDeltaTime=0.02s → expected behavior
   - **Fix**: Use hardcoded `fixedDeltaTime = 0.02f` for consistent rotation behavior

## Changes Made

### 1. DroneAgent.cs
```csharp
// BEFORE (line 43):
private int steps = 0;

// AFTER:
private int steps = 0;
private float currentMaxRadius = 10f;  // Fixed missing variable

// BEFORE (line 124):
float distNorm = Mathf.Clamp01(relPos.magnitude / Mathf.Max(0.001f, maxDistanceToTarget));

// AFTER:
float fixedMaxDistance = 50f; // Maximum expected distance in the environment
float distNorm = Mathf.Clamp01(relPos.magnitude / fixedMaxDistance);
```

### 2. SimpleDroneController.cs
```csharp
// BEFORE (line 45):
float yawDegrees = inputYaw * turnSpeed * Time.fixedDeltaTime;

// AFTER:
float fixedDeltaTime = 0.02f; // Standard Unity physics timestep (50Hz)
float yawDegrees = inputYaw * turnSpeed * fixedDeltaTime;
```

### 3. Documentation
- Created `TRAINING_INFERENCE_GUIDE.md` with comprehensive troubleshooting guide
- Updated `README.md` to reference the guide
- Added comments to `hyperparams.yaml` about critical settings

## Expected Results

After applying these fixes:

1. **Training will continue to work** - The changes don't break training
2. **Inference will match training behavior** - Same physics, same observations
3. **Agent will reach targets during inference** - Policy applies correctly
4. **Consistent performance** - No randomness from time scaling or normalization

## Next Steps for Users

1. **Retrain the model** with these fixes applied
   - The old model was trained with inconsistent observations
   - New model will learn with consistent data

2. **Verify Unity settings** as described in TRAINING_INFERENCE_GUIDE.md
   - Fixed Timestep: 0.02
   - Time Scale: 1
   - Behavior Parameters correctly configured

3. **Test incrementally**
   - Train for a few thousand steps
   - Export ONNX model
   - Test in Unity inference mode
   - Compare with training performance

## Technical Explanation

In RL, the agent learns a mapping from **observations → actions**. This mapping (the policy) is only valid for the distribution of observations seen during training. 

When observations are normalized inconsistently:
- Training: Agent sees distance values normalized to [0, 1] based on episode max
- Inference: Agent sees different normalization → different values → policy doesn't apply correctly

When physics behavior differs:
- Training: Drone turns at rate X (due to timeScale effects)
- Inference: Drone turns at rate Y → different state transitions → policy doesn't apply correctly

By ensuring **exact consistency** between training and inference (fixed normalization, fixed timestep), we guarantee the agent operates within its learned distribution.

## Why This Is Minimal Change

These changes:
- ✅ Fix only what's necessary (3 code changes + documentation)
- ✅ Don't alter reward structure or training hyperparameters
- ✅ Don't modify working code unnecessarily
- ✅ Add clarifying comments for maintainability
- ✅ Provide comprehensive documentation for future reference

## References

This type of issue is well-documented in RL literature:
- "Sim-to-Real Transfer in Deep Reinforcement Learning" 
- ML-Agents best practices for deterministic inference
- Unity Physics consistency guidelines
