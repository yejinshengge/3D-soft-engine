namespace SoftEngine;

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
}