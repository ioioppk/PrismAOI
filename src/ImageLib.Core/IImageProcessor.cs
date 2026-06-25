using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ImageLib.Core
{
    /// <summary>
    /// 图像处理算子接口
    /// </summary>
    public interface IImageProcessor
    {
        string Id { get; }
        string Name { get; }
        string Category { get; }

        IReadOnlyList<ParamDef> InputParams { get; }
        IReadOnlyList<ParamDef> OutputParams { get; }

        Task<ProcessResult> ExecuteAsync(ProcessContext context, CancellationToken ct = default);
    }

    /// <summary>
    /// 算子执行上下文
    /// </summary>
    public class ProcessContext
    {
        private readonly Dictionary<string, object> _items = new Dictionary<string, object>();

        public IImageData InputImage { get; set; }
        public IRegionData InputRegion { get; set; }
        public IImageData OutputImage { get; set; }
        public IRegionData OutputRegion { get; set; }
        public Dictionary<string, IImageData> Images { get; } = new Dictionary<string, IImageData>();
        public Dictionary<string, IRegionData> Regions { get; } = new Dictionary<string, IRegionData>();

        public object this[string key]
        {
            get => _items.TryGetValue(key, out var value) ? value : null;
            set => _items[key] = value;
        }

        public T Get<T>(string key) where T : class
        {
            return this[key] as T;
        }

        public T GetOrCreate<T>(string key, Func<T> factory) where T : class
        {
            if (this[key] is T existing)
                return existing;

            var value = factory();
            this[key] = value;
            return value;
        }
    }
}
