import os
from contextlib import asynccontextmanager

import httpx
from fastapi import FastAPI, File, HTTPException, UploadFile
from fastapi.responses import JSONResponse

from app.neo4j_client import Neo4jClient
from app.repository import MachineRepository
from app.schemas import ClassifyResult, SubgraphResponse

NEO4J_URI = os.getenv("NEO4J_URI", "bolt://localhost:7687")
NEO4J_USER = os.getenv("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.getenv("NEO4J_PASSWORD", "password")
CV_SERVICE_URL = os.getenv("CV_SERVICE_URL", "http://localhost:8002")

neo4j_client = Neo4jClient(NEO4J_URI, NEO4J_USER, NEO4J_PASSWORD)
machine_repository = MachineRepository(neo4j_client.driver)

# TODO: load from Neo4j or config file; for now, hardcoded seed data labels
LABEL_TO_MACHINE_ID = {
    "generator": "MAT-001",
    "compressor": "MAT-002",
}


@asynccontextmanager
async def lifespan(app: FastAPI):
    yield
    neo4j_client.close()


app = FastAPI(title="OmniScan AR Backend", lifespan=lifespan)


@app.get("/health")
def health():
    neo4j_status = "up" if neo4j_client.is_reachable() else "down"
    return {"status": "ok", "neo4j": neo4j_status}


@app.post("/api/v1/cv/classify", response_model=ClassifyResult)
async def classify(image: UploadFile = File(...)):
    """Classify an image by forwarding to the CV service and mapping label to machineId."""
    try:
        # Read image bytes and forward to CV service
        image_bytes = await image.read()
        async with httpx.AsyncClient() as client:
            files = {"file": ("frame.jpg", image_bytes, "image/jpeg")}
            response = await client.post(f"{CV_SERVICE_URL}/classify", files=files, timeout=10)
            response.raise_for_status()
            cv_result = response.json()
    except httpx.HTTPError as e:
        # CV service returned an error (e.g., 404 no detection, 500 error)
        if isinstance(e, httpx.HTTPStatusError) and e.response.status_code == 404:
            return JSONResponse(
                status_code=404,
                content={
                    "error": {
                        "code": "NO_DETECTION",
                        "message": "No object detected with sufficient confidence",
                    }
                },
            )
        raise HTTPException(status_code=503, detail="CV service unavailable")
    except Exception as e:
        raise HTTPException(status_code=503, detail="CV service unavailable")

    # Map label to machineId
    label = cv_result.get("label", "").lower()
    machine_id = LABEL_TO_MACHINE_ID.get(label)
    if not machine_id:
        return JSONResponse(
            status_code=404,
            content={
                "error": {
                    "code": "UNKNOWN_LABEL",
                    "message": f"'{label}' is not a known machine type",
                }
            },
        )

    return ClassifyResult(
        machineId=machine_id,
        label=cv_result.get("label"),
        confidence=cv_result.get("confidence"),
    )


@app.get("/api/v1/machines/{machine_id}/subgraph", response_model=SubgraphResponse)
def get_machine_subgraph(machine_id: str):
    subgraph = machine_repository.get_subgraph(machine_id)
    if subgraph is None:
        return JSONResponse(
            status_code=404,
            content={
                "error": {
                    "code": "MACHINE_NOT_FOUND",
                    "message": f"No machine with id {machine_id}",
                }
            },
        )
    return subgraph
