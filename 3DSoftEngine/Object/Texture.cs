using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpDX;

namespace SoftEngine;

public class Texture
{
    private byte[] _buffer;
    private int _width;
    private int _height;

    public Texture(string path,int width,int height)
    {
        _width = width;
        _height = height;
        _load(path);
    }

    private async void _load(string path)
    {
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string filePath = Path.Combine(currentDirectory, path);
        
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            // 使用BitmapImage先加载图像，然后转换为WriteableBitmap
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
        
            // 创建WriteableBitmap
            var bmp = new WriteableBitmap(bitmapImage);
            
            // 获取像素数据
            _buffer = new byte[bmp.BackBufferStride * bmp.PixelHeight];
            bmp.CopyPixels(_buffer, bmp.BackBufferStride, 0);
        }
    }
    
    /// <summary>
    /// 根据uv坐标获取颜色值
    /// </summary>
    /// <param name="tu"></param>
    /// <param name="tv"></param>
    /// <returns></returns>
    public Color4 Map(float tu, float tv)
    {
        if (_buffer == null)
        {
            return Color4.White;
        }
        // 循环采样
        int u = Math.Abs((int) (tu*_width) % _width);
        int v = Math.Abs((int) (tv*_height) % _height);

        int pos = (u + v * _width) * 4;
        byte b = _buffer[pos];
        byte g = _buffer[pos + 1];
        byte r = _buffer[pos + 2];
        byte a = _buffer[pos + 3];

        return new Color4(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
    }
}