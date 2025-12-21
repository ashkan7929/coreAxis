using CoreAxis.Modules.ProductOrderModule.Application.Flows.Mehraban;
using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Connectors;
using CoreAxis.Modules.ProductOrderModule.Application.DTOs.Quotes;
using Moq;
using Xunit;

using Microsoft.Extensions.Logging;

namespace CoreAxis.Tests.ProductOrderModule;

public class MehrabanQuoteFlowTests
{
    private readonly Mock<IRiskConnector> _riskConnectorMock;
    private readonly Mock<IFanavaranConnector> _fanavaranConnectorMock;
    private readonly Mock<ILogger<MehrabanQuoteFlow>> _loggerMock;
    private readonly MehrabanQuoteFlow _flow;

    public MehrabanQuoteFlowTests()
    {
        _riskConnectorMock = new Mock<IRiskConnector>();
        _fanavaranConnectorMock = new Mock<IFanavaranConnector>();
        _loggerMock = new Mock<ILogger<MehrabanQuoteFlow>>();
        _flow = new MehrabanQuoteFlow(_riskConnectorMock.Object, _fanavaranConnectorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CalculateQuoteAsync_ShouldReturnCorrectPremium_GivenMockedRisk()
    {
        // Arrange
        _riskConnectorMock.Setup(x => x.CalculateRiskAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1.2m); // 20% load

        var appData = "{}";

        // Act
        var result = await _flow.CalculateQuoteAsync(appData, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1200000m, result.FinalPremium); // 1,000,000 * 1.2
        Assert.NotEmpty(result.Blocks);
        
        var priceBlock = result.Blocks.OfType<PriceBreakdownBlock>().FirstOrDefault();
        Assert.NotNull(priceBlock);
        Assert.Equal(1200000m, priceBlock.Total);
    }
}
