using System;

namespace HyperQL
{
    //***
    //Use this attribute to decorate properties that should 
    //be used for building where clause or the query condition
    //***
    //Set CompareAlways property to make sure that each query 
    //sets decorated propery value in the condition, never to be ignored
    //By default property is ignored if it has default type value 
    //eg. int? default value is null or int default value is 0
    //But the property will still be ignored if it is decorated with Ignore attribute
    //***
    [AttributeUsage(AttributeTargets.Property)]
    public class CompareAttribute : Attribute
    {
        public CompareType? CompareType { get; set; }
        public bool? IsCaseSensitive { get; set; }
        public bool CompareAlways { get; set; }

        public CompareAttribute()
        {
            CompareType = null;
            IsCaseSensitive = null;
        }

        public CompareAttribute(CompareType compareType)
        {
            CompareType = compareType;
            IsCaseSensitive = null;
        }

        public CompareAttribute(bool compareAlways)
        {
            CompareType = null;
            IsCaseSensitive = null;
            CompareAlways = compareAlways;
        }

        public CompareAttribute(CompareType compareType, bool isCaseSensitive = false, bool compareAlways = false)
        {
            CompareType = compareType;
            IsCaseSensitive = isCaseSensitive;
            CompareAlways = compareAlways;
        }
    }

    public enum CompareType
    {
        Equals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        StartsWith,
        Contains,
        EndsWith,
    }
}
