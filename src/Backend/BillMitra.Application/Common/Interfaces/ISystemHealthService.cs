using BillMitra.Contracts.System;

namespace BillMitra.Application.Common.Interfaces;

public interface ISystemHealthService
{
    Task<SystemHealthDto> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}
