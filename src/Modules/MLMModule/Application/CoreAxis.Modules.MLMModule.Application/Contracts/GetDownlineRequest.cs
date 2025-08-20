namespace CoreAxis.Modules.MLMModule.Application.Contracts;

public class GetDownlineRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int MaxDepth { get; set; } = 5;
}