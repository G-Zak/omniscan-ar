import os
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.neo4j_client import Neo4jClient

NEO4J_URI = os.getenv("NEO4J_URI", "bolt://localhost:7687")
NEO4J_USER = os.getenv("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.getenv("NEO4J_PASSWORD", "password")

neo4j_client = Neo4jClient(NEO4J_URI, NEO4J_USER, NEO4J_PASSWORD)


@asynccontextmanager
async def lifespan(app: FastAPI):
    yield
    neo4j_client.close()


app = FastAPI(title="OmniScan AR Backend", lifespan=lifespan)


@app.get("/health")
def health():
    neo4j_status = "up" if neo4j_client.is_reachable() else "down"
    return {"status": "ok", "neo4j": neo4j_status}
