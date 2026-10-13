using Nexos_Network_System.ViewModels;

namespace Nexos_Network_System.Interfaces;

public interface IAdminSearchService
{
    Task<SearchViewModel> SearchAsync(SearchViewModel model, CancellationToken cancellationToken = default);
}
