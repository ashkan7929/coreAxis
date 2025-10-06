using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.ApiManager.Domain;

namespace CoreAxis.Modules.ApiManager.Application.Contracts;

public interface IAuthSchemeHandler
{
    SecurityType SupportedType { get; }
    Task ApplyAsync(HttpRequestMessage request, SecurityProfile profile, CancellationToken cancellationToken);
}