using System;

namespace ImageLib.Core
{
    /// <summary>
    /// 算子参数定义
    /// </summary>
    public sealed class ParamDef
    {
        public string Name { get; }
        public ParamType Type { get; }
        public object DefaultValue { get; }
        public string Description { get; }
        public object MinValue { get; }
        public object MaxValue { get; }

        public ParamDef(string name, ParamType type, object defaultValue, string description, object minValue = null, object maxValue = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
            DefaultValue = defaultValue;
            Description = description;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }

    public enum ParamType
    {
        Int,
        Double,
        String,
        Bool,
        Enum,
        Image,
        Region
    }
}
