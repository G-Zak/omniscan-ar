import os
import pathlib

import pytest
from neo4j import GraphDatabase

NEO4J_URI = os.getenv("NEO4J_URI", "bolt://localhost:7687")
NEO4J_USER = os.getenv("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.getenv("NEO4J_PASSWORD", "password")

SEED_FILE = pathlib.Path(__file__).resolve().parents[1] / "neo4j" / "seed.cypher"


@pytest.fixture(scope="session", autouse=True)
def seeded_database():
    """Wipes the test Neo4j instance and reloads the Sprint 1 seed dataset."""
    driver = GraphDatabase.driver(NEO4J_URI, auth=(NEO4J_USER, NEO4J_PASSWORD))
    statements = [s.strip() for s in SEED_FILE.read_text().split(";") if s.strip()]

    with driver.session() as session:
        session.run("MATCH (n) DETACH DELETE n")
        for statement in statements:
            session.run(statement)

    yield

    driver.close()
