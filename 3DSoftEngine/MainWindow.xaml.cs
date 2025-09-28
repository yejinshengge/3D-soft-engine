using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpDX;

namespace SoftEngine;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Device _device;
    private Mesh _mesh = new Mesh("Cube",8,12);
    private Camera _camera = new Camera();
    
    public MainWindow()
    {
        InitializeComponent();
        Loaded += _onLoaded;
    }

    private void _onLoaded(object sender, RoutedEventArgs e)
    {
        // 1. 创建 WriteableBitmap
        WriteableBitmap bmp = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgra32, null);
        
        // 2.创建device
        _device = new Device(bmp);
        
        // 3. 将 bmp 设置为 Image 控件（frontBuffer）的源
        frontBuffer.Source = bmp;
        
        // 4. 初始化Cube网格顶点
        _mesh.Vertices[0] = new Vector3(-1, 1, 1);
        _mesh.Vertices[1] = new Vector3(1, 1, 1);
        _mesh.Vertices[2] = new Vector3(-1, -1, 1);
        _mesh.Vertices[3] = new Vector3(1, -1, 1);
        _mesh.Vertices[4] = new Vector3(-1, 1, -1);
        _mesh.Vertices[5] = new Vector3(1, 1, -1);
        _mesh.Vertices[6] = new Vector3(1, -1, -1);
        _mesh.Vertices[7] = new Vector3(-1, -1, -1);
        
        _mesh.Faces[0] = new Face { A = 0, B = 1, C = 2 };
        _mesh.Faces[1] = new Face { A = 1, B = 2, C = 3 };
        _mesh.Faces[2] = new Face { A = 1, B = 3, C = 6 };
        _mesh.Faces[3] = new Face { A = 1, B = 5, C = 6 };
        _mesh.Faces[4] = new Face { A = 0, B = 1, C = 4 };
        _mesh.Faces[5] = new Face { A = 1, B = 4, C = 5 };

        _mesh.Faces[6] = new Face { A = 2, B = 3, C = 7 };
        _mesh.Faces[7] = new Face { A = 3, B = 6, C = 7 };
        _mesh.Faces[8] = new Face { A = 0, B = 2, C = 7 };
        _mesh.Faces[9] = new Face { A = 0, B = 4, C = 7 };
        _mesh.Faces[10] = new Face { A = 4, B = 5, C = 6 };
        _mesh.Faces[11] = new Face { A = 4, B = 6, C = 7 };
        
        _camera.Position = new Vector3(0, 0, 10.0f);
        _camera.Target = Vector3.Zero;
        
        // 5. 注册 WPF 渲染循环事件
        CompositionTarget.Rendering += _onRendering;
    }

    private void _onRendering(object? sender, EventArgs e)
    {
        // 清空缓冲区
        _device.Clear(0,0,0,255);
        // 执行旋转
        _mesh.Rotation = new Vector3(_mesh.Rotation.X + 0.01f, _mesh.Rotation.Y + 0.01f, _mesh.Rotation.Z);
        // 填充到缓冲区
        _device.Render(_camera,_mesh);
        // 提交
        _device.Present();
    }
}