using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Secure expression evaluation engine with limited DSL support.
    /// </summary>
    public class ExpressionEngine : IExpressionEngine
    {
        private readonly ILogger<ExpressionEngine> _logger;
        private readonly Dictionary<string, Func<object[], object>> _functions;
        private readonly HashSet<string> _allowedOperators;
        private readonly HashSet<string> _securityBlacklist;

        public ExpressionEngine(ILogger<ExpressionEngine> logger)
        {
            _logger = logger;
            _functions = InitializeFunctions();
            _allowedOperators = InitializeOperators();
            _securityBlacklist = InitializeSecurityBlacklist();
        }

        public async Task<ExpressionEvaluationResult> EvaluateAsync(
            FormulaExpression expression,
            ExpressionEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Security validation
                if (!IsSafeExpression(expression.Expression))
                {
                    return ExpressionEvaluationResult.Failure(
                        "Expression contains unsafe operations",
                        stopwatch.ElapsedMilliseconds);
                }

                // Timeout protection
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(context.MaxExecutionTime);

                var result = await Task.Run(() => EvaluateExpressionInternal(
                    expression.Expression, context), timeoutCts.Token);

                stopwatch.Stop();

                return ExpressionEvaluationResult.Success(
                    result.Value,
                    result.Type,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return ExpressionEvaluationResult.Failure(
                    "Expression evaluation timed out",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error evaluating expression: {Expression}", expression.Expression);
                return ExpressionEvaluationResult.Failure(
                    $"Evaluation error: {ex.Message}",
                    stopwatch.ElapsedMilliseconds);
            }
        }

        public async Task<bool> EvaluateConditionalAsync(
            ConditionalLogic conditionalLogic,
            ExpressionEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var expression = FormulaExpression.Boolean(conditionalLogic.Expression);
                var result = await EvaluateAsync(expression, context, cancellationToken);
                
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Conditional evaluation failed: {Error}", result.ErrorMessage);
                    return false;
                }

                return Convert.ToBoolean(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating conditional logic: {Expression}", conditionalLogic.Expression);
                return false;
            }
        }

        public ExpressionValidationResult ValidateExpression(
            string expression,
            ValidationContext? validationContext = null)
        {
            var result = new ExpressionValidationResult();

            try
            {
                // Basic syntax validation
                if (string.IsNullOrWhiteSpace(expression))
                {
                    result.Errors.Add("Expression cannot be empty");
                    return result;
                }

                // Security validation
                var securityIssues = ValidateSecurityConstraints(expression);
                if (securityIssues.Any())
                {
                    result.SecurityIssues.AddRange(securityIssues);
                    result.Errors.AddRange(securityIssues);
                    return result;
                }

                // Syntax validation
                var syntaxErrors = ValidateSyntax(expression);
                if (syntaxErrors.Any())
                {
                    result.Errors.AddRange(syntaxErrors);
                    return result;
                }

                // Variable validation
                if (validationContext != null)
                {
                    var variableErrors = ValidateVariables(expression, validationContext);
                    if (variableErrors.Any())
                    {
                        result.Errors.AddRange(variableErrors);
                        return result;
                    }
                }

                result.IsValid = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating expression: {Expression}", expression);
                result.Errors.Add($"Validation error: {ex.Message}");
                return result;
            }
        }

        public IEnumerable<FunctionSignature> GetAvailableFunctions()
        {
            return new List<FunctionSignature>
            {
                new FunctionSignature("IF", typeof(object), new[] { typeof(bool), typeof(object), typeof(object) }),
                new FunctionSignature("AND", typeof(bool), new[] { typeof(bool), typeof(bool) }),
                new FunctionSignature("OR", typeof(bool), new[] { typeof(bool), typeof(bool) }),
                new FunctionSignature("NOT", typeof(bool), new[] { typeof(bool) }),
                new FunctionSignature("EQUALS", typeof(bool), new[] { typeof(object), typeof(object) }),
                new FunctionSignature("GREATER_THAN", typeof(bool), new[] { typeof(decimal), typeof(decimal) }),
                new FunctionSignature("LESS_THAN", typeof(bool), new[] { typeof(decimal), typeof(decimal) }),
                new FunctionSignature("CONTAINS", typeof(bool), new[] { typeof(string), typeof(string) }),
                new FunctionSignature("LENGTH", typeof(int), new[] { typeof(string) }),
                new FunctionSignature("UPPER", typeof(string), new[] { typeof(string) }),
                new FunctionSignature("LOWER", typeof(string), new[] { typeof(string) }),
                new FunctionSignature("TRIM", typeof(string), new[] { typeof(string) }),
                new FunctionSignature("SUBSTRING", typeof(string), new[] { typeof(string), typeof(int), typeof(int) }),
                new FunctionSignature("CONCAT", typeof(string), new[] { typeof(string), typeof(string) }),
                new FunctionSignature("ADD", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                new FunctionSignature("SUBTRACT", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                new FunctionSignature("MULTIPLY", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                new FunctionSignature("DIVIDE", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                new FunctionSignature("ROUND", typeof(decimal), new[] { typeof(decimal), typeof(int) }),
                new FunctionSignature("ABS", typeof(decimal), new[] { typeof(decimal) }),
                new FunctionSignature("MIN", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                new FunctionSignature("MAX", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                new FunctionSignature("NOW", typeof(DateTime), new Type[0]),
                new FunctionSignature("TODAY", typeof(DateTime), new Type[0]),
                new FunctionSignature("DATE_ADD", typeof(DateTime), new[] { typeof(DateTime), typeof(int), typeof(string) }),
                new FunctionSignature("DATE_DIFF", typeof(int), new[] { typeof(DateTime), typeof(DateTime), typeof(string) }),
                new FunctionSignature("FORMAT_DATE", typeof(string), new[] { typeof(DateTime), typeof(string) }),
                new FunctionSignature("IS_NULL", typeof(bool), new[] { typeof(object) }),
                new FunctionSignature("IS_EMPTY", typeof(bool), new[] { typeof(string) }),
                new FunctionSignature("COALESCE", typeof(object), new[] { typeof(object), typeof(object) })
            };
        }

        public IEnumerable<string> GetSupportedOperators()
        {
            return _allowedOperators;
        }

        public bool IsSafeExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            // Check against security blacklist
            foreach (var blacklistedItem in _securityBlacklist)
            {
                if (expression.Contains(blacklistedItem, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Check for potentially dangerous patterns
            var dangerousPatterns = new[]
            {
                @"\beval\b",
                @"\bexec\b",
                @"\bsystem\b",
                @"\bprocess\b",
                @"\bfile\b",
                @"\bdirectory\b",
                @"\bregistry\b",
                @"\bnetwork\b",
                @"\bsocket\b",
                @"\bhttp\b",
                @"\burl\b",
                @"\breflection\b",
                @"\bassembly\b",
                @"\btype\b",
                @"\bmethod\b",
                @"\bproperty\b",
                @"\bfield\b",
                @"\bconstructor\b",
                @"\.\.\.",
                @"\$\{",
                @"<%",
                @"%>",
                @"<script",
                @"javascript:",
                @"vbscript:",
                @"data:",
                @"\bwhile\s*\(",
                @"\bfor\s*\(",
                @"\bgoto\b",
                @"\bthrow\b",
                @"\btry\b",
                @"\bcatch\b",
                @"\bfinally\b"
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (Regex.IsMatch(expression, pattern, RegexOptions.IgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private (object Value, Type Type) EvaluateExpressionInternal(
            string expression,
            ExpressionEvaluationContext context)
        {
            // Simple expression parser for basic operations
            // This is a simplified implementation - in production, you might want to use a proper parser
            
            expression = expression.Trim();

            // Handle function calls
            if (expression.Contains("(") && expression.Contains(")"))
            {
                return EvaluateFunctionCall(expression, context);
            }

            // Handle variables
            if (expression.StartsWith("$"))
            {
                var variableName = expression.Substring(1);
                var value = context.GetVariable(variableName);
                return (value, value?.GetType() ?? typeof(object));
            }

            // Handle literals
            if (bool.TryParse(expression, out var boolValue))
            {
                return (boolValue, typeof(bool));
            }

            if (decimal.TryParse(expression, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
            {
                return (decimalValue, typeof(decimal));
            }

            if (expression.StartsWith("'") && expression.EndsWith("'"))
            {
                return (expression.Substring(1, expression.Length - 2), typeof(string));
            }

            if (expression.StartsWith("\"") && expression.EndsWith("\""))
            {
                return (expression.Substring(1, expression.Length - 2), typeof(string));
            }

            // Handle simple binary operations
            return EvaluateBinaryOperation(expression, context);
        }

        private (object Value, Type Type) EvaluateFunctionCall(
            string expression,
            ExpressionEvaluationContext context)
        {
            var match = Regex.Match(expression, @"^(\w+)\((.*)\)$");
            if (!match.Success)
            {
                throw new ArgumentException($"Invalid function call syntax: {expression}");
            }

            var functionName = match.Groups[1].Value.ToUpper();
            var argumentsString = match.Groups[2].Value;

            if (!_functions.ContainsKey(functionName))
            {
                throw new ArgumentException($"Unknown function: {functionName}");
            }

            var arguments = ParseArguments(argumentsString, context);
            var result = _functions[functionName](arguments);

            return (result, result?.GetType() ?? typeof(object));
        }

        private (object Value, Type Type) EvaluateBinaryOperation(
            string expression,
            ExpressionEvaluationContext context)
        {
            // Simple binary operation evaluation
            // This is a basic implementation - you might want to use a proper expression parser
            
            var operators = new[] { "==", "!=", ">=", "<=", ">", "<", "+", "-", "*", "/", "&&", "||", "&", "|" };
            
            foreach (var op in operators)
            {
                var index = expression.IndexOf(op);
                if (index > 0)
                {
                    var left = expression.Substring(0, index).Trim();
                    var right = expression.Substring(index + op.Length).Trim();
                    
                    var leftResult = EvaluateExpressionInternal(left, context);
                    var rightResult = EvaluateExpressionInternal(right, context);
                    
                    return EvaluateOperator(op, leftResult.Value, rightResult.Value);
                }
            }

            throw new ArgumentException($"Unable to evaluate expression: {expression}");
        }

        private (object Value, Type Type) EvaluateOperator(string op, object left, object right)
        {
            switch (op)
            {
                case "==":
                    return (Equals(left, right), typeof(bool));
                case "!=":
                    return (!Equals(left, right), typeof(bool));
                case ">":
                    return (CompareValues(left, right) > 0, typeof(bool));
                case "<":
                    return (CompareValues(left, right) < 0, typeof(bool));
                case ">=":
                    return (CompareValues(left, right) >= 0, typeof(bool));
                case "<=":
                    return (CompareValues(left, right) <= 0, typeof(bool));
                case "+":
                    return (Convert.ToDecimal(left) + Convert.ToDecimal(right), typeof(decimal));
                case "-":
                    return (Convert.ToDecimal(left) - Convert.ToDecimal(right), typeof(decimal));
                case "*":
                    return (Convert.ToDecimal(left) * Convert.ToDecimal(right), typeof(decimal));
                case "/":
                    return (Convert.ToDecimal(left) / Convert.ToDecimal(right), typeof(decimal));
                case "&&":
                case "&":
                    return (Convert.ToBoolean(left) && Convert.ToBoolean(right), typeof(bool));
                case "||":
                case "|":
                    return (Convert.ToBoolean(left) || Convert.ToBoolean(right), typeof(bool));
                default:
                    throw new ArgumentException($"Unsupported operator: {op}");
            }
        }

        private int CompareValues(object left, object right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return -1;
            if (right == null) return 1;

            if (left is IComparable leftComparable && right is IComparable rightComparable)
            {
                return leftComparable.CompareTo(rightComparable);
            }

            return string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal);
        }

        private object[] ParseArguments(string argumentsString, ExpressionEvaluationContext context)
        {
            if (string.IsNullOrWhiteSpace(argumentsString))
            {
                return new object[0];
            }

            var arguments = new List<object>();
            var parts = SplitArguments(argumentsString);

            foreach (var part in parts)
            {
                var result = EvaluateExpressionInternal(part.Trim(), context);
                arguments.Add(result.Value);
            }

            return arguments.ToArray();
        }

        private string[] SplitArguments(string argumentsString)
        {
            var arguments = new List<string>();
            var current = "";
            var depth = 0;
            var inString = false;
            var stringChar = '\0';

            for (int i = 0; i < argumentsString.Length; i++)
            {
                var c = argumentsString[i];

                if (!inString && (c == '\'' || c == '"'))
                {
                    inString = true;
                    stringChar = c;
                    current += c;
                }
                else if (inString && c == stringChar)
                {
                    inString = false;
                    current += c;
                }
                else if (!inString && c == '(')
                {
                    depth++;
                    current += c;
                }
                else if (!inString && c == ')')
                {
                    depth--;
                    current += c;
                }
                else if (!inString && c == ',' && depth == 0)
                {
                    arguments.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            if (!string.IsNullOrEmpty(current))
            {
                arguments.Add(current);
            }

            return arguments.ToArray();
        }

        private Dictionary<string, Func<object[], object>> InitializeFunctions()
        {
            return new Dictionary<string, Func<object[], object>>(StringComparer.OrdinalIgnoreCase)
            {
                ["IF"] = args => Convert.ToBoolean(args[0]) ? args[1] : args[2],
                ["AND"] = args => Convert.ToBoolean(args[0]) && Convert.ToBoolean(args[1]),
                ["OR"] = args => Convert.ToBoolean(args[0]) || Convert.ToBoolean(args[1]),
                ["NOT"] = args => !Convert.ToBoolean(args[0]),
                ["EQUALS"] = args => Equals(args[0], args[1]),
                ["GREATER_THAN"] = args => Convert.ToDecimal(args[0]) > Convert.ToDecimal(args[1]),
                ["LESS_THAN"] = args => Convert.ToDecimal(args[0]) < Convert.ToDecimal(args[1]),
                ["CONTAINS"] = args => args[0]?.ToString()?.Contains(args[1]?.ToString() ?? "") ?? false,
                ["LENGTH"] = args => args[0]?.ToString()?.Length ?? 0,
                ["UPPER"] = args => args[0]?.ToString()?.ToUpper() ?? "",
                ["LOWER"] = args => args[0]?.ToString()?.ToLower() ?? "",
                ["TRIM"] = args => args[0]?.ToString()?.Trim() ?? "",
                ["SUBSTRING"] = args => args[0]?.ToString()?.Substring(Convert.ToInt32(args[1]), Convert.ToInt32(args[2])) ?? "",
                ["CONCAT"] = args => (args[0]?.ToString() ?? "") + (args[1]?.ToString() ?? ""),
                ["ADD"] = args => Convert.ToDecimal(args[0]) + Convert.ToDecimal(args[1]),
                ["SUBTRACT"] = args => Convert.ToDecimal(args[0]) - Convert.ToDecimal(args[1]),
                ["MULTIPLY"] = args => Convert.ToDecimal(args[0]) * Convert.ToDecimal(args[1]),
                ["DIVIDE"] = args => Convert.ToDecimal(args[0]) / Convert.ToDecimal(args[1]),
                ["ROUND"] = args => Math.Round(Convert.ToDecimal(args[0]), Convert.ToInt32(args[1])),
                ["ABS"] = args => Math.Abs(Convert.ToDecimal(args[0])),
                ["MIN"] = args => Math.Min(Convert.ToDecimal(args[0]), Convert.ToDecimal(args[1])),
                ["MAX"] = args => Math.Max(Convert.ToDecimal(args[0]), Convert.ToDecimal(args[1])),
                ["NOW"] = args => DateTime.Now,
                ["TODAY"] = args => DateTime.Today,
                ["DATE_ADD"] = args => AddToDate(Convert.ToDateTime(args[0]), Convert.ToInt32(args[1]), args[2]?.ToString()),
                ["DATE_DIFF"] = args => GetDateDifference(Convert.ToDateTime(args[0]), Convert.ToDateTime(args[1]), args[2]?.ToString()),
                ["FORMAT_DATE"] = args => Convert.ToDateTime(args[0]).ToString(args[1]?.ToString()),
                ["IS_NULL"] = args => args[0] == null,
                ["IS_EMPTY"] = args => string.IsNullOrEmpty(args[0]?.ToString()),
                ["COALESCE"] = args => args[0] ?? args[1]
            };
        }

        private DateTime AddToDate(DateTime date, int value, string unit)
        {
            return unit?.ToLower() switch
            {
                "days" or "day" or "d" => date.AddDays(value),
                "months" or "month" or "m" => date.AddMonths(value),
                "years" or "year" or "y" => date.AddYears(value),
                "hours" or "hour" or "h" => date.AddHours(value),
                "minutes" or "minute" or "min" => date.AddMinutes(value),
                "seconds" or "second" or "s" => date.AddSeconds(value),
                _ => date
            };
        }

        private int GetDateDifference(DateTime date1, DateTime date2, string unit)
        {
            var diff = date2 - date1;
            return unit?.ToLower() switch
            {
                "days" or "day" or "d" => (int)diff.TotalDays,
                "hours" or "hour" or "h" => (int)diff.TotalHours,
                "minutes" or "minute" or "min" => (int)diff.TotalMinutes,
                "seconds" or "second" or "s" => (int)diff.TotalSeconds,
                "months" or "month" or "m" => ((date2.Year - date1.Year) * 12) + date2.Month - date1.Month,
                "years" or "year" or "y" => date2.Year - date1.Year,
                _ => (int)diff.TotalDays
            };
        }

        private HashSet<string> InitializeOperators()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "+", "-", "*", "/", "%",
                "==", "!=", ">", "<", ">=", "<=",
                "&&", "||", "!",
                "&", "|", "^",
                "(", ")", ","
            };
        }

        private HashSet<string> InitializeSecurityBlacklist()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "eval", "exec", "system", "process", "file", "directory",
                "registry", "network", "socket", "http", "url", "reflection",
                "assembly", "type", "method", "property", "field", "constructor",
                "activator", "invoke", "gettype", "typeof", "nameof",
                "environment", "appdomain", "thread", "task", "parallel",
                "unsafe", "fixed", "stackalloc", "marshal", "pointer",
                "gc", "finalizer", "destructor", "dispose", "using",
                "lock", "monitor", "mutex", "semaphore", "event",
                "delegate", "action", "func", "expression", "lambda",
                "dynamic", "var", "object", "void", "null",
                "class", "struct", "interface", "enum", "namespace",
                "public", "private", "protected", "internal", "static",
                "virtual", "override", "abstract", "sealed", "partial",
                "const", "readonly", "volatile", "extern", "unsafe",
                "checked", "unchecked", "sizeof", "default", "typeof",
                "is", "as", "new", "this", "base", "ref", "out", "in",
                "params", "yield", "await", "async", "var", "dynamic"
            };
        }

        private List<string> ValidateSecurityConstraints(string expression)
        {
            var issues = new List<string>();

            foreach (var blacklistedItem in _securityBlacklist)
            {
                if (expression.Contains(blacklistedItem, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add($"Expression contains forbidden keyword: {blacklistedItem}");
                }
            }

            return issues;
        }

        private List<string> ValidateSyntax(string expression)
        {
            var errors = new List<string>();

            // Check for balanced parentheses
            var openParens = expression.Count(c => c == '(');
            var closeParens = expression.Count(c => c == ')');
            if (openParens != closeParens)
            {
                errors.Add("Unbalanced parentheses in expression");
            }

            // Check for balanced quotes
            var singleQuotes = expression.Count(c => c == '\'');
            var doubleQuotes = expression.Count(c => c == '"');
            if (singleQuotes % 2 != 0)
            {
                errors.Add("Unbalanced single quotes in expression");
            }
            if (doubleQuotes % 2 != 0)
            {
                errors.Add("Unbalanced double quotes in expression");
            }

            return errors;
        }

        private List<string> ValidateVariables(string expression, ValidationContext validationContext)
        {
            var errors = new List<string>();

            // Extract variable references (starting with $)
            var variableMatches = Regex.Matches(expression, @"\$([a-zA-Z_][a-zA-Z0-9_]*)");
            
            foreach (Match match in variableMatches)
            {
                var variableName = match.Groups[1].Value;
                if (!validationContext.Variables.ContainsKey(variableName))
                {
                    errors.Add($"Unknown variable: {variableName}");
                }
            }

            return errors;
        }
    }
}