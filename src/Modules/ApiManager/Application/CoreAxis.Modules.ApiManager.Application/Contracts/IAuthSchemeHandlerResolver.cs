using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.ApiManager.Domain;

namespace CoreAxis.Modules.ApiManager.Application.Contracts;

public interface IAuthSchemeHandlerResolver
{
    Task ApplyAsync(HttpRequestMessage request, SecurityProfile profile, CancellationToken cancellationToken);
}