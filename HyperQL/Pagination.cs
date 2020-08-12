using System.Collections.Generic;

namespace HyperQL
{
    public class Pagination
    {
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public int? Page { get; set; }
        public bool? ShouldTakeAllRecords { get; set; }
        public List<OrderField> OrderFields { get; set; }

        public int TotalNumberOfRecords { get; set; }
        public int TotalNumberOfPages => Take.GetValueOrDefault() == 0 ? 0 : (TotalNumberOfRecords % Take.GetValueOrDefault() != 0) ? (TotalNumberOfRecords / Take.GetValueOrDefault()) + 1 : TotalNumberOfRecords / Take.GetValueOrDefault();
    }
}
