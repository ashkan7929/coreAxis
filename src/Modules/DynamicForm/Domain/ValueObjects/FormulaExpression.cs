using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;

namespace CoreAxis.Modules.DynamicForm.Domain.ValueObjects
{
    /// <summary>
    /// Represents a formula expression with validation, dependency tracking, and metadata.
    /// </summary>
    public class FormulaExpression : IEquatable<FormulaExpression>
    {
        /// <summary>
        /// Gets the raw expression string.
        /// </summary>
        public string Expression { get; private set; }

        /// <summary>
        /// Gets the normalized expression string (cleaned and formatted).
        /// </summary>
        public string NormalizedExpression { get; private set; }

        /// <summary>
        /// Gets the return type of the expression.
        /// </summary>
        public Type ReturnType { get; private set; }

        /// <summary>
        /// Gets the variables referenced in the expression.
        /// </summary>
        public HashSet<string> Variables { get; private set; }

        /// <summary>
        /// Gets the functions used in the expression.
        /// </summary>
        public HashSet<string> Functions { get; private set; }

        /// <summary>
        /// Gets the constants defined in the expression.
        /// </summary>
        public Dictionary<string, object> Constants { get; private set; }

        /// <summary>
        /// Gets whether the expression is valid.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets the validation errors if the expression is invalid.
        /// </summary>
        public IReadOnlyList<string> ValidationErrors { get; private set; }

        /// <summary>
        /// Gets the complexity score of the expression (0-100).
        /// </summary>
        public int ComplexityScore { get; private set; }

        /// <summary>
        /// Gets whether the expression is deterministic (always returns the same result for the same inputs).
        /// </summary>
        public bool IsDeterministic { get; private set; }

        /// <summary>
        /// Gets whether the expression has side effects.
        /// </summary>
        public bool HasSideEffects { get; private set; }

        /// <summary>
        /// Gets the estimated execution time category.
        /// </summary>
        public ExecutionTimeCategory EstimatedExecutionTime { get; private set; }

        /// <summary>
        /// Gets additional metadata about the expression.
        /// </summary>
        public Dictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Gets the expression format version.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the hash of the expression for caching purposes.
        /// </summary>
        public string Hash { get; private set; }

        /// <summary>
        /// Initializes a new instance of the FormulaExpression class.
        /// </summary>
        /// <param name="expression">The expression string.</param>
        /// <param name="returnType">The expected return type.</param>
        /// <param name="version">The expression format version.</param>
        public FormulaExpression(string expression, Type returnType = null, string version = "1.0")
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Expression cannot be null or empty.", nameof(expression));

            Expression = expression.Trim();
            ReturnType = returnType ?? typeof(object);
            Version = version ?? "1.0";
            
            Initialize();
        }

        /// <summary>
        /// Creates a formula expression from a string.
        /// </summary>
        /// <param name="expression">The expression string.</param>
        /// <param name="returnType">The expected return type.</param>
        /// <returns>A new formula expression.</returns>
        public static FormulaExpression Create(string expression, Type returnType = null)
        {
            return new FormulaExpression(expression, returnType);
        }

        /// <summary>
        /// Creates a simple arithmetic expression.
        /// </summary>
        /// <param name="expression">The arithmetic expression.</param>
        /// <returns>A new formula expression for arithmetic operations.</returns>
        public static FormulaExpression Arithmetic(string expression)
        {
            return new FormulaExpression(expression, typeof(decimal));
        }

        /// <summary>
        /// Creates a boolean logic expression.
        /// </summary>
        /// <param name="expression">The boolean expression.</param>
        /// <returns>A new formula expression for boolean operations.</returns>
        public static FormulaExpression Boolean(string expression)
        {
            return new FormulaExpression(expression, typeof(bool));
        }

        /// <summary>
        /// Creates a string manipulation expression.
        /// </summary>
        /// <param name="expression">The string expression.</param>
        /// <returns>A new formula expression for string operations.</returns>
        public static FormulaExpression String(string expression)
        {
            return new FormulaExpression(expression, typeof(string));
        }

        /// <summary>
        /// Creates a date/time expression.
        /// </summary>
        /// <param name="expression">The date/time expression.</param>
        /// <returns>A new formula expression for date/time operations.</returns>
        public static FormulaExpression DateTime(string expression)
        {
            return new FormulaExpression(expression, typeof(DateTime));
        }

        /// <summary>
        /// Creates an array/collection expression.
        /// </summary>
        /// <param name="expression">The array expression.</param>
        /// <returns>A new formula expression for array operations.</returns>
        public static FormulaExpression Array(string expression)
        {
            return new FormulaExpression(expression, typeof(object[]));
        }

        /// <summary>
        /// Creates a conditional expression.
        /// </summary>
        /// <param name="condition">The condition expression.</param>
        /// <param name="trueValue">The value when condition is true.</param>
        /// <param name="falseValue">The value when condition is false.</param>
        /// <returns>A new conditional formula expression.</returns>
        public static FormulaExpression Conditional(string condition, string trueValue, string falseValue)
        {
            var expression = $"IF({condition}, {trueValue}, {falseValue})";
            return new FormulaExpression(expression, typeof(object));
        }

        /// <summary>
        /// Creates a lookup expression for referencing other fields or data sources.
        /// </summary>
        /// <param name="source">The data source or field reference.</param>
        /// <param name="key">The lookup key.</param>
        /// <param name="defaultValue">The default value if lookup fails.</param>
        /// <returns>A new lookup formula expression.</returns>
        public static FormulaExpression Lookup(string source, string key, string defaultValue = null)
        {
            var expression = defaultValue != null 
                ? $"LOOKUP({source}, {key}, {defaultValue})"
                : $"LOOKUP({source}, {key})";
            return new FormulaExpression(expression, typeof(object));
        }

        /// <summary>
        /// Creates an aggregation expression for calculating values across collections.
        /// </summary>
        /// <param name="function">The aggregation function (SUM, AVG, COUNT, etc.).</param>
        /// <param name="source">The data source.</param>
        /// <param name="condition">Optional condition to filter data.</param>
        /// <returns>A new aggregation formula expression.</returns>
        public static FormulaExpression Aggregate(string function, string source, string condition = null)
        {
            var expression = condition != null 
                ? $"{function}({source}, {condition})"
                : $"{function}({source})";
            return new FormulaExpression(expression, typeof(decimal));
        }

        /// <summary>
        /// Validates the expression syntax and semantics.
        /// </summary>
        /// <param name="context">Optional validation context with available variables and functions.</param>
        /// <returns>True if the expression is valid; otherwise, false.</returns>
        public bool Validate(ValidationContext context = null)
        {
            var errors = new List<string>();
            context ??= ValidationContext.Default();

            // Basic syntax validation
            if (!ValidateSyntax(errors))
            {
                ValidationErrors = errors.AsReadOnly();
                IsValid = false;
                return false;
            }

            // Semantic validation
            if (!ValidateSemantics(context, errors))
            {
                ValidationErrors = errors.AsReadOnly();
                IsValid = false;
                return false;
            }

            ValidationErrors = new List<string>().AsReadOnly();
            IsValid = true;
            return true;
        }

        /// <summary>
        /// Gets the dependencies of this expression (variables and functions it uses).
        /// </summary>
        /// <returns>A collection of dependency names.</returns>
        public IEnumerable<string> GetDependencies()
        {
            return Variables.Concat(Functions).Distinct();
        }

        /// <summary>
        /// Checks if the expression depends on a specific variable or function.
        /// </summary>
        /// <param name="name">The variable or function name.</param>
        /// <returns>True if the expression depends on the specified name; otherwise, false.</returns>
        public bool DependsOn(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return Variables.Contains(name) || Functions.Contains(name);
        }

        /// <summary>
        /// Gets a simplified version of the expression by removing unnecessary whitespace and formatting.
        /// </summary>
        /// <returns>A simplified expression string.</returns>
        public string GetSimplified()
        {
            return NormalizedExpression;
        }

        /// <summary>
        /// Creates a copy of the expression with a different return type.
        /// </summary>
        /// <param name="newReturnType">The new return type.</param>
        /// <returns>A new formula expression with the specified return type.</returns>
        public FormulaExpression WithReturnType(Type newReturnType)
        {
            return new FormulaExpression(Expression, newReturnType, Version);
        }

        /// <summary>
        /// Creates a copy of the expression with additional metadata.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The metadata value.</param>
        /// <returns>A new formula expression with the added metadata.</returns>
        public FormulaExpression WithMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty.", nameof(key));

            var result = new FormulaExpression(Expression, ReturnType, Version);
            result.Metadata[key] = value;
            return result;
        }

        /// <summary>
        /// Converts the expression to a JSON representation.
        /// </summary>
        /// <returns>A JSON string representing the expression.</returns>
        public string ToJson()
        {
            var data = new
            {
                expression = Expression,
                normalizedExpression = NormalizedExpression,
                returnType = ReturnType.FullName,
                variables = Variables.ToArray(),
                functions = Functions.ToArray(),
                constants = Constants,
                isValid = IsValid,
                validationErrors = ValidationErrors.ToArray(),
                complexityScore = ComplexityScore,
                isDeterministic = IsDeterministic,
                hasSideEffects = HasSideEffects,
                estimatedExecutionTime = EstimatedExecutionTime.ToString(),
                metadata = Metadata,
                version = Version,
                hash = Hash
            };

            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        private void Initialize()
        {
            NormalizedExpression = NormalizeExpression(Expression);
            Variables = ExtractVariables(NormalizedExpression);
            Functions = ExtractFunctions(NormalizedExpression);
            Constants = ExtractConstants(NormalizedExpression);
            ComplexityScore = CalculateComplexity();
            IsDeterministic = CheckDeterminism();
            HasSideEffects = CheckSideEffects();
            EstimatedExecutionTime = EstimateExecutionTime();
            Metadata = new Dictionary<string, object>();
            Hash = CalculateHash();
            ValidationErrors = new List<string>().AsReadOnly();
            IsValid = true; // Will be set properly during validation
        }

        private string NormalizeExpression(string expr)
        {
            // Remove extra whitespace and normalize formatting
            return Regex.Replace(expr.Trim(), @"\s+", " ");
        }

        private HashSet<string> ExtractVariables(string expr)
        {
            var variables = new HashSet<string>();
            
            // Simple regex to find variable references (alphanumeric identifiers not followed by '(')
            var matches = Regex.Matches(expr, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b(?!\s*\()");
            
            foreach (Match match in matches)
            {
                var variable = match.Groups[1].Value;
                if (!IsReservedKeyword(variable))
                {
                    variables.Add(variable);
                }
            }
            
            return variables;
        }

        private HashSet<string> ExtractFunctions(string expr)
        {
            var functions = new HashSet<string>();
            
            // Regex to find function calls (identifiers followed by '(')
            var matches = Regex.Matches(expr, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\(");
            
            foreach (Match match in matches)
            {
                functions.Add(match.Groups[1].Value);
            }
            
            return functions;
        }

        private Dictionary<string, object> ExtractConstants(string expr)
        {
            var constants = new Dictionary<string, object>();
            
            // Extract numeric constants
            var numericMatches = Regex.Matches(expr, @"\b(\d+(?:\.\d+)?)\b");
            foreach (Match match in numericMatches)
            {
                if (decimal.TryParse(match.Value, out var value))
                {
                    constants[$"CONST_{match.Value}"] = value;
                }
            }
            
            // Extract string constants
            var stringMatches = Regex.Matches(expr, @"""([^""]*)""");
            foreach (Match match in stringMatches)
            {
                constants[$"STR_{match.Groups[1].Value}"] = match.Groups[1].Value;
            }
            
            return constants;
        }

        private int CalculateComplexity()
        {
            var score = 0;
            
            // Base complexity
            score += Math.Min(Expression.Length / 10, 20);
            
            // Function complexity
            score += Functions.Count * 5;
            
            // Variable complexity
            score += Variables.Count * 2;
            
            // Nesting complexity (count parentheses)
            var depth = 0;
            var maxDepth = 0;
            foreach (var c in Expression)
            {
                if (c == '(') depth++;
                else if (c == ')') depth--;
                maxDepth = Math.Max(maxDepth, depth);
            }
            score += maxDepth * 3;
            
            return Math.Min(score, 100);
        }

        private bool CheckDeterminism()
        {
            // Check for non-deterministic functions
            var nonDeterministicFunctions = new[] { "RAND", "RANDOM", "NOW", "TODAY", "NEWID" };
            return !Functions.Any(f => nonDeterministicFunctions.Contains(f.ToUpper()));
        }

        private bool CheckSideEffects()
        {
            // Check for functions that might have side effects
            var sideEffectFunctions = new[] { "SEND", "SAVE", "DELETE", "UPDATE", "INSERT", "LOG" };
            return Functions.Any(f => sideEffectFunctions.Contains(f.ToUpper()));
        }

        private ExecutionTimeCategory EstimateExecutionTime()
        {
            if (ComplexityScore < 20) return ExecutionTimeCategory.Fast;
            if (ComplexityScore < 50) return ExecutionTimeCategory.Medium;
            if (ComplexityScore < 80) return ExecutionTimeCategory.Slow;
            return ExecutionTimeCategory.VerySlow;
        }

        private string CalculateHash()
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(NormalizedExpression));
            return Convert.ToBase64String(hash);
        }

        private bool ValidateSyntax(List<string> errors)
        {
            var isValid = true;
            
            // Check for balanced parentheses
            var depth = 0;
            foreach (var c in Expression)
            {
                if (c == '(') depth++;
                else if (c == ')') depth--;
                if (depth < 0)
                {
                    errors.Add("Unmatched closing parenthesis.");
                    isValid = false;
                    break;
                }
            }
            
            if (depth > 0)
            {
                errors.Add("Unmatched opening parenthesis.");
                isValid = false;
            }
            
            // Check for empty expression
            if (string.IsNullOrWhiteSpace(NormalizedExpression))
            {
                errors.Add("Expression cannot be empty.");
                isValid = false;
            }
            
            return isValid;
        }

        private bool ValidateSemantics(ValidationContext context, List<string> errors)
        {
            var isValid = true;
            
            // Check if all variables are available
            foreach (var variable in Variables)
            {
                if (!context.AvailableVariables.Contains(variable))
                {
                    errors.Add($"Unknown variable: {variable}");
                    isValid = false;
                }
            }
            
            // Check if all functions are available
            foreach (var function in Functions)
            {
                if (!context.AvailableFunctions.Contains(function))
                {
                    errors.Add($"Unknown function: {function}");
                    isValid = false;
                }
            }
            
            return isValid;
        }

        private bool IsReservedKeyword(string word)
        {
            var keywords = new[] { "IF", "THEN", "ELSE", "AND", "OR", "NOT", "TRUE", "FALSE", "NULL" };
            return keywords.Contains(word.ToUpper());
        }

        /// <summary>
        /// Evaluates the expression using the provided expression engine.
        /// </summary>
        /// <param name="engine">The expression engine to use for evaluation.</param>
        /// <param name="context">The evaluation context containing variables and their values.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The evaluation result.</returns>
        public async Task<object> EvaluateAsync(
            IExpressionEngine engine,
            ExpressionEvaluationContext context,
            CancellationToken cancellationToken = default)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!IsValid)
                throw new InvalidOperationException($"Cannot evaluate invalid expression. Errors: {string.Join(", ", ValidationErrors)}");

            var result = await engine.EvaluateAsync(this, context, cancellationToken);
            
            if (!result.IsSuccess)
                throw new InvalidOperationException($"Expression evaluation failed: {result.ErrorMessage}");

            return result.Value;
        }

        /// <summary>
        /// Evaluates the expression synchronously using the provided expression engine.
        /// </summary>
        /// <param name="engine">The expression engine to use for evaluation.</param>
        /// <param name="context">The evaluation context containing variables and their values.</param>
        /// <returns>The evaluation result.</returns>
        public object Evaluate(
            IExpressionEngine engine,
            ExpressionEvaluationContext context)
        {
            return EvaluateAsync(engine, context).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(FormulaExpression other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return NormalizedExpression == other.NormalizedExpression &&
                   ReturnType == other.ReturnType &&
                   Version == other.Version;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>True if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as FormulaExpression);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(NormalizedExpression, ReturnType, Version);
        }

        /// <summary>
        /// Returns a string representation of the formula expression.
        /// </summary>
        /// <returns>A string representation of the formula expression.</returns>
        public override string ToString()
        {
            return $"FormulaExpression({Expression})";
        }

        public static bool operator ==(FormulaExpression left, FormulaExpression right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FormulaExpression left, FormulaExpression right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Represents the estimated execution time category for a formula expression.
    /// </summary>
    public enum ExecutionTimeCategory
    {
        /// <summary>
        /// Fast execution (< 1ms).
        /// </summary>
        Fast,

        /// <summary>
        /// Medium execution (1-10ms).
        /// </summary>
        Medium,

        /// <summary>
        /// Slow execution (10-100ms).
        /// </summary>
        Slow,

        /// <summary>
        /// Very slow execution (> 100ms).
        /// </summary>
        VerySlow
    }

    /// <summary>
    /// Represents the validation context for formula expressions.
    /// </summary>
    public class ValidationContext
    {
        /// <summary>
        /// Gets the available variables in the context.
        /// </summary>
        public HashSet<string> AvailableVariables { get; private set; }

        /// <summary>
        /// Gets the available functions in the context.
        /// </summary>
        public HashSet<string> AvailableFunctions { get; private set; }

        /// <summary>
        /// Gets the variable types in the context.
        /// </summary>
        public Dictionary<string, Type> VariableTypes { get; private set; }

        /// <summary>
        /// Gets the function signatures in the context.
        /// </summary>
        public Dictionary<string, FunctionSignature> FunctionSignatures { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ValidationContext class.
        /// </summary>
        /// <param name="availableVariables">The available variables.</param>
        /// <param name="availableFunctions">The available functions.</param>
        /// <param name="variableTypes">The variable types.</param>
        /// <param name="functionSignatures">The function signatures.</param>
        public ValidationContext(
            HashSet<string> availableVariables = null,
            HashSet<string> availableFunctions = null,
            Dictionary<string, Type> variableTypes = null,
            Dictionary<string, FunctionSignature> functionSignatures = null)
        {
            AvailableVariables = availableVariables ?? new HashSet<string>();
            AvailableFunctions = availableFunctions ?? new HashSet<string>();
            VariableTypes = variableTypes ?? new Dictionary<string, Type>();
            FunctionSignatures = functionSignatures ?? new Dictionary<string, FunctionSignature>();
        }

        /// <summary>
        /// Creates a default validation context with common variables and functions.
        /// </summary>
        /// <returns>A default validation context.</returns>
        public static ValidationContext Default()
        {
            var variables = new HashSet<string> { "value", "field", "form", "user", "date", "time" };
            var functions = new HashSet<string> { "IF", "SUM", "AVG", "COUNT", "MIN", "MAX", "ROUND", "ABS", "SQRT", "CONCAT", "UPPER", "LOWER", "TRIM" };
            
            return new ValidationContext(variables, functions);
        }
    }

    /// <summary>
    /// Represents a function signature for validation purposes.
    /// </summary>
    public class FunctionSignature
    {
        /// <summary>
        /// Gets the function name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parameter types.
        /// </summary>
        public Type[] ParameterTypes { get; }

        /// <summary>
        /// Gets the return type.
        /// </summary>
        public Type ReturnType { get; }

        /// <summary>
        /// Gets whether the function accepts variable arguments.
        /// </summary>
        public bool IsVariadic { get; }

        /// <summary>
        /// Initializes a new instance of the FunctionSignature class.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="returnType">The return type.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="isVariadic">Whether the function accepts variable arguments.</param>
        public FunctionSignature(string name, Type returnType, Type[] parameterTypes, bool isVariadic = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ParameterTypes = parameterTypes ?? throw new ArgumentNullException(nameof(parameterTypes));
            ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            IsVariadic = isVariadic;
        }
    }
}