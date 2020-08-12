using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HyperQL
{
    public class CrudServiceBase<TEntity, TInsertRequest, TUpdateRequest, TSearchRequest, TSearchResponse, TEntityDTO>
        : ReadServiceBase<TEntity, TSearchRequest, TSearchResponse, TEntityDTO>
        , ICrudServiceBase<TEntity, TInsertRequest, TUpdateRequest, TSearchRequest, TSearchResponse, TEntityDTO>
        where TEntity : EntityBase,
        new() where TInsertRequest : class,
        new() where TUpdateRequest : class, IUpdateRequestBase,
        new() where TSearchRequest : SearchRequestBase,
        new() where TSearchResponse : SearchResponseBase<TSearchRequest, TEntityDTO>,
        new() where TEntityDTO : class
    {
        public CrudServiceBase(IServiceProvider serviceProvider, DbContext dbContext) : base(serviceProvider, dbContext) { }

        #region Inits

        virtual public async Task<TInsertRequest> InitForAdd()
        {
            return new TInsertRequest();
        }

        virtual public async Task<TUpdateRequest> InitForUpdate(int id)
        {
            var dto = await GetById(id);
            return Mapper.Map<TUpdateRequest>(dto);
        }

        virtual public async Task<TUpdateRequest> InitForUpdate<TExecutionUser>(int id, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var dto = await GetById(id, executionUser);
            return Mapper.Map<TUpdateRequest>(dto);
        }

        #endregion

        #region Regular

        #region Add

        virtual public async Task<TEntityDTO> Add(TEntity entity)
        {
            var result = DbSet.Add(entity).Entity;
            await SaveChanges();
            return Mapper.Map<TEntityDTO>(result);
        }

        virtual public async Task<TEntityDTO> Add(TInsertRequest request)
        {
            return await Add(Mapper.Map<TEntity>(request));
        }

        virtual public async Task<IEnumerable<TEntityDTO>> AddRange(IEnumerable<TEntity> entities)
        {
            DbSet.AddRange(entities);
            await SaveChanges();

            return Mapper.Map<IEnumerable<TEntityDTO>>(entities);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> AddRange(IEnumerable<TInsertRequest> requests)
        {
            return await AddRange(Mapper.Map<IEnumerable<TEntity>>(requests));
        }

        #endregion

        #region Update

        virtual public async Task<TEntityDTO> Update(TEntity entity)
        {
            var result = DbSet.Update(entity).Entity;
            await SaveChanges();
            return Mapper.Map<TEntityDTO>(result);
        }

        virtual public async Task<TEntityDTO> Update(TUpdateRequest request)
        {
            var entity = DbSet.First(x => x.Id == request.Id);

            Mapper.Map(request, entity);

            return await Update(entity);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> UpdateRange(IEnumerable<TEntity> entities)
        {
            DbSet.UpdateRange(entities);

            return Mapper.Map<IEnumerable<TEntityDTO>>(entities);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> UpdateRange(IEnumerable<TUpdateRequest> requests)
        {
            var requestIds = requests.Select(y => y.Id);

            var entities = DbSet.Where(x => requestIds.Contains(x.Id)).ToList();

            Mapper.Map(requests, entities);

            return await UpdateRange(entities);
        }

        #endregion

        #region Delete

        virtual public async Task Delete(int id, bool soft = true)
        {
            var dbEntity = DbSet.FirstOrDefault(x => x.Id.Equals(id));
            await Delete(dbEntity, soft);
        }

        virtual public async Task DeleteRange(IEnumerable<int> ids, bool soft = true)
        {
            var entities = DbSet.Where(x => ids.Contains(x.Id)).ToList();

            if (soft)
            {
                entities?.ForEach(x => x.IsDeleted = true);
                DbSet.UpdateRange(entities);
            }
            else
            {
                DbSet.RemoveRange(entities);
            }
            await SaveChanges();
        }

        virtual public async Task Delete(TEntity entity, bool soft = true)
        {
            var dbEntity = DbSet.First(x => x.Id.Equals(entity.Id));

            if (dbEntity == null)
                throw new NullReferenceException();

            if (soft)
            {
                dbEntity.IsDeleted = true;
            }
            else
            {
                DbSet.Remove(dbEntity);
            }
            await SaveChanges();
        }

        virtual public async Task DeleteRange(IEnumerable<TEntity> entities1, bool soft = true)
        {
            var entities = DbSet.Where(x => entities1.Select(x => x.Id).Contains(x.Id)).ToList();

            if (soft)
            {
                entities?.ForEach(x => x.IsDeleted = true);
                DbSet.UpdateRange(entities);
            }
            else
            {
                DbSet.RemoveRange(entities);
            }
            await SaveChanges();
        }

        #endregion

        #region UnDelete

        virtual public async Task<TEntityDTO> UnDelete(int id)
        {
            var dbEntity = DbSet.First(x => x.Id.Equals(id));

            if (dbEntity == null)
                throw new NullReferenceException();

            dbEntity.IsDeleted = false;
            await SaveChanges();
            return Mapper.Map<TEntityDTO>(dbEntity);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> UnDeleteRange(IEnumerable<int> ids)
        {
            var entities = DbSet.Where(x => ids.Contains(x.Id)).ToList();

            entities?.ForEach(x => x.IsDeleted = false);

            await SaveChanges();
            return Mapper.Map<IEnumerable<TEntityDTO>>(entities);
        }

        virtual public async Task<TEntityDTO> UnDelete(TEntity entity)
        {
            var dbEntity = DbSet.First(x => x.Id.Equals(entity.Id));

            if (dbEntity == null)
                throw new NullReferenceException();

            dbEntity.IsDeleted = false;
            await SaveChanges();
            return Mapper.Map<TEntityDTO>(dbEntity);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> UnDeleteRange(IEnumerable<TEntity> entities1)
        {
            var entities = DbSet.Where(x => entities1.Select(x => x.Id).Contains(x.Id)).ToList();

            entities?.ForEach(x => x.IsDeleted = false);

            await SaveChanges();
            return Mapper.Map<IEnumerable<TEntityDTO>>(entities);
        }

        #endregion

        #endregion

        #region With record level authorization

        #region Add

        virtual public async Task<TEntityDTO> Add<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            if (!IsAuthorizedToAdd(entity, executionUser))
                throw new UnauthorizedAccessException();

            return await Add(entity);
        }

        virtual public async Task<TEntityDTO> Add<TExecutionUser>(TInsertRequest request, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            return await Add(Mapper.Map<TEntity>(request), executionUser);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> AddRange<TExecutionUser>(IEnumerable<TEntity> entities, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            if (entities?.Any(x => !IsAuthorizedToAdd(x, executionUser)) ?? false)
                throw new UnauthorizedAccessException();

            return await AddRange(entities);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> AddRange<TExecutionUser>(IEnumerable<TInsertRequest> requests, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            return await AddRange(Mapper.Map<IEnumerable<TEntity>>(requests), executionUser);
        }

        #endregion

        #region Update

        virtual public async Task<TEntityDTO> Update<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            if (!IsAuthorizedToUpdate(entity, executionUser))
                throw new UnauthorizedAccessException();

            return await Update(entity);
        }

        virtual public async Task<TEntityDTO> Update<TExecutionUser>(TUpdateRequest request, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var entity = DbSet.First(x => x.Id == request.Id);

            Mapper.Map(request, entity);

            return await Update(entity, executionUser);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> UpdateRange<TExecutionUser>(IEnumerable<TEntity> entities, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            if (entities?.Any(x => !IsAuthorizedToUpdate(x, executionUser)) ?? false)
                throw new UnauthorizedAccessException();

            return await UpdateRange(entities);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> UpdateRange<TExecutionUser>(IEnumerable<TUpdateRequest> requests, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var requestIds = requests.Select(y => y.Id);

            var entities = DbSet.Where(x => requestIds.Contains(x.Id)).ToList();

            Mapper.Map(requests, entities);

            return await UpdateRange(entities, executionUser);
        }

        #endregion

        #region Delete

        virtual public async Task Delete<TExecutionUser>(int id, TExecutionUser executionUser = null, bool soft = true) where TExecutionUser : class
        {
            var dbEntity = DbSet.FirstOrDefault(x => x.Id.Equals(id));

            if (!IsAuthorizedToDelete(dbEntity, executionUser))
                throw new UnauthorizedAccessException();

            await Delete(dbEntity, executionUser, soft);
        }

        virtual public async Task DeleteRange<TExecutionUser>(IEnumerable<int> ids, TExecutionUser executionUser = null, bool soft = true) where TExecutionUser : class
        {
            var entities = DbSet.Where(x => ids.Contains(x.Id)).ToList();

            if (entities?.Any(x => !IsAuthorizedToDelete(x, executionUser)) ?? false)
                throw new UnauthorizedAccessException();

            if (soft)
            {
                entities?.ForEach(x => x.IsDeleted = true);
                DbSet.UpdateRange(entities);
            }
            else
            {
                DbSet.RemoveRange(entities);
            }
            await SaveChanges();
        }

        virtual public async Task Delete<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null, bool soft = true) where TExecutionUser : class
        {
            var dbEntity = DbSet.First(x => x.Id.Equals(entity.Id));

            if (dbEntity == null)
                throw new NullReferenceException();

            if (!IsAuthorizedToDelete(dbEntity, executionUser))
                throw new UnauthorizedAccessException();

            if (soft)
            {
                dbEntity.IsDeleted = true;
            }
            else
            {
                DbSet.Remove(dbEntity);
            }
            await SaveChanges();
        }

        virtual public async Task DeleteRange<TExecutionUser>(IEnumerable<TEntity> entities1, TExecutionUser executionUser = null, bool soft = true) where TExecutionUser : class
        {
            var entities = DbSet.Where(x => entities1.Select(x => x.Id).Contains(x.Id)).ToList();

            if (entities?.Any(x => !IsAuthorizedToDelete(x, executionUser)) ?? false)
                throw new UnauthorizedAccessException();

            if (soft)
            {
                entities?.ForEach(x => x.IsDeleted = true);
                DbSet.UpdateRange(entities);
            }
            else
            {
                DbSet.RemoveRange(entities);
            }
            await SaveChanges();
        }

        #endregion

        #region UnDelete

        virtual public async Task<TEntityDTO> UnDelete<TExecutionUser>(int id, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var dbEntity = DbSet.First(x => x.Id.Equals(id));

            if (dbEntity == null)
                throw new NullReferenceException();

            if (!IsAuthorizedToDelete(dbEntity, executionUser))
                throw new UnauthorizedAccessException();

            dbEntity.IsDeleted = false;
            await SaveChanges();
            return Mapper.Map<TEntityDTO>(dbEntity);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> UnDeleteRange<TExecutionUser>(IEnumerable<int> ids, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var entities = DbSet.Where(x => ids.Contains(x.Id)).ToList();

            if (entities?.Any(x => !IsAuthorizedToDelete(x, executionUser)) ?? false)
                throw new UnauthorizedAccessException();

            entities?.ForEach(x => x.IsDeleted = false);

            await SaveChanges();
            return Mapper.Map<IEnumerable<TEntityDTO>>(entities);
        }

        virtual public async Task<TEntityDTO> UnDelete<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var dbEntity = DbSet.First(x => x.Id.Equals(entity.Id));

            if (dbEntity == null)
                throw new NullReferenceException();

            if (!IsAuthorizedToDelete(dbEntity, executionUser))
                throw new UnauthorizedAccessException();

            dbEntity.IsDeleted = false;
            await SaveChanges();
            return Mapper.Map<TEntityDTO>(dbEntity);
        }

        virtual public async Task<IEnumerable<TEntityDTO>> UnDeleteRange<TExecutionUser>(IEnumerable<TEntity> entities1, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var entities = DbSet.Where(x => entities1.Select(x => x.Id).Contains(x.Id)).ToList();

            if (entities?.Any(x => !IsAuthorizedToDelete(x, executionUser)) ?? false)
                throw new UnauthorizedAccessException();

            entities?.ForEach(x => x.IsDeleted = false);

            await SaveChanges();
            return Mapper.Map<IEnumerable<TEntityDTO>>(entities);
        }

        #endregion

        #endregion

        #region Helpers

        virtual public async Task SaveChanges()
        {
            await BaseDatabaseContext.SaveChangesAsync();
        }

        public bool IsAuthorizedToAdd<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var authorizationResolver = ServiceProvider.GetService<IAuthorizationResolver<TEntity, TExecutionUser>>();
            return authorizationResolver?.IsAuthorizedToAdd(ServiceProvider, entity, executionUser) ?? true;
        }

        public bool IsAuthorizedToDelete<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var authorizationResolver = ServiceProvider.GetService<IAuthorizationResolver<TEntity, TExecutionUser>>();
            return authorizationResolver?.IsAuthorizedToDelete(ServiceProvider, entity, executionUser) ?? true;
        }

        public bool IsAuthorizedToUpdate<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var authorizationResolver = ServiceProvider.GetService<IAuthorizationResolver<TEntity, TExecutionUser>>();
            return authorizationResolver?.IsAuthorizedToUpdate(ServiceProvider, entity, executionUser) ?? true;
        }

        #endregion
    }
}
