using SharpDX;

namespace SoftEngine;

public class Mesh
{
    public string Name { get; set; }
    
    /// <summary>
    /// 顶点集合
    /// </summary>
    public Vector3[] Vertices { get; set; }
    
    public Vector3 Position { get; set; }
    
    public Vector3 Rotation { get; set; }

    public Mesh(string name, int verticesCnt)
    {
        Name = name;
        Vertices = new Vector3[verticesCnt];
    }
}