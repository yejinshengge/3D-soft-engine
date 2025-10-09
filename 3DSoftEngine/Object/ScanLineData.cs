namespace SoftEngine;

/// <summary>
/// 扫描线数据
/// </summary>
public struct ScanLineData
{
    public int CurrentY;
    /// <summary>
    /// 法线和光线的点积
    /// </summary>
    public float NDotLa;
    public float NDotLb;
    public float NDotLc;
    public float NDotLd;

    /// <summary>
    /// uv
    /// </summary>
    public float Ua;
    public float Ub;
    public float Uc;
    public float Ud;
    
    public float Va;
    public float Vb;
    public float Vc;
    public float Vd;
}