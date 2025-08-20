namespace CoreAxis.Modules.MLMModule.Application.Contracts;

public class GetCommissionsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}