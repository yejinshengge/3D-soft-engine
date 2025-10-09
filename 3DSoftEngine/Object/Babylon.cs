namespace SoftEngine;

public class Babylon
{
    public List<BabyMesh> meshes;

    public List<Material> materials;
}

public class BabyMesh
{
    public string name;
    
    public List<float> vertices;

    public List<int> indices;

    public List<float> position;

    public string materialId;

    public int uvCount;
} 

public class Material
{
    public string name;
    public string id;
    public DiffuseTexture diffuseTexture;
}

public class DiffuseTexture
{
    public string name;
}