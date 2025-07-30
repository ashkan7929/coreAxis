namespace CoreAxis.Modules.AuthModule.Application.DTOs;

public class AccessLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public DateTime Timestamp { get; set; }

}

public class CreateAccessLogDto
{
    public Guid? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }

}

public class AccessLogFilterDto
{
    public Guid? UserId { get; set; }
    public string? Action { get; set; }
    public bool? IsSuccess { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public int PageSize { get; set; } = 50;
    public int PageNumber { get; set; } = 1;
}