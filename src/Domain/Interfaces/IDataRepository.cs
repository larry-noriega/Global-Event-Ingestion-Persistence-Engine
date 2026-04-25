using System;
using System.Threading.Tasks;

namespace EventEngine.Api.Domain.Interfaces;

public interface IDataRepository<TEntity>
{
    // Strategy for atomic persistence under heavy load
    Task SaveWithIntegrityAsync(TEntity entity);

    // Verifies record consistency across distributed nodes
    Task<bool> VerifyConsistencyAsync(Guid correlationId);
}

