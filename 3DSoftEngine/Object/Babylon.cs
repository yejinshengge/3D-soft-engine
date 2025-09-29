namespace SoftEngine;

public class Babylon
{
    public List<BabyMesh> meshes;
}

public class BabyMesh
{
    public string name;
    
    public List<float> vertices;

    public List<int> indices;

    public List<float> position;

    public int uvCount;
} 