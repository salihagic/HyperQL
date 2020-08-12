namespace HyperQL
{
    public class OrderField
    {
        public string Field { get; set; }
        public SortDirection Direction { get; set; }
    }

    public enum SortDirection
    {
        ASC,
        DESC
    }
}
