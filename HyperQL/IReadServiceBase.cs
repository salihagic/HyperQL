using System.Collections.Generic;
using System.Threading.Tasks;

namespace HyperQL
{
    public interface IReadServiceBase<TEntity, TSearchRequest, TSearchResponse, TEntityDTO>
        where TEntity : EntityBase,
        new() where TSearchRequest : SearchRequestBase,
        new() where TSearchResponse : SearchResponseBase<TSearchRequest, TEntityDTO>,
        new() where TEntityDTO : class
    {
        #region Inits

        Task<TSearchRequest> InitForGet();

        #endregion

        #region Regular

        Task<TEntityDTO> GetById(int id, TSearchRequest searchRequest = null);
        Task<TSearchResponse> Get(TSearchRequest searchRequest = null);
        Task<List<TEntityDTO>> GetItems(TSearchRequest searchRequest = null);
        Task SetParameters(TSearchRequest searchRequest = null);

        #endregion

        #region With record level authorization

        Task<TEntityDTO> GetById<TExecutionUser>(int id, TExecutionUser executionUser = null, TSearchRequest searchRequest = null) where TExecutionUser : class;
        Task<TSearchResponse> Get<TExecutionUser>(TSearchRequest searchRequest = null, TExecutionUser executionUser = null) where TExecutionUser : class;
        Task<List<TEntityDTO>> GetItems<TExecutionUser>(TSearchRequest searchRequest = null, TExecutionUser executionUser = null) where TExecutionUser : class;

        #endregion
    }
}
