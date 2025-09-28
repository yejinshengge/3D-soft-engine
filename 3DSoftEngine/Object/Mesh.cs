using SharpDX;

namespace SoftEngine;

/// <summary>
/// 三角面对应的三个顶点
/// </summary>
public class Face
{
    public int A;
    public int B;
    public int C;
}

public class Mesh
{
    public string Name { get; set; }
    
    /// <summary>
    /// 顶点集合
    /// </summary>
    public Vector3[] Vertices { get; set; }
    
    /// <summary>
    /// 三角面集合
    /// </summary>
    public Face[] Faces { get; set; }
    
    public Vector3 Position { get; set; }
    
    public Vector3 Rotation { get; set; }

    public Mesh(string name, int verticesCnt, int faceCnt)
    {
        Name = name;
        Vertices = new Vector3[verticesCnt];
        Faces = new Face[faceCnt];
    }
}