from datetime import datetime
from typing import Optional

from pydantic import BaseModel


class Machine(BaseModel):
    id: str
    name: str
    manufacturer: str
    modelNumber: str
    has3DModel: bool
    createdAt: Optional[datetime] = None


class Component(BaseModel):
    id: str
    name: str
    partNumber: str
    description: Optional[str] = None


class Document(BaseModel):
    id: str
    title: str
    type: str
    fileUrl: str
    pageCount: Optional[int] = None


class Model3DAsset(BaseModel):
    id: str
    glbUrl: str
    meshNodeId: str
    polyCount: Optional[int] = None


class SubgraphResponse(BaseModel):
    machine: Machine
    components: list[Component]
    documents: list[Document]
    model3d: Optional[Model3DAsset] = None


class ErrorDetail(BaseModel):
    code: str
    message: str


class ClassifyResult(BaseModel):
    machineId: str
    label: str
    confidence: float


class ErrorResponse(BaseModel):
    error: ErrorDetail
