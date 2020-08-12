namespace HyperQL
{
    public class SearchRequestBase
    {
        public int Id { get; set; }
        public bool? IsDeleted { get; set; }
        [Ignore]
        public Pagination Pagination { get; set; }
    }
}
