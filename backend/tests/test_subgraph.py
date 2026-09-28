from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_subgraph_matches_contract_shape_for_seeded_machine():
    response = client.get("/api/v1/machines/mch_0192/subgraph")

    assert response.status_code == 200
    body = response.json()

    assert body["machine"]["id"] == "mch_0192"
    assert body["machine"]["name"] == "Hydraulic Pump X200"
    assert body["machine"]["has3DModel"] is True

    component_ids = {c["id"] for c in body["components"]}
    assert component_ids == {"cmp_001", "cmp_002", "cmp_003"}

    document_ids = {d["id"] for d in body["documents"]}
    assert document_ids == {"doc_001", "doc_002"}

    assert body["model3d"]["id"] == "mdl_001"
    assert body["model3d"]["glbUrl"] == "https://example.com/x200.glb"


def test_subgraph_returns_404_envelope_for_unknown_machine():
    response = client.get("/api/v1/machines/does-not-exist/subgraph")

    assert response.status_code == 404
    assert response.json() == {
        "error": {
            "code": "MACHINE_NOT_FOUND",
            "message": "No machine with id does-not-exist",
        }
    }
