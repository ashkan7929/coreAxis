using CoreAxis.Modules.DynamicForm.Application.DTOs;
using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PricingController : ControllerBase
    {
        private readonly IFormRepository _formRepository;
        private readonly IValidationEngine _validationEngine;
        private readonly IFormulaService _formulaService;
        private readonly IDataOrchestrator _dataOrchestrator;
        private readonly IRoundingPolicy _roundingPolicy;
        private readonly ILogger<PricingController> _logger;

        public PricingController(
            IFormRepository formRepository,
            IValidationEngine validationEngine,
            IFormulaService formulaService,
            IDataOrchestrator dataOrchestrator,
            IRoundingPolicy roundingPolicy,
            ILogger<PricingController> logger)
        {
            _formRepository = formRepository ?? throw new ArgumentNullException(nameof(formRepository));
            _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _dataOrchestrator = dataOrchestrator ?? throw new ArgumentNullException(nameof(dataOrchestrator));
            _roundingPolicy = roundingPolicy ?? throw new ArgumentNullException(nameof(roundingPolicy));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Calculates pricing using form data and a formula.
        /// </summary>
        /// <param name="request">Pricing calculation input.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Pricing calculation result.</returns>
        [HttpPost("calculate")]
        [ProducesResponseType(typeof(PricingResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CalculatePricing(
            [FromBody] PricingCalculationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var correlationId = HttpContext.GetCorrelationId();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    HttpContext.Response.Headers["X-Correlation-Id"] = correlationId;
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Code} Invalid model | CorrelationId={CorrelationId}", "PRC_INVALID_FORM", correlationId);
                    var vpd = new ValidationProblemDetails(ModelState)
                    {
                        Title = "Invalid form data",
                        Detail = "Input did not pass validation rules",
                        Type = "https://errors.coreaxis.dev/pricing/PRC_INVALID_FORM",
                        Status = StatusCodes.Status400BadRequest,
                    };
                    vpd.Extensions["correlationId"] = correlationId;
                    return ValidationProblem(vpd);
                }

                // Load form with fields
                var form = await _formRepository.GetByIdWithIncludesAsync(request.FormId, includeFields: true, includeSubmissions: false, cancellationToken);
                if (form == null)
                {
                    _logger.LogWarning("{Code} Form not found | CorrelationId={CorrelationId} | FormId={FormId}", "PRC_INVALID_FORM", correlationId, request.FormId);
                    var pd = new ProblemDetails
                    {
                        Title = "Form not found",
                        Detail = $"Form with id '{request.FormId}' not found",
                        Status = StatusCodes.Status404NotFound,
                        Type = "https://errors.coreaxis.dev/pricing/PRC_INVALID_FORM"
                    };
                    pd.Extensions["correlationId"] = correlationId;
                    return new ObjectResult(pd) { StatusCode = pd.Status };
                }

                // Map fields to validation definitions
                var fieldDefs = MapToFieldDefinitions(form.Fields);

                // Validate input data against form schema
                var culture = string.IsNullOrWhiteSpace(request.Locale) ? CultureInfo.InvariantCulture : new CultureInfo(request.Locale);
                var validation = await _validationEngine.ValidateAsync(request.FormData, fieldDefs, culture);
                if (!validation.IsValid)
                {
                    var errors = new Dictionary<string, string[]>();
                    foreach (var kv in validation.FieldResults)
                    {
                        if (!kv.Value.IsValid)
                        {
                            errors[kv.Key] = kv.Value.Errors.Select(e => e.Message).ToArray();
                        }
                    }

                    // Include any form-level errors
                    if (validation.FormErrors?.Any() == true)
                    {
                        errors["form"] = validation.FormErrors.Select(e => e.Message).ToArray();
                    }

                    var vpd = new ValidationProblemDetails(errors)
                    {
                        Title = "Invalid form data",
                        Detail = "Input did not pass validation rules",
                        Type = "https://errors.coreaxis.dev/pricing/PRC_INVALID_FORM",
                        Status = StatusCodes.Status400BadRequest,
                    };
                    vpd.Extensions["correlationId"] = correlationId;
                    return ValidationProblem(vpd);
                }

                // Prepare evaluation context and fetch external data if requested
                var evalContext = new Dictionary<string, object?>
                {
                    ["formId"] = form.Id,
                    ["productId"] = request.ProductId,
                    ["currency"] = request.Currency,
                    ["externalDataSources"] = request.ExternalDataSources ?? new Dictionary<string, object?>()
                };

                var externalDataRes = await _dataOrchestrator.GetExternalDataAsync(evalContext, cancellationToken);
                var externalData = externalDataRes.IsSuccess ? externalDataRes.Value.Data : new Dictionary<string, object?>();
                if (!externalDataRes.IsSuccess && (request.ExternalDataSources?.Count > 0))
                {
                    _logger.LogError("{Code} Datasource fetch failed | CorrelationId={CorrelationId} | Error={Error}", "PRC_DATASOURCE_FAIL", correlationId, externalDataRes.Error);
                    var pd = new ProblemDetails
                    {
                        Title = "Datasource fetch failed",
                        Detail = externalDataRes.Error ?? "Failed to fetch external data",
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://errors.coreaxis.dev/pricing/PRC_DATASOURCE_FAIL"
                    };
                    pd.Extensions["correlationId"] = correlationId;
                    return new ObjectResult(pd) { StatusCode = pd.Status };
                }

                // Combine form data and external data into inputs for formula
                var inputs = new Dictionary<string, object>();
                foreach (var kv in request.FormData)
                {
                    if (kv.Value is not null)
                        inputs[kv.Key] = kv.Value!;
                }
                foreach (var kv in externalData)
                {
                    if (kv.Value is not null)
                        inputs[$"ext_{kv.Key}"] = kv.Value!;
                }

                // Evaluate formula (by name/version if provided)
                Result<FormulaEvaluationResult> evalResult;
                if (!string.IsNullOrWhiteSpace(request.FormulaName))
                {
                    evalResult = await _formulaService.EvaluateFormulaAsync(
                        request.FormulaName!,
                        request.Version,
                        inputs,
                        ToNonNullContext(evalContext),
                        cancellationToken);
                }
                else
                {
                    // If no formulaName provided, attempt to use a calculated field expression from the form
                    // Fallback: return bad request if neither provided
                    _logger.LogWarning("{Code} No formula specified | CorrelationId={CorrelationId}", "PRC_NO_FORMULA", correlationId);
                    var pd = new ProblemDetails
                    {
                        Title = "Formula name required",
                        Detail = "Provide FormulaName or configure formula resolution via form",
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://errors.coreaxis.dev/pricing/PRC_NO_FORMULA"
                    };
                    pd.Extensions["correlationId"] = correlationId;
                    return new ObjectResult(pd) { StatusCode = pd.Status };
                }

                if (!evalResult.IsSuccess)
                {
                    _logger.LogError("{Code} Eval failed | CorrelationId={CorrelationId} | Error={Error}", "PRC_EVAL_ERROR", correlationId, evalResult.Error);
                    var pd = new ProblemDetails
                    {
                        Title = "Formula evaluation failed",
                        Detail = evalResult.Error,
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://errors.coreaxis.dev/pricing/PRC_EVAL_ERROR"
                    };
                    pd.Extensions["correlationId"] = correlationId;
                    return new ObjectResult(pd) { StatusCode = pd.Status };
                }

                var valueDecimal = TryToDecimal(evalResult.Value.Value);
                if (valueDecimal is null)
                {
                    _logger.LogError("{Code} Non-numeric result | CorrelationId={CorrelationId}", "PRC_EVAL_ERROR", correlationId);
                    var pd = new ProblemDetails
                    {
                        Title = "Non-numeric formula result",
                        Detail = "Formula result could not be converted to decimal",
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://errors.coreaxis.dev/pricing/PRC_EVAL_ERROR"
                    };
                    pd.Extensions["correlationId"] = correlationId;
                    return new ObjectResult(pd) { StatusCode = pd.Status };
                }

                var pricingDto = new PricingResultDto
                {
                    FinalPrice = _roundingPolicy.NormalizeMoney(valueDecimal.Value, 2, RoundingMode.Bankers),
                    FormulaVersion = evalResult.Value.FormulaVersion,
                    Currency = request.Currency,
                    UsedData = new Dictionary<string, object?>()
                };

                // Breakdown from evaluation metadata (best-effort)
                if (evalResult.Value.Metadata?.Count > 0)
                {
                    foreach (var m in evalResult.Value.Metadata)
                    {
                        pricingDto.Breakdown.Add(new PricingBreakdownItem { Key = m.Key, Value = m.Value });
                    }
                }

                // Used data snapshot
                foreach (var kv in request.FormData)
                {
                    pricingDto.UsedData[kv.Key] = kv.Value;
                }
                if (externalData.Count > 0)
                {
                    pricingDto.UsedData["external"] = externalData;
                }

                sw.Stop();
                _logger.LogInformation("{Code} Pricing calculated | CorrelationId={CorrelationId} | LatencyMs={Latency}", "PRC_OK", correlationId, sw.ElapsedMilliseconds);
                return Ok(pricingDto);
            }
            catch (Exception ex)
            {
                var correlationId = HttpContext.GetCorrelationId();
                _logger.LogError(ex, "{Code} Unexpected error | CorrelationId={CorrelationId}", "PRC_UNEXPECTED_ERROR", correlationId);
                var pd = new ProblemDetails
                {
                    Title = "Unexpected error",
                    Detail = "An unexpected error occurred while calculating pricing",
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://errors.coreaxis.dev/pricing/PRC_UNEXPECTED_ERROR"
                };
                pd.Extensions["correlationId"] = correlationId;
                return new ObjectResult(pd) { StatusCode = pd.Status };
            }
        }

        private static Dictionary<string, object> ToNonNullContext(Dictionary<string, object?> ctx)
        {
            var result = new Dictionary<string, object>();
            foreach (var kv in ctx)
            {
                if (kv.Value is not null)
                {
                    result[kv.Key] = kv.Value!;
                }
            }
            return result;
        }

        private static decimal? TryToDecimal(object value)
        {
            try
            {
                return value switch
                {
                    null => null,
                    decimal d => d,
                    double db => Convert.ToDecimal(db),
                    float f => Convert.ToDecimal(f),
                    int i => i,
                    long l => l,
                    string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        private static List<FieldDefinition> MapToFieldDefinitions(IEnumerable<FormField> fields)
        {
            var list = new List<FieldDefinition>();
            foreach (var f in fields.OrderBy(ff => ff.Order))
            {
                var def = new FieldDefinition
                {
                    Name = f.Name,
                    Type = f.FieldType,
                    Label = f.Label,
                    IsRequired = f.IsRequired,
                    Metadata = new Dictionary<string, object?>()
                };

                // Parse JSON payloads best-effort
                if (!string.IsNullOrWhiteSpace(f.ValidationRules))
                {
                    try
                    {
                        var rules = JsonSerializer.Deserialize<List<ValidationRule>>(f.ValidationRules);
                        if (rules != null)
                            def.ValidationRules = rules;
                    }
                    catch { /* ignore malformed JSON */ }
                }
                if (!string.IsNullOrWhiteSpace(f.Options))
                {
                    try
                    {
                        var options = JsonSerializer.Deserialize<List<FieldOption>>(f.Options);
                        if (options != null)
                            def.Options = options;
                    }
                    catch { /* ignore malformed JSON */ }
                }
                if (!string.IsNullOrWhiteSpace(f.ConditionalLogic))
                {
                    try
                    {
                        var cond = JsonSerializer.Deserialize<Dictionary<string, string?>>(f.ConditionalLogic);
                        if (cond != null)
                        {
                            cond.TryGetValue("visibility", out var vis);
                            cond.TryGetValue("enabled", out var en);
                            cond.TryGetValue("required", out var req);
                            def.VisibilityExpression = vis;
                            def.EnabledExpression = en;
                            def.RequiredExpression = req;
                        }
                    }
                    catch { /* ignore malformed JSON */ }
                }

                list.Add(def);
            }
            return list;
        }
    }
}