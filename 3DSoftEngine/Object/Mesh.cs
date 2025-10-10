using SharpDX;

namespace SoftEngine;

/// <summary>
/// 三角面
/// </summary>
public struct Face
{
    public int A;
    public int B;
    public int C;
    /// <summary>
    /// 表面法线
    /// </summary>
    public Vector3 Normal;
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

    /// <summary>
    /// 计算表面法线（几何法线，用于背面剔除）
    /// </summary>
    public void ComputeFacesNormal()
    {
        Parallel.For(0, Faces.Length, faceIndex =>
        {
            var face = Faces[faceIndex];
            var vertexA = Vertices[face.A];
            var vertexB = Vertices[face.B];
            var vertexC = Vertices[face.C];

            // 使用叉积计算几何法线
            // edge1 = B - A
            // edge2 = C - A
            // 在左手坐标系中，根据顶点绕序，使用 edge2 × edge1 来得到正确的法线方向
            var edge1 = vertexB.Coordinates - vertexA.Coordinates;
            var edge2 = vertexC.Coordinates - vertexA.Coordinates;
            
            // 注意：叉积顺序决定法线方向
            var normal = Vector3.Cross(edge2, edge1);
            normal.Normalize();
            
            Faces[faceIndex].Normal = normal;
        });
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