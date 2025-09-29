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
    private Mesh[] _meshes;
    private Camera _camera = new ();
    
    public MainWindow()
    {
        InitializeComponent();
        Loaded += _onLoaded;
    }

    private async void _onLoaded(object sender, RoutedEventArgs e)
    {
        // 1. 创建 WriteableBitmap
        WriteableBitmap bmp = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgra32, null);
        
        // 2.创建device
        _device = new Device(bmp);
        
        // 3. 将 bmp 设置为 Image 控件（frontBuffer）的源
        frontBuffer.Source = bmp;
        
        // 4. 初始化网格顶点
        _meshes = await _device.LoadMeshFromJSONFile("Resource/monkey.babylon");
        
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
        foreach (var mesh in _meshes)
        {
            mesh.Rotation = new Vector3(mesh.Rotation.X + 0.01f, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
        }
        // 填充到缓冲区
        _device.Render(_camera,_meshes);
        // 提交
        _device.Present();
    }
}