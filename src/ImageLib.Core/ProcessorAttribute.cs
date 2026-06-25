using System;

namespace ImageLib.Core
{
    /// <summary>
    /// 算子元数据标记特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ProcessorAttribute : Attribute
    {
        public string Name { get; }
        public string Category { get; }
        public string Description { get; }

        public ProcessorAttribute(string name, string category, string description = "")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Category = category ?? throw new ArgumentNullException(nameof(category));
            Description = description ?? string.Empty;
        }
    }
}
