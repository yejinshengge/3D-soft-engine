using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using SharpDX;

namespace SoftEngine;

public class Device
{
    // 后备缓冲区
    private byte[] backBuffer;
    // 深度缓冲区
    private float[] depthBuffer;
    // 步长 每行像素所占字节数
    private int backBufferStride;
    
    private WriteableBitmap bmp;

    public Device(WriteableBitmap bmp)
    {
        this.bmp = bmp;
        backBufferStride = bmp.PixelWidth * 4;
        // 长*宽*4(RGBA)
        backBuffer = new byte[backBufferStride * bmp.PixelHeight];
        depthBuffer = new float[bmp.PixelWidth* bmp.PixelHeight];
    }

    /// <summary>
    /// 重置缓冲区
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="a"></param>
    public void Clear(byte r, byte g, byte b, byte a)
    {
        // 重置后备缓冲区
        for (int i = 0; i < backBuffer.Length; i += 4)
        {
            // Windows 使用 BGRA 格式
            backBuffer[i] = b;
            backBuffer[i + 1] = g;
            backBuffer[i + 2] = r;
            backBuffer[i + 3] = a;
        }
        // 重置深度缓冲区
        for (int i = 0; i < depthBuffer.Length; i++)
        {
            depthBuffer[i] = float.MaxValue;
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
    /// <param name="z"></param>
    /// <param name="color"></param>
    public void PutPixel(int x, int y,float z, Color4 color)
    {
        int index = x + y * bmp.PixelWidth;
        int index4 = index * 4;
        // 深度大于缓存值,不渲染
        if(z > depthBuffer[index]) return;
        // 设置后备缓冲区
        backBuffer[index4] = (byte)(color.Blue * 255);
        backBuffer[index4 + 1] = (byte)(color.Green * 255);
        backBuffer[index4 + 2] = (byte)(color.Red * 255);
        backBuffer[index4 + 3] = (byte)(color.Alpha * 255);
        // 设置深度缓冲区
        depthBuffer[index] = z;
    }
    
    /// <summary>
    /// 投影
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="transMat"></param>
    /// <returns></returns>
    public Vector3 Project(Vector3 coord, Matrix transMat)
    {
        // 矩阵变换
        var point = Vector3.TransformCoordinate(coord, transMat);
        // 变换后的结果位于标准化空间,x,y取值范围[-1,1]
        // 映射到屏幕空间,(0,0)为屏幕左上角
        var x = point.X * bmp.PixelWidth + bmp.PixelWidth / 2.0f;
        // 取反是因为标准化空间中y轴正方向向上,而屏幕坐标中y轴正方向向下
        var y = -point.Y * bmp.PixelHeight + bmp.PixelHeight / 2.0f;
        return new Vector3(x, y,point.Z);
    }

    /// <summary>
    /// 置指定像素颜色 考虑裁剪设
    /// </summary>
    /// <param name="point"></param>
    /// <param name="color"></param>
    public void DrawPoint(Vector3 point,Color4 color)
    {
        if (point.X >= 0 && point.Y >= 0 && point.X < bmp.PixelWidth && point.Y < bmp.PixelHeight)
            PutPixel((int)point.X, (int)point.Y, point.Z, color);
    }
    
    /// <summary>
    /// 限制值
    /// </summary>
    /// <param name="tar"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    private float _clamp(float tar, float min = 0f, float max = 1f)
    {
        return Math.Max(min, Math.Min(tar, max));
    }

    /// <summary>
    /// 取插值
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="percent"></param>
    /// <returns></returns>
    private float _interpolate(float min, float max, float percent)
    {
        return min + (max - min) * _clamp(percent);
    }

    /// <summary>
    /// 画线
    /// </summary>
    /// <param name="curY"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="p4"></param>
    /// <param name="color"></param>
    private void _drawScanLine(int curY, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,Color4 color)
    {
        // y轴当前进度
        var percent1 = p1.Y != p2.Y ? (curY - p1.Y) / (p2.Y - p1.Y) : 1;
        var percent2 = p3.Y != p4.Y ? (curY - p3.Y) / (p4.Y - p3.Y) : 1;
        
        // 根据进度得出x起点和终点
        var x1 = (int)_interpolate(p1.X, p2.X, percent1);
        var x2 = (int)_interpolate(p3.X, p4.X, percent2);

        // 根据进度得出z起点和终点
        var z1 = _interpolate(p1.Z, p2.Z, percent1);
        var z2 = _interpolate(p3.Z, p4.Z, percent2);

        for (int x = x1; x < x2; x++)
        {
            // 根据进度得出z位置
            var percent = (x - x1) / (float)(x2 - x1);
            var z = _interpolate(z1, z2, percent);
            DrawPoint(new Vector3(x,curY,z),color);
        }
    }

    /// <summary>
    /// 画三角形
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="color"></param>
    private void _drawTriangle(Vector3 p1,Vector3 p2,Vector3 p3,Color4 color)
    {
        // 按y轴排序 p1 p2 p3
        // 因为是屏幕坐标,所以y越小越靠上
        if (p1.Y > p2.Y) (p1, p2) = (p2, p1);
        if (p2.Y > p3.Y) (p2, p3) = (p3, p2);
        if (p1.Y > p2.Y) (p1, p2) = (p2, p1);
        
        // 计算斜率
        float dP1P2, dP1P3;
        if (p2.Y - p1.Y > 0)
            dP1P2 = (p2.X - p1.X) / (p2.Y - p1.Y);
        else
            dP1P2 = 0;

        if (p3.Y - p1.Y > 0)
            dP1P3 = (p3.X - p1.X) / (p3.Y - p1.Y);
        else
            dP1P3 = 0;

        // p2在右边
        //     P1
        //    -
        //    -- 
        //   - -
        //   -  -
        //  -   - P2
        //  -  -
        // - -
        // -
        // P3
        if (dP1P2 > dP1P3)
        {
            for (int y = (int)p1.Y; y <= (int)p3.Y; y++)
            {
                // 画上半部分
                if(y < p2.Y) _drawScanLine(y,p1,p3,p1,p2,color);
                // 画下半部分
                else _drawScanLine(y,p1,p3,p2,p3,color);
            }
        }
        // p2在右边
        //            P1
        //            -
        //           -- 
        //         - -
        //        -  -
        //   P2 -   - 
        //       -  -
        //       - -
        //         -
        //       P3
        else
        {
            for (int y = (int)p1.Y; y <= (int)p3.Y; y++)
            {
                // 画上半部分
                if(y < p2.Y) _drawScanLine(y,p1,p2,p1,p3,color);
                // 画下半部分
                else _drawScanLine(y,p2,p3,p1,p3,color);
            }
        }


    }

    /// <summary>
    /// 从JSON文件获取网格
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public async Task<Mesh[]> LoadMeshFromJSONFile(string fileName)
    {
        var meshes = new List<Mesh>();
        // 获取工程根目录路径
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(currentDirectory, fileName);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"文件未找到: {filePath}");
        }
        
        // 异步读取JSON文件
        string jsonContent = await File.ReadAllTextAsync(filePath);
        var jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Babylon>(jsonContent);

        for (int i = 0; i < jsonObject.meshes.Count; i++)
        {
            var meshData = jsonObject.meshes[i];
            // 顶点
            var vertices = meshData.vertices;
            // 面的顶点
            var indices = meshData.indices;
            // uv数量
            var uvCnt = meshData.uvCount;
            // 步长
            var step = 1;
            
            // 根据uv数量调整步长
            switch (uvCnt)
            {
                case 0:
                    step = 6;
                    break;
                case 1:
                    step = 8;
                    break;
                case 2:
                    step = 10;
                    break;
            }
            
            // 实际的顶点数量
            var verticesCnt = vertices.Count / step;
            // 面的数量
            var faceCnt = indices.Count / 3;

            var mesh = new Mesh(meshData.name, verticesCnt, faceCnt);

            // 填充顶点坐标
            for (int j = 0; j < verticesCnt; j++)
            {
                var x = vertices[j * step];
                var y = vertices[j * step + 1];
                var z = vertices[j * step + 2];
                mesh.Vertices[j] = new Vector3(x, y, z);
            }
            
            // 填充面数据
            for (int j = 0; j < faceCnt; j++)
            {
                var a = indices[j * 3];
                var b = indices[j * 3 + 1];
                var c = indices[j * 3 + 2];
                mesh.Faces[j] = new Face() { A = a, B = b, C = c };
            }
            // 设置位置
            var pos = meshData.position;
            mesh.Position = new Vector3(pos[0], pos[1], pos[2]);
            meshes.Add(mesh);
        }

        return meshes.ToArray();
    }

    public void Render(Camera camera,params Mesh[] meshes)
    {
        // 创建相机坐标系转换矩阵(左手坐标系)
        var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
        // 创建投影矩阵
        var projectionMatrix = Matrix.PerspectiveFovLH(0.78f, (float)bmp.PixelWidth / bmp.PixelHeight, 0.01f, 1.0f);

        var faceIndex = 0;
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
                // 随机一个颜色
                var color = 0.25f + (faceIndex % mesh.Faces.Length) * 0.75f / mesh.Faces.Length;
                _drawTriangle(point1,point2,point3,new Color4(color,color,color,1));
                faceIndex++;
            }
        }
    }
}