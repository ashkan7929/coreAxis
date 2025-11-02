using CoreAxis.Modules.ProductOrderModule.Application.DTOs.ThirdParty;
using CoreAxis.Modules.ProductOrderModule.Application.Queries.ThirdParty;
using MediatR;
using System.Net.Http.Json;

namespace CoreAxis.Modules.ProductOrderModule.Application.Handlers.ThirdParty;

public class GetMilliPriceQueryHandler : IRequestHandler<GetMilliPriceQuery, MilliPriceResponseDto>
{
    private readonly HttpClient _httpClient;

    public GetMilliPriceQueryHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MilliPriceResponseDto> Handle(GetMilliPriceQuery request, CancellationToken cancellationToken)
    {
        var url = "https://milli.gold/api/v1/public/milli-price/external";

        var response = await _httpClient.GetFromJsonAsync<MilliPriceResponseDto>(url, cancellationToken);

        if (response is null)
            throw new Exception("Failed to fetch milli price data.");

        return response;
    }
}
