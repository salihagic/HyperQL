using System;

namespace HyperQL
{
    internal class PropInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public object Value { get; set; }
        public CompareType? CompareType { get; set; }
        public bool? IsCaseSensitive { get; set; }
    }
}
