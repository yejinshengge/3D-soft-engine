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
    // 用来给像素处理加锁
    private object[] lockBuffer;
    // 步长 每行像素所占字节数
    private int backBufferStride;
    
    private WriteableBitmap bmp;
    // 窗口宽度
    private int _pixelWidth;
    // 窗口高度
    private int _pixelHeight;

    public Device(WriteableBitmap bmp)
    {
        this.bmp = bmp;
        _pixelWidth = bmp.PixelWidth;
        _pixelHeight = bmp.PixelHeight;
        backBufferStride = bmp.PixelWidth * 4;
        // 长*宽*4(RGBA)
        backBuffer = new byte[backBufferStride * _pixelHeight];
        depthBuffer = new float[_pixelWidth * _pixelHeight];
        // 初始化锁
        lockBuffer = new Object[_pixelWidth * _pixelHeight];
        for (int i = 0; i < lockBuffer.Length; i++)
        {
            lockBuffer[i] = new object();
        }
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
        bmp.WritePixels(new Int32Rect(0, 0, _pixelWidth, _pixelHeight), 
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
        int index = x + y * _pixelWidth;
        int index4 = index * 4;
        // 加锁
        lock (lockBuffer[index])
        {
            // 深度大于缓存值,不渲染
            if(z > depthBuffer[index]) return;
            // 设置深度缓冲区
            depthBuffer[index] = z;
            // 设置后备缓冲区
            backBuffer[index4] = (byte)(color.Blue * 255);
            backBuffer[index4 + 1] = (byte)(color.Green * 255);
            backBuffer[index4 + 2] = (byte)(color.Red * 255);
            backBuffer[index4 + 3] = (byte)(color.Alpha * 255);
        }
    }

    /// <summary>
    /// 投影
    /// </summary>
    /// <param name="vertex"></param>
    /// <param name="transMat"></param>
    /// <param name="worldMat"></param>
    /// <returns></returns>
    public Vertex Project(Vertex vertex, Matrix transMat,Matrix worldMat)
    {
        // 齐次裁剪空间
        var point2D = Vector3.TransformCoordinate(vertex.Coordinates, transMat);
        // 世界坐标
        var point3D = Vector3.TransformCoordinate(vertex.Coordinates, worldMat);
        var normal3D = Vector3.TransformCoordinate(vertex.Normal, worldMat);
        
        // 变换后的结果位于标准化空间,x,y取值范围[-1,1]
        // 映射到屏幕空间,(0,0)为屏幕左上角
        var x = point2D.X * _pixelWidth + _pixelWidth / 2.0f;
        // 取反是因为标准化空间中y轴正方向向上,而屏幕坐标中y轴正方向向下
        var y = -point2D.Y * _pixelHeight + _pixelHeight / 2.0f;
        return new Vertex()
        {
            Coordinates = new Vector3(x, y, point2D.Z),
            Normal = normal3D,
            WorldCoordinates = point3D,
            // 纹理坐标原样传递
            TextureCoordinates = vertex.TextureCoordinates
        };
    }

    /// <summary>
    /// 置指定像素颜色 考虑裁剪设
    /// </summary>
    /// <param name="point"></param>
    /// <param name="color"></param>
    public void DrawPoint(Vector3 point,Color4 color)
    {
        if (point.X >= 0 && point.Y >= 0 && point.X < _pixelWidth && point.Y < _pixelHeight)
            PutPixel((int)point.X, (int)point.Y, point.Z, color);
    }

    /// <summary>
    /// 画线
    /// </summary>
    /// <param name="data"></param>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="v4"></param>
    /// <param name="color"></param>
    /// <param name="texture"></param>
    private void _drawScanLine(ScanLineData data, Vertex v1, Vertex v2, Vertex v3, Vertex v4,Color4 color,Texture texture)
    {
        var p1 = v1.Coordinates;
        var p2 = v2.Coordinates;
        var p3 = v3.Coordinates;
        var p4 = v4.Coordinates;
        
        // y轴当前进度
        var percent1 = p1.Y != p2.Y ? (data.CurrentY - p1.Y) / (p2.Y - p1.Y) : 1;
        var percent2 = p3.Y != p4.Y ? (data.CurrentY - p3.Y) / (p4.Y - p3.Y) : 1;
        
        // 根据进度得出x起点和终点
        var x1 = (int)_interpolate(p1.X, p2.X, percent1);
        var x2 = (int)_interpolate(p3.X, p4.X, percent2);

        // 根据进度得出z起点和终点
        var z1 = _interpolate(p1.Z, p2.Z, percent1);
        var z2 = _interpolate(p3.Z, p4.Z, percent2);
        
        // 根据进度得出法线和光线的点积
        var snl = _interpolate(data.NDotLa, data.NDotLb, percent1);
        var enl = _interpolate(data.NDotLc, data.NDotLd, percent2);
        
        // 根据进度得出uv
        var su = _interpolate(data.Ua, data.Ub, percent1);
        var eu = _interpolate(data.Uc, data.Ud, percent2);
        var sv = _interpolate(data.Va, data.Vb, percent2);
        var ev = _interpolate(data.Vc, data.Vd, percent2);

        for (int x = x1; x < x2; x++)
        {
            // 根据进度得出z位置
            var percent = (x - x1) / (float)(x2 - x1);
            var z = _interpolate(z1, z2, percent);
            var ndotl = _interpolate(snl, enl, percent);
            var u = _interpolate(su, eu, percent);
            var v = _interpolate(sv, ev, percent);

            Color4 textureColor;
            if (texture != null)
                textureColor = texture.Map(u, v);
            else
                textureColor = new Color4(1, 1, 1, 1);

            var brightness = color * ndotl;
            brightness.Alpha = 1;
            DrawPoint(new Vector3(x,data.CurrentY,z),brightness * textureColor);
        }
    }

    /// <summary>
    /// 画三角形
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    /// <param name="color"></param>
    /// <param name="texture"></param>
    private void _drawTriangle(Vertex v1,Vertex v2,Vertex v3,Color4 color,Texture texture)
    {

        // 按y轴排序 p1 p2 p3
        // 因为是屏幕坐标,所以y越小越靠上
        if (v1.Coordinates.Y > v2.Coordinates.Y) (v1, v2) = (v2, v1);
        if (v2.Coordinates.Y > v3.Coordinates.Y) (v2, v3) = (v3, v2);
        if (v1.Coordinates.Y > v2.Coordinates.Y) (v1, v2) = (v2, v1);
        
        var p1 = v1.Coordinates;
        var p2 = v2.Coordinates;
        var p3 = v3.Coordinates;
        // 光源
        var lightPos = new Vector3(0, 10, 10);
        // 计算法线和光线方向的点积
        var nl1 = _computeNDotL(v1.WorldCoordinates, v1.Normal, lightPos);
        var nl2 = _computeNDotL(v2.WorldCoordinates, v2.Normal, lightPos);
        var nl3 = _computeNDotL(v3.WorldCoordinates, v3.Normal, lightPos);

        var scanLineData = new ScanLineData();
        
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
                scanLineData.CurrentY = y;
                // 画上半部分
                if (y < p2.Y)
                {
                    scanLineData.NDotLa = nl1;
                    scanLineData.NDotLb = nl3;
                    scanLineData.NDotLc = nl1;
                    scanLineData.NDotLd = nl2;

                    scanLineData.Ua = v1.TextureCoordinates.X;
                    scanLineData.Ub = v3.TextureCoordinates.X;
                    scanLineData.Uc = v1.TextureCoordinates.X;
                    scanLineData.Ud = v2.TextureCoordinates.X;
                    
                    scanLineData.Va = v1.TextureCoordinates.Y;
                    scanLineData.Vb = v3.TextureCoordinates.Y;
                    scanLineData.Vc = v1.TextureCoordinates.Y;
                    scanLineData.Vd = v2.TextureCoordinates.Y;
                    _drawScanLine(scanLineData,v1,v3,v1,v2,color,texture);
                }
                // 画下半部分
                else
                {
                    scanLineData.NDotLa = nl1;
                    scanLineData.NDotLb = nl3;
                    scanLineData.NDotLc = nl2;
                    scanLineData.NDotLd = nl3;
                    
                    scanLineData.Ua = v1.TextureCoordinates.X;
                    scanLineData.Ub = v3.TextureCoordinates.X;
                    scanLineData.Uc = v2.TextureCoordinates.X;
                    scanLineData.Ud = v3.TextureCoordinates.X;
                    
                    scanLineData.Va = v1.TextureCoordinates.Y;
                    scanLineData.Vb = v3.TextureCoordinates.Y;
                    scanLineData.Vc = v2.TextureCoordinates.Y;
                    scanLineData.Vd = v3.TextureCoordinates.Y;
                    _drawScanLine(scanLineData,v1,v3,v2,v3,color,texture);
                }
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
                scanLineData.CurrentY = y;
                // 画上半部分
                if (y < p2.Y)
                {
                    scanLineData.NDotLa = nl1;
                    scanLineData.NDotLb = nl2;
                    scanLineData.NDotLc = nl1;
                    scanLineData.NDotLd = nl3;
                    
                    scanLineData.Ua = v1.TextureCoordinates.X;
                    scanLineData.Ub = v2.TextureCoordinates.X;
                    scanLineData.Uc = v1.TextureCoordinates.X;
                    scanLineData.Ud = v3.TextureCoordinates.X;
                    
                    scanLineData.Va = v1.TextureCoordinates.Y;
                    scanLineData.Vb = v2.TextureCoordinates.Y;
                    scanLineData.Vc = v1.TextureCoordinates.Y;
                    scanLineData.Vd = v3.TextureCoordinates.Y;
                    _drawScanLine(scanLineData,v1,v2,v1,v3,color,texture);
                }
                // 画下半部分
                else
                {
                    scanLineData.NDotLa = nl2;
                    scanLineData.NDotLb = nl3;
                    scanLineData.NDotLc = nl1;
                    scanLineData.NDotLd = nl3;
                    
                    scanLineData.Ua = v2.TextureCoordinates.X;
                    scanLineData.Ub = v3.TextureCoordinates.X;
                    scanLineData.Uc = v1.TextureCoordinates.X;
                    scanLineData.Ud = v3.TextureCoordinates.X;
                    
                    scanLineData.Va = v2.TextureCoordinates.Y;
                    scanLineData.Vb = v3.TextureCoordinates.Y;
                    scanLineData.Vc = v1.TextureCoordinates.Y;
                    scanLineData.Vd = v3.TextureCoordinates.Y;
                    _drawScanLine(scanLineData,v2,v3,v1,v3,color,texture);
                }
            }
        }


    }

    /// <summary>
    /// 从JSON文件获取网格
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public async Task<Mesh[]> LoadMeshFromJsonFile(string fileName)
    {
        var meshes = new List<Mesh>();
        var materials = new Dictionary<String,Material>();
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

        if (jsonObject == null) throw new Exception("解析json出错!");
        
        // 材质
        for (var materialIndex = 0; materialIndex < jsonObject.materials.Count; materialIndex++)
        {
            var material = jsonObject.materials[materialIndex];
            materials.Add(material.id, material);
        }
        
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
                // 坐标
                var x = vertices[j * step];
                var y = vertices[j * step + 1];
                var z = vertices[j * step + 2];
                // 法线
                var nx = vertices[j * step + 3];
                var ny = vertices[j * step + 4];
                var nz = vertices[j * step + 5];
                mesh.Vertices[j] = new Vertex()
                {
                    Coordinates = new Vector3(x, y, z),
                    Normal = new Vector3(nx,ny,nz)
                };
                
                
                if (uvCnt > 0)
                {
                    // uv
                    float u = vertices[j * step + 6];
                    float v = vertices[j * step + 7];
                    mesh.Vertices[j].TextureCoordinates = new Vector2(u, v);
                }
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

            if (uvCnt > 0)
            {
                var meshTextureId = meshData.materialId;
                var meshTextureName = materials[meshTextureId].diffuseTexture.name;
                mesh.Texture = new Texture($"Resource\\{meshTextureName}", 512, 512);
            }
            mesh.ComputeFacesNormal();
            meshes.Add(mesh);
        }

        return meshes.ToArray();
    }

    public void Render(Camera camera,params Mesh[] meshes)
    {
        // 创建相机坐标系转换矩阵(左手坐标系)
        var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
        // 创建投影矩阵
        var projectionMatrix = Matrix.PerspectiveFovLH(0.78f, (float)_pixelWidth / _pixelHeight, 0.01f, 1.0f);

        foreach (var mesh in meshes)
        {
            // 创建世界坐标变换矩阵
            var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, 
                                  mesh.Rotation.X, mesh.Rotation.Z) * Matrix.Translation(mesh.Position);
            // 相机空间变换矩阵
            var worldViewMatrix = worldMatrix * viewMatrix;
            // 投影变换矩阵
            var transformMatrix = worldViewMatrix * projectionMatrix;

            // 并行绘制
            Parallel.For(0, mesh.Faces.Length, faceIndex =>
            {
                var face = mesh.Faces[faceIndex];
                var verA = mesh.Vertices[face.A];
                var verB = mesh.Vertices[face.B];
                var verC = mesh.Vertices[face.C];
                
                // 背面剔除（考虑透视相机）
                // 计算三角形中心点在世界空间的位置
                var worldCenter = (Vector3.TransformCoordinate(verA.Coordinates, worldMatrix) +
                                   Vector3.TransformCoordinate(verB.Coordinates, worldMatrix) +
                                   Vector3.TransformCoordinate(verC.Coordinates, worldMatrix)) / 3.0f;
                
                // 计算从相机到三角形中心的视线向量
                var viewDirection = worldCenter - camera.Position;
                
                // 将法线变换到世界空间
                var worldNormal = Vector3.TransformNormal(face.Normal, worldMatrix);
                
                // 计算视线向量与法线的点积
                // 如果点积 > 0，说明法线背向相机，是背面，应该剔除
                if (Vector3.Dot(viewDirection, worldNormal) > 0)
                {
                    return;
                }
                
                // 转换到2d屏幕空间
                var point1 = Project(verA, transformMatrix,worldMatrix);
                var point2 = Project(verB, transformMatrix,worldMatrix);
                var point3 = Project(verC, transformMatrix,worldMatrix);
                // 绘制到屏幕上
                // 随机一个颜色
                // var color = 0.25f + (faceIndex % mesh.Faces.Length) * 0.75f / mesh.Faces.Length;
                _drawTriangle(point1, point2, point3, new Color4(1f, 1f, 1f, 1f),mesh.Texture);
            });
        }
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
    /// 计算法线和光线方向的点积
    /// </summary>
    /// <param name="vertex"></param>
    /// <param name="normal"></param>
    /// <param name="lightPos"></param>
    /// <returns></returns>
    private float _computeNDotL(Vector3 vertex, Vector3 normal, Vector3 lightPos)
    {
        // 光线方向
        var lightDir = lightPos - vertex;
        
        // 归一化
        normal.Normalize();
        lightDir.Normalize();

        return Math.Max(0, Vector3.Dot(normal, lightDir));
    }
}