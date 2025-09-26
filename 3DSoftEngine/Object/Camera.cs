using SharpDX;

namespace SoftEngine;

public class Camera
{
    /// <summary>
    /// 世界坐标
    /// </summary>
    public Vector3 Position { get; set; }
    
    /// <summary>
    /// 目标方向
    /// </summary>
    public Vector3 Target { get; set; }
}