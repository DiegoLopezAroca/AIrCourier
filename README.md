<img width="1092" height="653" alt="image" src="https://github.com/user-attachments/assets/d44cc014-b0e7-41a3-a989-7e2adefc3a0c" /># AICourier — ML-Agents Installation & Training Guide

This guide provides complete instructions for installing **Unity ML-Agents**, setting up the Python environment, and running training using your `hyperparams.yaml` file.

---

## Prerequisites

Make sure you have the following installed:

- **Conda** (Anaconda or Miniconda)
- **Unity Editor** (2021 LTS or another version compatible with ML-Agents 1.1.0)
- **Git** (optional)

---

## 1. Create Conda Environment

Create a new environment using Python **3.10.12**:

```bash
conda create -n mlagents python=3.10.12
conda activate mlagents
```

## 2. Install ML-Agents
Install **ML-Agents version 1.1.0**:

```bash
python -m pip install mlagents==1.1.0
pip install onnx
```

In case this installation shows dependency problems, try with following guide:
[Installation | ML Agents](https://docs.unity3d.com/Packages/com.unity.ml-agents@4.0/manual/Installation.html)

## 3. Verify Installation
Run:
```bash
mlagents-learn --help
```
## 4. Unity Setup for the Project
1. Open the project in Unity.
2. Install or verify that the **ML-Agents Unity package** is present (Package Manager or manual import).
3. Ensure the scene is ready for training.

## 5. Running Training
Make sure the `hyperparams.yaml` file is inside the project folder.
Start training with (each training should have a different id):
```bash
mlagents-learn hyperparams.yaml --run-id=DroneDeliveryRunId
```
Then press **Play** button inside Unity.
The training results will be stored in (inside each subtraining folder identified with its unique identifier): 
```bash
results/DroneDeliveryRun/
```

To run an ML-Agents training using an executable (Build) instead of the Unity editor:
```bash
mlagents-learn hyperparams.yaml --env="Ejecutable/AIr Courier.exe" --run-id=DroneDeliveryRunId --no-graphics
```

## 6. Project Structure 
```bash
AIrCourier/
├── AIr Courier    # Unity Project
├── README.md      # README.md file of the Github repository
├── hyperpams.yaml # Hyperparameter configuration file for training
└── results        # Training results storage file
```
