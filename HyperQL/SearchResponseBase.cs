using System.Collections.Generic;

namespace HyperQL
{
    public class SearchResponseBase<TSearchRequest, TEntityDTO> 
        where TSearchRequest : SearchRequestBase,
        new() where TEntityDTO : class
    {
        public TSearchRequest SearchRequest { get; set; }
        public List<TEntityDTO> Items { get; set; }
    }
}
