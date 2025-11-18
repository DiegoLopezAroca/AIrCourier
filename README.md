# MOMO SHOW
conda create -n mlagents python=3.10.12 && conda activate mlagents
python -m pip install mlagents==1.1.0

RUN: 
mlagents-learn hyperpams.yaml --run-id=DroneDeliveryRun4