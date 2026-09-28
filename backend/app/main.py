import os
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.responses import JSONResponse

from app.neo4j_client import Neo4jClient
from app.repository import MachineRepository
from app.schemas import SubgraphResponse

NEO4J_URI = os.getenv("NEO4J_URI", "bolt://localhost:7687")
NEO4J_USER = os.getenv("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.getenv("NEO4J_PASSWORD", "password")

neo4j_client = Neo4jClient(NEO4J_URI, NEO4J_USER, NEO4J_PASSWORD)
machine_repository = MachineRepository(neo4j_client.driver)


@asynccontextmanager
async def lifespan(app: FastAPI):
    yield
    neo4j_client.close()


app = FastAPI(title="OmniScan AR Backend", lifespan=lifespan)


@app.get("/health")
def health():
    neo4j_status = "up" if neo4j_client.is_reachable() else "down"
    return {"status": "ok", "neo4j": neo4j_status}


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
