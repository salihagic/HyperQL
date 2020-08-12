using System;

namespace HyperQL
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IncludeAttribute : Attribute
    {
        public string EntityToInclude { get; set; }

        public IncludeAttribute(string entityToInclude = "")
        {
            EntityToInclude = entityToInclude;
        }
    }
}
