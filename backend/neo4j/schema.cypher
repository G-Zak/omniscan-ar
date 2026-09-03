CREATE CONSTRAINT machine_id_unique IF NOT EXISTS
  FOR (m:Machine) REQUIRE m.id IS UNIQUE;

CREATE CONSTRAINT component_id_unique IF NOT EXISTS
  FOR (c:Component) REQUIRE c.id IS UNIQUE;

CREATE CONSTRAINT document_id_unique IF NOT EXISTS
  FOR (d:Document) REQUIRE d.id IS UNIQUE;

CREATE CONSTRAINT model3d_id_unique IF NOT EXISTS
  FOR (mdl:Model3DAsset) REQUIRE mdl.id IS UNIQUE;

CREATE CONSTRAINT cvlabel_id_unique IF NOT EXISTS
  FOR (cv:CVLabel) REQUIRE cv.id IS UNIQUE;