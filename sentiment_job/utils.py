# utils.py
import numpy as np

def amplify(x: float, k: float = 6.0) -> float:
    return float(1.0 / (1.0 + np.exp(-k * (x - 0.5))))
