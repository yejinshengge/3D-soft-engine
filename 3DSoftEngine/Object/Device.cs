using System.Windows;
using System.Windows.Media.Imaging;
using SharpDX;

namespace SoftEngine;

public class Device
{
    // 后备缓冲区
    private byte[] backBuffer;
    // 步长 每行像素所占字节数
    private int backBufferStride;
    private WriteableBitmap bmp;

    public Device(WriteableBitmap bmp)
    {
        this.bmp = bmp;
        backBufferStride = bmp.PixelWidth * 4;
        // 长*宽*4(RGBA)
        backBuffer = new byte[backBufferStride * bmp.PixelHeight];
    }

    /// <summary>
    /// 将缓冲区置为同一颜色
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="a"></param>
    public void Clear(byte r, byte g, byte b, byte a)
    {
        for (int i = 0; i < backBuffer.Length; i += 4)
        {
            // Windows 使用 BGRA 格式
            backBuffer[i] = b;
            backBuffer[i + 1] = g;
            backBuffer[i + 2] = r;
            backBuffer[i + 3] = a;
        }
    }
    
    /// <summary>
    /// 将后台缓冲区刷新到前台缓冲区
    /// </summary>
    public void Present()
    {
        // 使用 WritePixels 将我们的 byte[] 复制到 WriteableBitmap 中
        // 参数：要更新的区域、源数组、源数组的步长、目标位置的偏移量
        bmp.WritePixels(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight), 
            backBuffer, 
            backBufferStride, 
            0);
            
        // 在 WPF 中，WriteableBitmap 会自动失效并通知 UI 线程进行重绘。
        // 不需要像 UWP 那样调用 bmp.Invalidate()。
    }

    /// <summary>
    /// 设置指定像素的颜色
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="color"></param>
    public void PutPixel(int x, int y, Color4 color)
    {
        int index = (x + y * bmp.PixelWidth) * 4;
        backBuffer[index] = (byte)(color.Blue * 255);
        backBuffer[index + 1] = (byte)(color.Green * 255);
        backBuffer[index + 2] = (byte)(color.Red * 255);
        backBuffer[index + 3] = (byte)(color.Alpha * 255);
    }
    
    // 将3D坐标转为2D屏幕坐标
    public Vector2 Project(Vector3 coord, Matrix transMat)
    {
        // 矩阵变换
        var point = Vector3.TransformCoordinate(coord, transMat);
        // 变换后的结果位于标准化空间,x,y取值范围[-1,1]
        // 映射到屏幕空间,(0,0)为屏幕左上角
        var x = point.X * bmp.PixelWidth + bmp.PixelWidth / 2.0f;
        // 取反是因为标准化空间中y轴正方向向上,而屏幕坐标中y轴正方向向下
        var y = -point.Y * bmp.PixelHeight + bmp.PixelHeight / 2.0f;
        return new Vector2(x, y);
    }

    /// <summary>
    /// 置指定像素颜色 考虑裁剪设
    /// </summary>
    /// <param name="point"></param>
    public void DrawPoint(Vector2 point)
    {
        if (point.X >= 0 && point.Y >= 0 && point.X < bmp.PixelWidth && point.Y < bmp.PixelHeight)
            // TODO:颜色先写死
            PutPixel((int)point.X, (int)point.Y, new Color4(1, 1, 0, 1));
    }

    /// <summary>
    /// 画线
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    public void DrawLine(Vector2 p0, Vector2 p1)
    {
        var dis = (p1 - p0).Length();
        // 小于两像素 停止画线
        if(dis < 2) return;
        // 中间点
        var midP = p0 + (p1 - p0) / 2;
        DrawPoint(midP);
        // 画两边
        DrawLine(p0,midP);
        DrawLine(midP,p1);
    }
    
    /// <summary>
    /// Bresenham 算法画直线
    /// </summary>
    /// <param name="point0"></param>
    /// <param name="point1"></param>
    public void DrawBLine(Vector2 point0, Vector2 point1)
    {
        int x0 = (int)point0.X;
        int y0 = (int)point0.Y;
        int x1 = (int)point1.X;
        int y1 = (int)point1.Y;
            
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = (x0 < x1) ? 1 : -1;
        var sy = (y0 < y1) ? 1 : -1;
        var err = dx - dy;

        while (true) {
            DrawPoint(new Vector2(x0, y0));

            if ((x0 == x1) && (y0 == y1)) break;
            var e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    public void Render(Camera camera,params Mesh[] meshes)
    {
        // 创建相机坐标系转换矩阵(左手坐标系)
        var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
        // 创建投影矩阵
        var projectionMatrix = Matrix.PerspectiveFovLH(0.78f, (float)bmp.PixelWidth / bmp.PixelHeight, 0.01f, 1.0f);
        
        foreach (var mesh in meshes)
        {
            // 创建世界坐标变换矩阵
            var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, 
                                  mesh.Rotation.X, mesh.Rotation.Z) * Matrix.Translation(mesh.Position);
            var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;
            
            foreach(var face in mesh.Faces)
            {
                var verA = mesh.Vertices[face.A];
                var verB = mesh.Vertices[face.B];
                var verC = mesh.Vertices[face.C];
                // 转换到2d屏幕空间
                var point1 = Project(verA, transformMatrix);
                var point2 = Project(verB, transformMatrix);
                var point3 = Project(verC, transformMatrix);
                // 绘制到屏幕上
                DrawBLine(point1,point2);
                DrawBLine(point2,point3);
                DrawBLine(point3,point1);
            }
        }
    }
}