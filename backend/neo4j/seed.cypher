CREATE (m1:Machine {
  id: "mch_0192",
  name: "Hydraulic Pump X200",
  manufacturer: "Bosch Rexroth",
  modelNumber: "A10VSO-71-DRG",
  has3DModel: true
})

CREATE (c1:Component {id: "cmp_001", name: "Inlet Valve", partNumber: "IV-200", description: "Controls fluid intake"})
CREATE (c2:Component {id: "cmp_002", name: "Relief Valve", partNumber: "RV-200", description: "Limits system pressure"})
CREATE (c3:Component {id: "cmp_003", name: "Swash Plate", partNumber: "SP-200", description: "Controls displacement"})

CREATE (d1:Document {id: "doc_001", title: "X200 Service Manual", type: "manual", fileUrl: "https://example.com/x200-manual.pdf", pageCount: 120})
CREATE (d2:Document {id: "doc_002", title: "X200 Parts Catalog", type: "catalog", fileUrl: "https://example.com/x200-parts.pdf", pageCount: 45})

CREATE (mdl1:Model3DAsset {id: "mdl_001", glbUrl: "https://example.com/x200.glb", meshNodeId: "Pump_Body", polyCount: 15000})

CREATE (m1)-[:HAS_COMPONENT]->(c1)
CREATE (m1)-[:HAS_COMPONENT]->(c2)
CREATE (m1)-[:HAS_COMPONENT]->(c3)
CREATE (m1)-[:DOCUMENTED_BY]->(d1)
CREATE (m1)-[:DOCUMENTED_BY]->(d2)
CREATE (m1)-[:HAS_3D_MODEL]->(mdl1);

CREATE (m2:Machine {
  id: "mch_0301",
  name: "Industrial Air Compressor GA37",
  manufacturer: "Atlas Copco",
  modelNumber: "GA-37VSD",
  has3DModel: true
})

CREATE (c4:Component {id: "cmp_004", name: "Intake Filter", partNumber: "IF-37", description: "Filters incoming air"})
CREATE (c5:Component {id: "cmp_005", name: "Oil Separator", partNumber: "OS-37", description: "Separates oil from compressed air"})

CREATE (d3:Document {id: "doc_003", title: "GA37 Operation Manual", type: "manual", fileUrl: "https://example.com/ga37-manual.pdf", pageCount: 95})

CREATE (mdl2:Model3DAsset {id: "mdl_002", glbUrl: "https://example.com/ga37.glb", meshNodeId: "Compressor_Tank", polyCount: 22000})

CREATE (m2)-[:HAS_COMPONENT]->(c4)
CREATE (m2)-[:HAS_COMPONENT]->(c5)
CREATE (m2)-[:DOCUMENTED_BY]->(d3)
CREATE (m2)-[:HAS_3D_MODEL]->(mdl2);