using Nexos_Network_System.Models.Enums;
using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Interfaces;

public interface IConnectionService
{
    Task<int> CreateFromOrderAsync(ConnectionCreateViewModel model, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int connectionId, ConnectionStatus status, string? notes, CancellationToken cancellationToken = default);
}
