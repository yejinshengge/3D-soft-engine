using SharpDX;

namespace SoftEngine;

/// <summary>
/// 三角面对应的三个顶点
/// </summary>
public struct Face
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
    public Vertex[] Vertices { get; set; }
    
    /// <summary>
    /// 三角面集合
    /// </summary>
    public Face[] Faces { get; set; }
    
    /// <summary>
    /// 贴图
    /// </summary>
    public Texture Texture { get; set; }
    
    public Vector3 Position { get; set; }
    
    public Vector3 Rotation { get; set; }

    public Mesh(string name, int verticesCnt, int faceCnt)
    {
        Name = name;
        Vertices = new Vertex[verticesCnt];
        Faces = new Face[faceCnt];
    }
}

/// <summary>
/// 顶点
/// </summary>
public struct Vertex
{
    /// <summary>
    /// 当前坐标
    /// </summary>
    public Vector3 Coordinates;
    /// <summary>
    /// 世界坐标
    /// </summary>
    public Vector3 WorldCoordinates;
    /// <summary>
    /// 法线
    /// </summary>
    public Vector3 Normal;
    /// <summary>
    /// 纹理坐标
    /// </summary>
    public Vector2 TextureCoordinates;

}