using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace HyperQL
{
    public class ReadServiceBase<TEntity, TSearchRequest, TSearchResponse, TEntityDTO>
              : IReadServiceBase<TEntity, TSearchRequest, TSearchResponse, TEntityDTO>
        where TEntity : EntityBase,
        new() where TSearchRequest : SearchRequestBase,
        new() where TSearchResponse : SearchResponseBase<TSearchRequest, TEntityDTO>,
        new() where TEntityDTO : class
    {
        protected IServiceProvider ServiceProvider { get; }
        protected IMapper Mapper { get; }
        protected DbSet<TEntity> DbSet { get; set; }
        protected IQueryable<TEntity> Query { get; set; }
        protected DbContext BaseDatabaseContext;
        protected DbConnection DatabaseConnection { get; set; }

        public ReadServiceBase(IServiceProvider serviceProvider, DbContext dbContext)
        {
            ServiceProvider = serviceProvider;

            Mapper = serviceProvider.GetService<IMapper>();
            BaseDatabaseContext = dbContext;

            DbSet = BaseDatabaseContext.Set<TEntity>();
            Query = DbSet.AsQueryable();
            DatabaseConnection = BaseDatabaseContext.Database.GetDbConnection();
        }

        #region Inits

        virtual public async Task<TSearchRequest> InitForGet()
        {
            return new TSearchRequest();
        }

        private T InitForGet<T>(T searchRequestSubObject, Type parentType)
        {
            var excludeClassTypes = new List<Type>
            {
                typeof(string),
                typeof(IEnumerable<>),
                typeof(List<>)
            };

            var props = searchRequestSubObject.GetType().GetProperties().ToList();

            foreach (var prop in props)
            {
                if (prop.PropertyType.IsClass && !excludeClassTypes.Contains(prop.PropertyType) && prop.PropertyType != parentType && prop.Name != "Items")
                {
                    var value = prop.GetValue(searchRequestSubObject);
                    if (value == null)
                    {
                        value = Activator.CreateInstance(prop.PropertyType);
                        value = InitForGet(value, parentType);

                        prop.SetValue(searchRequestSubObject, value);
                    }
                }
            }

            return searchRequestSubObject;
        }

        #endregion

        #region Regular

        virtual public async Task<TEntityDTO> GetById(int id, TSearchRequest searchRequest = null)
        {
            searchRequest = searchRequest ?? new TSearchRequest();

            if (searchRequest.Id == 0)
                searchRequest.Id = id;

            if (searchRequest.Id == 0)
                return null;

            return (await GetItems(searchRequest))?.FirstOrDefault();
        }

        virtual public async Task<TSearchResponse> Get(TSearchRequest searchRequest = null)
        {
            return new TSearchResponse
            {
                SearchRequest = searchRequest,
                Items = await GetItems(searchRequest)
            };
        }

        virtual public async Task<List<TEntityDTO>> GetItems(TSearchRequest searchRequest = null)
        {
            var items = await GetItemsEntities(searchRequest);
            return Mapper.Map<List<TEntityDTO>>(items);
        }

        private async Task<List<TEntity>> GetItemsEntities(TSearchRequest searchRequest = null)
        {
            await SetParameters(searchRequest);
            return Query.ToList();
        }

        virtual public async Task SetParameters(TSearchRequest searchRequest = null)
        {
            Query = Query
                .Where(x => (searchRequest != null && searchRequest.IsDeleted == null) || !x.IsDeleted)
                .Where(searchRequest)
                .Include(searchRequest)
                .OrderBy(searchRequest?.Pagination?.OrderFields);

            if (searchRequest?.Pagination != null)
            {
                if (!(searchRequest.Pagination.ShouldTakeAllRecords ?? false))
                {
                    if (searchRequest.Pagination.Skip.GetValueOrDefault() != 0)
                        Query = Query.Skip(searchRequest.Pagination.Skip.Value);
                    if (searchRequest.Pagination.Take.GetValueOrDefault() != 0)
                        Query = Query.Take(searchRequest.Pagination.Take.Value);
                }

                searchRequest.Pagination.TotalNumberOfRecords = Query.Count();
            }
        }

        #endregion

        #region With record level authorization

        virtual public async Task<TEntityDTO> GetById<TExecutionUser>(int id, TExecutionUser executionUser = null, TSearchRequest searchRequest = null) where TExecutionUser : class
        {
            searchRequest = searchRequest ?? new TSearchRequest();

            if (searchRequest.Id == 0)
                searchRequest.Id = id;

            if (searchRequest.Id == 0)
                return null;

            return (await GetItems(searchRequest, executionUser))?.FirstOrDefault();
        }

        virtual public async Task<TSearchResponse> Get<TExecutionUser>(TSearchRequest searchRequest = null, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            return new TSearchResponse
            {
                SearchRequest = searchRequest,
                Items = await GetItems(searchRequest, executionUser)
            };
        }

        virtual public async Task<List<TEntityDTO>> GetItems<TExecutionUser>(TSearchRequest searchRequest = null, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            var items = await GetItemsEntities(searchRequest);

            if (!IsAuthorizedToGet(items, executionUser))
                throw new UnauthorizedAccessException();

            return Mapper.Map<List<TEntityDTO>>(items);
        }

        #endregion

        #region Helpers

        private bool IsAuthorizedToGet<TExecutionUser>(IEnumerable<object> entities, TExecutionUser executionUser = null, object grandparent = null, object parent = null) where TExecutionUser : class
        {
            return executionUser == null || (entities?.All(entity => IsAuthorizedToGetSingle(entity, executionUser, grandparent, parent)) ?? true);
        }

        private bool IsAuthorizedToGetSingle<TExecutionUser>(object entity, TExecutionUser executionUser = null, object grandparent = null, object parent = null) where TExecutionUser : class
        {
            if (entity == null) return true;

            if (executionUser == null) return true;

            if (!InvokeAuthorizationMethod<TExecutionUser>(entity.GetType(), nameof(IsRecordOwner), new object[] { ServiceProvider, entity, executionUser }))
                return false;

            var props = entity.GetType().GetProperties();

            foreach (var prop in props)
            {
                if (AreEqualEntities(grandparent, prop.GetValue(entity)))
                    continue;

                if (!InvokeAuthorizationMethod<TExecutionUser>(prop.PropertyType, nameof(IsAuthorizedToGet), new object[] { ServiceProvider, prop.GetValue(entity), executionUser }))
                    return false;

                if (prop.GetValue(entity) is IEnumerable<object> && !IsAuthorizedToGet(prop.GetValue(entity) as IEnumerable<object>, executionUser, parent, entity))
                    return false;
            }

            return true;
        }

        private bool IsRecordOwner<TExecutionUser>(TEntity entity, TExecutionUser executionUser = null) where TExecutionUser : class
        {
            if (executionUser == null)
                return true;

            var authorizationResolver = ServiceProvider.GetService<IAuthorizationResolver<TEntity, TExecutionUser>>();

            return authorizationResolver?.IsRecordOwner(ServiceProvider, entity, executionUser) ?? true;
        }

        private bool AreEqualEntities(object grandparent, object entity)
        {
            if (grandparent == null || entity == null)
                return false;

            if (!(typeof(EntityBase).IsAssignableFrom(grandparent.GetType()) && typeof(EntityBase).IsAssignableFrom(entity.GetType())))
                return false;

            return (((EntityBase)grandparent).Id == ((EntityBase)entity).Id);
        }

        private bool InvokeAuthorizationMethod<TExecutionUser>(Type entityType, string methodName, object?[]? parameters) where TExecutionUser : class
        {
            var authorizationResolverTypeForEntity = typeof(IAuthorizationResolver<,>).MakeGenericType(new Type[] { entityType, typeof(TExecutionUser) });

            var authorizationResolvereForEntityMethod = authorizationResolverTypeForEntity.GetMethod(methodName);

            var authorizationResolvereForSubEntity = ServiceProvider.GetService(authorizationResolverTypeForEntity);

            return authorizationResolvereForEntityMethod?.Invoke(authorizationResolvereForSubEntity, parameters) as bool? ?? false;
        }

        #endregion
    }
}
