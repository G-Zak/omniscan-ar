using System.Collections.Generic;

namespace OmniScan.Api
{
    public class ClassifyResult
    {
        public string machineId;
        public string label;
        public float confidence;
    }

    public class MachineDto
    {
        public string id;
        public string name;
        public bool has3DModel;
    }

    public class ComponentDto
    {
        public string id;
        public string name;
        public string partNumber;
    }

    public class DocumentDto
    {
        public string id;
        public string title;
        public string type;
        public string fileUrl;
    }

    public class Model3DAssetDto
    {
        public string id;
        public string glbUrl;
    }

    /// <summary>Response of GET /machines/{machineId}/subgraph (api-contracts.md section 1).</summary>
    public class SubgraphResponse
    {
        public MachineDto machine;
        public List<ComponentDto> components;
        public List<DocumentDto> documents;
        public Model3DAssetDto model3d;
    }

    /// <summary>The error envelope every endpoint uses (api-contracts.md section 5).</summary>
    public class ErrorDetail
    {
        public string code;
        public string message;
    }

    public class ErrorEnvelope
    {
        public ErrorDetail error;
    }
}
