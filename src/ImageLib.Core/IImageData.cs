using System;
using System.Collections.Generic;

namespace ImageLib.Core
{
    /// <summary>
    /// 图像数据封装接口（不直接暴露 Halcon HObject）
    /// </summary>
    public interface IImageData : IDisposable
    {
        string Id { get; }
        int Width { get; }
        int Height { get; }
        int Channels { get; }
        string PixelType { get; }
        DateTime CaptureTime { get; }
        IReadOnlyDictionary<string, object> Metadata { get; }

        object GetNativeHandle();
        IImageData Clone();
    }
}
