# YOLOv8 inference wrapper for object detection
# Handles model loading, image preprocessing, and detection logic

import cv2
import numpy as np
from ultralytics import YOLO


class ObjectDetector:
    # Wraps the YOLOv8 machine learning model to detect objects in images
    # Loads the model once at startup, then runs predictions on uploaded images
    # Returns the highest-confidence detection or None if below threshold

    def __init__(self, model_name: str = "yolov8n.pt"):
        # Load YOLOv8 model from disk.
        self.model = YOLO(model_name)
        self.class_names = self.model.names
    
    def decode_image_bytes(self, image_bytes: bytes):
        # Convert raw image file bytes to OpenCV format (numpy array)
        # Handles JPEG, PNG, and other image formats.
        image_as_array = np.frombuffer(image_bytes, np.uint8)
        image_opencv = cv2.imdecode(image_as_array, cv2.IMREAD_COLOR)
        return image_opencv
    
    def predict(self, image, confidence_threshold: float):
        # Run YOLOv8 inference on the image
        # Returns dict with label and confidence if detection found above threshold
        inference_results = self.model(image, conf=confidence_threshold, verbose=False)
        detected_boxes = inference_results[0].boxes
        
        # No detections above threshold
        if len(detected_boxes) == 0:
            return None
        
        # Extract highest confidence detection
        top_detection = detected_boxes[0]
        class_index = int(top_detection.cls)
        class_name = self.class_names[class_index]
        confidence_score = float(top_detection.conf)

        # If detection found: `{"label": "generator", "confidence": 0.87}`
        return {
            "label": class_name,
            "confidence": confidence_score
        }