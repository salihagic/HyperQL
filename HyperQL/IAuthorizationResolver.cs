using System;

namespace HyperQL
{
    public interface IAuthorizationResolver<TEntity, TExecutionUser> 
        where TEntity : EntityBase,
        new() where TExecutionUser : class
    {
        bool IsRecordOwner(IServiceProvider serviceProvider, TEntity entity, TExecutionUser executionUser = null) => true;
        bool IsAuthorizedToAdd(IServiceProvider serviceProvider, TEntity entity, TExecutionUser executionUser = null) => IsRecordOwner(serviceProvider, entity, executionUser);
        bool IsAuthorizedToDelete(IServiceProvider serviceProvider, TEntity entity, TExecutionUser executionUser = null) => IsRecordOwner(serviceProvider, entity, executionUser);
        bool IsAuthorizedToGet(IServiceProvider serviceProvider, TEntity entity, TExecutionUser executionUser = null) => IsRecordOwner(serviceProvider, entity, executionUser);
        bool IsAuthorizedToUpdate(IServiceProvider serviceProvider, TEntity entity, TExecutionUser executionUser = null) => IsRecordOwner(serviceProvider, entity, executionUser);
    }
}
