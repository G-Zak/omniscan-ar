from fastapi import FastAPI, File, UploadFile, HTTPException
from pydantic import BaseModel
import os
from inference import ObjectDetector

# Load settings from environment or use defaults
YOLO_MODEL_NAME = os.getenv("YOLOV8_MODEL", "yolov8n.pt")
CONFIDENCE_THRESHOLD = float(os.getenv("CONFIDENCE_THRESHOLD", 0.6))

# DATA MODELS (the shape of responses)
class DetectionResult(BaseModel):
    label: str  
    confidence: float 


# INITIALIZE DETECTOR
print("Loading YOLOv8 model...")
detector = ObjectDetector(YOLO_MODEL_NAME)
print(f"Model loaded: {YOLO_MODEL_NAME}")


# FASTAPI APP & ROUTES
app = FastAPI(
    title="YOLOv8 Classification Service",
    description="Detects objects in uploaded images using YOLOv8"
)


@app.post("/classify", response_model=DetectionResult)
async def classify_image(file: UploadFile = File(...)):
    # Read the uploaded image bytes
    image_bytes = await file.read()
    
    # Decode bytes into OpenCV format
    image = detector.decode_image_bytes(image_bytes)
    
    # Run inference using detector
    detection = detector.predict(image, CONFIDENCE_THRESHOLD)
    
    # If nothing found above threshold, return 404
    if detection is None:
        raise HTTPException(
            status_code=404,
            detail=f"No object detected with confidence >= {CONFIDENCE_THRESHOLD}"
        )
    
    # Return the result
    return DetectionResult(
        label=detection["label"],
        confidence=detection["confidence"]
    )


# STARTUP
@app.get("/health")
async def health_check():
    return {"status": "ok"}


if __name__ == "__main__":
    print("To start the server, run:")
    print("  python -m uvicorn main:app --reload")