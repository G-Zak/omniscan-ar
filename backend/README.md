# Backend

The backend is the central API and orchestration service for OmniScan AR — it handles authentication, persists scan/session data, and coordinates communication between the `cv-service`, `dashboard`, and `unity-client` components.

## Running the full stack

From the project root, run:

```bash
docker compose up
```

This starts:
- **Neo4j** at `http://localhost:7474` (username: `neo4j`, password: `password`)
- **API** at `http://localhost:8080` (placeholder in Sprint 1, Spring Boot to follow)
- **CV Service** at `http://localhost:5000` (placeholder in Sprint 1, Python service to follow)

Data persists in `neo4j-data/` volume across restarts.

## Building the backend service

When implementation begins, this section will document Spring Boot setup and build commands (see `target/` in `.gitignore`).
