from typing import Optional

from neo4j import Driver

from app.schemas import Component, Document, Machine, Model3DAsset


class MachineRepository:
    """Wraps the Cypher queries behind typed methods"""

    def __init__(self, driver: Driver):
        self.driver = driver

    def get_machine(self, machine_id: str) -> Optional[Machine]:
        query = "MATCH (m:Machine {id: $id}) RETURN m"
        with self.driver.session() as session:
            record = session.run(query, id=machine_id).single()
        return Machine(**dict(record["m"])) if record else None

    def list_machines(self) -> list[Machine]:
        query = "MATCH (m:Machine) RETURN m ORDER BY m.name"
        with self.driver.session() as session:
            records = list(session.run(query))
        return [Machine(**dict(record["m"])) for record in records]

    def get_components(self, machine_id: str) -> list[Component]:
        query = """
        MATCH (:Machine {id: $id})-[:HAS_COMPONENT]->(c:Component)
        RETURN c
        """
        with self.driver.session() as session:
            records = list(session.run(query, id=machine_id))
        return [Component(**dict(record["c"])) for record in records]

    def get_documents(self, machine_id: str) -> list[Document]:
        query = """
        MATCH (m:Machine {id: $id})
        OPTIONAL MATCH (m)-[:DOCUMENTED_BY]->(mdoc:Document)
        OPTIONAL MATCH (m)-[:HAS_COMPONENT]->(:Component)-[:DOCUMENTED_BY]->(cdoc:Document)
        WITH collect(DISTINCT mdoc) + collect(DISTINCT cdoc) AS docs
        UNWIND docs AS doc
        RETURN DISTINCT doc
        """
        with self.driver.session() as session:
            records = list(session.run(query, id=machine_id))
        return [Document(**dict(record["doc"])) for record in records if record["doc"] is not None]

    def get_model3d(self, machine_id: str) -> Optional[Model3DAsset]:
        query = """
        MATCH (:Machine {id: $id})-[:HAS_3D_MODEL]->(model:Model3DAsset)
        RETURN model
        """
        with self.driver.session() as session:
            record = session.run(query, id=machine_id).single()
        return Model3DAsset(**dict(record["model"])) if record else None
