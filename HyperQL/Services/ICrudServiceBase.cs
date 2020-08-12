using System.Collections.Generic;
using System.Threading.Tasks;

namespace HyperQL
{
    public interface ICrudServiceBase<TEntity, TInsertRequest, TUpdateRequest, TSearchRequest, TSearchResponse, TEntityDTO>
        : IReadServiceBase<TEntity, TSearchRequest, TSearchResponse, TEntityDTO>
        where TEntity : EntityBase,
        new() where TInsertRequest : class,
        new() where TUpdateRequest : class, IUpdateRequestBase,
        new() where TSearchRequest : SearchRequestBase,
        new() where TSearchResponse : SearchResponseBase<TSearchRequest, TEntityDTO>,
        new() where TEntityDTO : class
    {
        #region Inits

        Task<TInsertRequest> InitForAdd();
        Task<TUpdateRequest> InitForUpdate(int id);
        Task<TUpdateRequest> InitForUpdate<TExecutionUser>(int id, TExecutionUser executionUser = null) where TExecutionUser : class;

        #endregion

        #region Regular

        #region Add

        Task<TEntityDTO> Add(TEntity entity);
        Task<TEntityDTO> Add(TInsertRequest request);
        Task<IEnumerable<TEntityDTO>> AddRange(IEnumerable<TEntity> entities);
        Task<IEnumerable<TEntityDTO>> AddRange(IEnumerable<TInsertRequest> requests);

        #endregion

        #region Update

        Task<TEntityDTO> Update(TEntity entity);
        Task<TEntityDTO> Update(TUpdateRequest request);
        Task<IEnumerable<TEntityDTO>> UpdateRange(IEnumerable<TEntity> entities);
        Task<IEnumerable<TEntityDTO>> UpdateRange(IEnumerable<TUpdateRequest> requests);

        #endregion

        #region Delete

        Task Delete(int id, bool soft = true);
        Task DeleteRange(IEnumerable<int> ids, bool soft = true);
        Task Delete(TEntity entity, bool soft = true);
        Task DeleteRange(IEnumerable<TEntity> entities, bool soft = true);

        #endregion

        #region UnDelete

        Task<TEntityDTO> UnDelete(int id);
        Task<IEnumerable<TEntityDTO>> UnDeleteRange(IEnumerable<int> ids);
        Task<TEntityDTO> UnDelete(TEntity entity);
        Task<IEnumerable<TEntityDTO>> UnDeleteRange(IEnumerable<TEntity> entities);

        #endregion

        #endregion

        #region With record level authorization

        #region Add

        Task<TEntityDTO> Add<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null) where TExecutionUser : class;
        Task<TEntityDTO> Add<TExecutionUser>(TInsertRequest request, TExecutionUser executionUser = null) where TExecutionUser : class;
        Task<IEnumerable<TEntityDTO>> AddRange<TExecutionUser>(IEnumerable<TEntity> entities, TExecutionUser executionUser = null) where TExecutionUser : class;
        Task<IEnumerable<TEntityDTO>> AddRange<TExecutionUser>(IEnumerable<TInsertRequest> requests, TExecutionUser executionUser = null) where TExecutionUser : class;

        #endregion

        #region Update

        Task<TEntityDTO> Update<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null) where TExecutionUser : class;
        Task<TEntityDTO> Update<TExecutionUser>(TUpdateRequest request, TExecutionUser executionUser = null) where TExecutionUser : class;
        Task<IEnumerable<TEntityDTO>> UpdateRange<TExecutionUser>(IEnumerable<TEntity> entities, TExecutionUser executionUser = null) where TExecutionUser : class;
        Task<IEnumerable<TEntityDTO>> UpdateRange<TExecutionUser>(IEnumerable<TUpdateRequest> requests, TExecutionUser executionUser = null) where TExecutionUser : class;

        #endregion

        #region Delete

        Task Delete<TExecutionUser>(int id, TExecutionUser executionUser = null, bool soft = true) where TExecutionUser : class;
        Task DeleteRange<TExecutionUser>(IEnumerable<int> ids, TExecutionUser executionUser = null, bool soft = true) where TExecutionUser : class;
        Task Delete<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null, bool soft = true) where TExecutionUser : class;
        Task DeleteRange<TExecutionUser>(IEnumerable<TEntity> entities, TExecutionUser executionUser = null, bool soft = true) where TExecutionUser : class;

        #endregion

        #region UnDelete

        Task<TEntityDTO> UnDelete<TExecutionUser>(int id, TExecutionUser executionUser = null) where TExecutionUser : class;
        Task<IEnumerable<TEntityDTO>> UnDeleteRange<TExecutionUser>(IEnumerable<int> ids, TExecutionUser executionUser = null) where TExecutionUser : class;
        Task<TEntityDTO> UnDelete<TExecutionUser>(TEntity entity, TExecutionUser entities = null) where TExecutionUser : class;
        Task<IEnumerable<TEntityDTO>> UnDeleteRange<TExecutionUser>(IEnumerable<TEntity> entities, TExecutionUser executionUser = null) where TExecutionUser : class;

        #endregion

        #endregion

        #region Helpers

        Task SaveChanges();

        #endregion
    }
}
