using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.ApiManager.Application.Commands;

public record TestApiMethodCommand(
    Guid MethodId,
    Dictionary<string, object> Parameters
) : IRequest<TestApiMethodResult>;

public class TestApiMethodResult
{
    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public long LatencyMs { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public string? Logs { get; set; }
}

public class TestApiMethodCommandHandler : IRequestHandler<TestApiMethodCommand, TestApiMethodResult>
{
    private readonly DbContext _dbContext;
    private readonly IApiProxy _apiProxy;
    private readonly ILogger<TestApiMethodCommandHandler> _logger;

    public TestApiMethodCommandHandler(
        DbContext dbContext,
        IApiProxy apiProxy,
        ILogger<TestApiMethodCommandHandler> logger)
    {
        _dbContext = dbContext;
        _apiProxy = apiProxy;
        _logger = logger;
    }

    public async Task<TestApiMethodResult> Handle(TestApiMethodCommand request, CancellationToken cancellationToken)
    {
        var result = new TestApiMethodResult();
        var logs = new List<string>();

        var method = await _dbContext.Set<WebServiceMethod>()
            .Include(m => m.WebService)
            .FirstOrDefaultAsync(m => m.Id == request.MethodId, cancellationToken);

        if (method == null)
        {
            result.Success = false;
            result.ValidationErrors.Add("Method not found");
            return result;
        }

        // 1. Validate Request Schema
        if (!string.IsNullOrEmpty(method.RequestSchema))
        {
            logs.Add("Validating request against schema...");
            try
            {
                var schema = await JsonSchema.FromJsonAsync(method.RequestSchema, cancellationToken);
                var requestJson = JsonSerializer.Serialize(request.Parameters);
                var errors = schema.Validate(requestJson);

                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        result.ValidationErrors.Add($"Request Validation Error: {error.Path}: {error.Kind}");
                        logs.Add($"Request Validation Error: {error.Path}: {error.Kind}");
                    }
                }
                else
                {
                    logs.Add("Request validation passed.");
                }
            }
            catch (Exception ex)
            {
                logs.Add($"Request validation failed with exception: {ex.Message}");
                result.ValidationErrors.Add($"Schema validation error: {ex.Message}");
            }
        }

        if (result.ValidationErrors.Count > 0)
        {
            result.Success = false;
            result.Logs = string.Join("\n", logs);
            return result;
        }

        // 2. Invoke
        logs.Add($"Invoking {method.HttpMethod} {method.Path}...");
        try
        {
            var proxyResult = await _apiProxy.InvokeAsync(request.MethodId, request.Parameters, null, null, cancellationToken);
            
            result.Success = proxyResult.IsSuccess;
            result.StatusCode = proxyResult.StatusCode;
            result.ResponseBody = proxyResult.ResponseBody;
            result.LatencyMs = proxyResult.LatencyMs;

            if (!proxyResult.IsSuccess)
            {
                logs.Add($"Invocation failed: {proxyResult.ErrorMessage}");
            }
            else
            {
                logs.Add("Invocation successful.");
                
                // 3. Validate Response Schema
                if (!string.IsNullOrEmpty(method.ResponseSchema) && !string.IsNullOrEmpty(result.ResponseBody))
                {
                    logs.Add("Validating response against schema...");
                    try
                    {
                        var schema = await JsonSchema.FromJsonAsync(method.ResponseSchema, cancellationToken);
                        var errors = schema.Validate(result.ResponseBody);

                        if (errors.Count > 0)
                        {
                            foreach (var error in errors)
                            {
                                var msg = $"Response Validation Error: {error.Path}: {error.Kind}";
                                result.ValidationErrors.Add(msg);
                                logs.Add(msg);
                            }
                        }
                        else
                        {
                            logs.Add("Response validation passed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        logs.Add($"Response validation failed with exception: {ex.Message}");
                        // We don't fail the request if response validation fails, just log it?
                        // Or maybe we should add it to validation errors but keep Success=true?
                        // The prompt says "invalid payload returns validation errors", which usually means request.
                        // For response, "Test endpoint returns response and call log".
                        // So I will just add to ValidationErrors but keep Success as per proxyResult.
                        result.ValidationErrors.Add($"Response schema validation error: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ValidationErrors.Add(ex.Message);
            logs.Add($"Exception: {ex.Message}");
        }

        result.Logs = string.Join("\n", logs);
        return result;
    }
}
