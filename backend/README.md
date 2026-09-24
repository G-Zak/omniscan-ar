# Backend

The backend is the central API and orchestration service for OmniScan AR — it's a FastAPI app that handles authentication, persists scan/session data, and coordinates communication between the `cv-service`, `dashboard`, and `unity-client` components. It talks to Neo4j via the official `neo4j` Python driver.

## Running the full stack

From the project root, run:

```bash
docker compose up
```

This starts:
- **Neo4j** at `http://localhost:7474` (username: `neo4j`, password: `password`)
- **API** at `http://localhost:8000`
- **CV Service** at `http://localhost:8002`

Data persists in `neo4j-data/` volume across restarts.

## Run it

With Docker (recommended, matches CI):

    docker compose up --build backend
    curl http://localhost:8000/health

Locally without Docker (requires Python 3.11+, and Neo4j running separately):

    cd backend
    pip install -r requirements.txt
    uvicorn app.main:app --reload

The app reads `NEO4J_URI`, `NEO4J_USER`, and `NEO4J_PASSWORD` from the environment, defaulting to `bolt://localhost:7687` / `neo4j` / `password` when unset.