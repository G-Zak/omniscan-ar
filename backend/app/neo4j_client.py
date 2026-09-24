from neo4j import GraphDatabase
from neo4j.exceptions import Neo4jError, ServiceUnavailable


class Neo4jClient:
    def __init__(self, uri: str, user: str, password: str):
        self.driver = GraphDatabase.driver(uri, auth=(user, password))

    def close(self):
        self.driver.close()

    def is_reachable(self) -> bool:
        try:
            self.driver.verify_connectivity()
            return True
        except (ServiceUnavailable, Neo4jError):
            return False
