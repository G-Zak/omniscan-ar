# Demo-machine fine-tuning (S2-02)

Fine-tunes `yolov8n.pt` on real photos of the project's demo objects, because
stock COCO classes don't reliably map onto them — see the full writeup in
[`label-mapping-demo-machines.md`](../../../documentation/concepts/cv-service/label-mapping-demo-machines.md)
for the empirical test that ruled out class-mapping.

## 1. Add your photos

Drop phone-camera photos of each physical demo object into:

```
dataset/images/train/   # ~30 photos per class, varied angle/light/background
dataset/images/val/     # ~10 photos per class
dataset/test/<class_name>/   # 20 held-out photos per class, NEVER used in training
```

`dataset/test/` is organized by class folder (`dataset/test/diesel_engine/`,
etc.) since `evaluate.py` reads ground truth from the folder name.
`images/train` and `images/val` are flat — Ultralytics matches each image to
its label file by filename.

Current class names (edit `data.yaml` if your objects differ):
`diesel_engine`, `drilling_machine`, `industrial_pump`.

## 2. Annotate

Use [Roboflow](https://roboflow.com) (browser-based, free tier) or
[LabelImg](https://github.com/HumanSignal/labelImg) (offline) to draw one
bounding box per training/val photo and export in **Ultralytics YOLO
format** straight into `dataset/labels/train/` and `dataset/labels/val/`
(one `.txt` per image, same filename). Test-set photos don't need
annotation — `evaluate.py` only checks the predicted label, not a box.

## 3. Train

```
python train.py --epochs 30
```

Weights land at `runs/detect/train/weights/best.pt`.

## 4. Evaluate against the acceptance criteria

```
python evaluate.py --weights runs/detect/train/weights/best.pt
```

Target: **>80% accuracy per class, across the 20 held-out test photos**.
If a class falls short, add more/varied training photos for it and re-train
before increasing epochs blindly.

## 5. Ship it

Copy `best.pt` into `../` (i.e. `cv-service/best.pt`) and point the service
at it:

```
YOLOV8_MODEL=best.pt
```

(in `docker-compose.yml` or your local env) — no code change needed in
`main.py`/`inference.py`.

Then add matching `Machine` + `CVLabel` nodes to `backend/neo4j/seed.cypher`,
`yoloClassId` matching `data.yaml`'s class order (0/1/2 above), and write the
trade-off note in `cv-service/README.md`.
