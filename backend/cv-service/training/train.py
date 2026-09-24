"""Fine-tune YOLOv8 on the demo-machine dataset (S2-02).

Starts from the same pretrained yolov8n.pt weights already used by the
cv-service (see ../inference.py) and fine-tunes on dataset/ (see data.yaml).
Output weights land at runs/detect/train/weights/best.pt — copy that file
into ../ and point the service's YOLOV8_MODEL env var at it to serve it.

Usage:
    python train.py [--epochs 30] [--imgsz 640] [--device cpu]
"""
import argparse
from pathlib import Path

from ultralytics import YOLO

BASE_WEIGHTS = Path(__file__).parent.parent / "yolov8n.pt"
DATA_YAML = Path(__file__).parent / "data.yaml"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--epochs", type=int, default=30)
    parser.add_argument("--imgsz", type=int, default=640)
    parser.add_argument("--device", default="cpu")
    args = parser.parse_args()

    model = YOLO(str(BASE_WEIGHTS))
    model.train(
        data=str(DATA_YAML),
        epochs=args.epochs,
        imgsz=args.imgsz,
        device=args.device,
    )


if __name__ == "__main__":
    main()
