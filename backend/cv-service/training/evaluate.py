"""Evaluate a fine-tuned model against the S2-02 acceptance criteria.

Expects dataset/test/<class_name>/*.jpg|png — one folder per class, holding
photos that were never used in training or validation. Reports per-class
accuracy: the fraction of photos where the model's top detection matches the
folder's class name. Target: >=80% per class, across >=20 photos each.

Usage:
    python evaluate.py --weights runs/detect/train/weights/best.pt
"""
import argparse
from pathlib import Path

from ultralytics import YOLO

TEST_DIR = Path(__file__).parent / "dataset" / "test"
IMAGE_EXTS = {".jpg", ".jpeg", ".png"}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--weights", required=True)
    parser.add_argument("--conf", type=float, default=0.6)
    args = parser.parse_args()

    model = YOLO(args.weights)

    class_dirs = sorted(p for p in TEST_DIR.iterdir() if p.is_dir())
    if not class_dirs:
        raise SystemExit(f"No class folders found under {TEST_DIR}")

    overall_correct = 0
    overall_total = 0

    for class_dir in class_dirs:
        true_label = class_dir.name
        photos = [p for p in class_dir.iterdir() if p.suffix.lower() in IMAGE_EXTS]
        correct = 0

        for photo in photos:
            results = model(str(photo), conf=args.conf, verbose=False)
            boxes = results[0].boxes
            if len(boxes) == 0:
                predicted = None
            else:
                top_box = max(boxes, key=lambda b: float(b.conf))
                predicted = model.names[int(top_box.cls)]

            if predicted == true_label:
                correct += 1

        total = len(photos)
        accuracy = (correct / total * 100) if total else 0.0
        status = "PASS" if accuracy >= 80 else "FAIL"
        note = "" if total >= 20 else "  (fewer than 20 test photos!)"
        print(f"{true_label:20s} {correct:3d}/{total:3d}  {accuracy:5.1f}%  [{status}]{note}")

        overall_correct += correct
        overall_total += total

    if overall_total:
        overall = overall_correct / overall_total * 100
        print(f"\n{'OVERALL':20s} {overall_correct:3d}/{overall_total:3d}  {overall:5.1f}%")


if __name__ == "__main__":
    main()
