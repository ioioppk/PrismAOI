using System;
using System.Collections.Generic;

namespace ImageLib.Core
{
    /// <summary>
    /// 区域数据封装接口（不直接暴露 Halcon HRegion）
    /// </summary>
    public interface IRegionData : IDisposable
    {
        string Id { get; }
        int Area { get; }
        string Type { get; }
        IReadOnlyDictionary<string, object> Metadata { get; }

        object GetNativeHandle();
        IRegionData Clone();
    }
}
