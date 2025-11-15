using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ClosedXML.Excel;

class EndpointInfo
{
    public string Method { get; set; } = "";
    public string Route { get; set; } = "";
    public string File { get; set; } = "";
}

class Program
{
    // نرمال‌سازی Route ها برای مقایسه
    static bool IsSameRoute(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;

        a = a.Trim().Trim('/').ToLowerInvariant();
        b = b.Trim().Trim('/').ToLowerInvariant();

        while (a.Contains("//")) a = a.Replace("//", "/");
        while (b.Contains("//")) b = b.Replace("//", "/");

        return a == b;
    }

    // نرمال‌سازی Method (HttpGet → GET, get → GET, POST ...)
    static string NormalizeMethod(string m)
    {
        if (string.IsNullOrWhiteSpace(m)) return "";
        return m.Replace("Http", "", StringComparison.OrdinalIgnoreCase)
                .Trim()
                .ToUpperInvariant();
    }

    static async Task Main()
    {
        Console.WriteLine("⏳ Scanning project folder...");

        // روت پروژه‌ات - اگر لازم شد عوضش کن
        string root = @"D:\00-Work\Fullstack-Team\02-Projects\CoreAxis\src";

        var csFiles = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                               .Where(f => Path.GetFileName(f)
                                   .EndsWith("Controller.cs", StringComparison.OrdinalIgnoreCase))
                               .ToList();

        List<EndpointInfo> codeEndpoints = new();

        foreach (var file in csFiles)
        {
            string text = File.ReadAllText(file);

            // نام کنترلر از روی اسم فایل
            string controllerName = Path.GetFileNameWithoutExtension(file);
            if (controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                controllerName = controllerName[..^"Controller".Length];

            // استخراج Route اصلی Controller
            string baseRoute = "";

            var controllerRouteMatch = Regex.Match(
                text,
                @"\[Route\(""([^""]*)""\)\]",
                RegexOptions.IgnoreCase
            );

            if (controllerRouteMatch.Success)
            {
                baseRoute = controllerRouteMatch.Groups[1].Value;

                // جایگزینی [controller] در Route
                baseRoute = baseRoute.Replace("[controller]", controllerName,
                    StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // اگر Route مشخص نشده، یک پیش‌فرض منطقی:
                // /api/{ControllerName}
                baseRoute = $"api/{controllerName}";
            }

            // استخراج متدهای Http*
            var methodMatches = Regex.Matches(
                text,
                @"\[(HttpGet|HttpPost|HttpPut|HttpDelete)(?:\(""([^""]*)""\))?\]",
                RegexOptions.IgnoreCase
            );

            foreach (Match m in methodMatches)
            {
                var http = m.Groups[1].Value;   // HttpGet / HttpPost ...
                var sub = m.Groups[2].Value;    // "login", "create" ...

                string finalRoute = baseRoute;

                if (!string.IsNullOrWhiteSpace(sub))
                {
                    finalRoute = finalRoute.TrimEnd('/');
                    finalRoute = $"{finalRoute}/{sub}".Replace("//", "/");
                }

                codeEndpoints.Add(new EndpointInfo
                {
                    Method = http,
                    Route = finalRoute,
                    File = Path.GetFileName(file)
                });
            }
        }

        Console.WriteLine($"📌 Found {codeEndpoints.Count} endpoints in code.");

        // ------------------------------------------------------
        // خواندن Swagger JSON
        // ------------------------------------------------------
        Console.WriteLine("⏳ Loading Swagger...");

        string swaggerUrl = "http://localhost:5077/swagger/v1/swagger.json";
        HttpClient client = new();

        string swaggerJson;
        try
        {
            swaggerJson = await client.GetStringAsync(swaggerUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ ERROR reading Swagger: " + ex.Message);
            return;
        }

        JObject swagger = JObject.Parse(swaggerJson);

        var swaggerEndpoints = new List<EndpointInfo>();

        var pathsToken = swagger["paths"] as JObject;
        if (pathsToken != null)
        {
            foreach (var pathProp in pathsToken.Properties())
            {
                string route = pathProp.Name; // مثل /api/auth/login

                if (pathProp.Value is not JObject methodsObj)
                    continue;

                foreach (var methodProp in methodsObj.Properties())
                {
                    string methodName = methodProp.Name; // get, post, put, delete

                    swaggerEndpoints.Add(new EndpointInfo
                    {
                        Method = methodName.ToUpperInvariant(),
                        Route = route,
                        File = "Swagger"
                    });
                }
            }
        }

        Console.WriteLine($"📌 Found {swaggerEndpoints.Count} endpoints in Swagger.");

        // ------------------------------------------------------
        // مقایسه
        // ------------------------------------------------------
        var missing = codeEndpoints
            .Where(c => !swaggerEndpoints.Any(s =>
                NormalizeMethod(s.Method) == NormalizeMethod(c.Method)
                && IsSameRoute(s.Route, c.Route)
            ))
            .ToList();

        Console.WriteLine($"⚠ Missing in Swagger: {missing.Count}");

        // ------------------------------------------------------
        // Excel خروجی
        // ------------------------------------------------------
        string excelPath = "EndpointCoverage.xlsx";
        var wb = new XLWorkbook();

        // Sheet 1 - Endpointهای کد
        var ws1 = wb.AddWorksheet("ControllersEndpoints");
        ws1.Cell(1, 1).Value = "Method";
        ws1.Cell(1, 2).Value = "Route";
        ws1.Cell(1, 3).Value = "File";

        for (int i = 0; i < codeEndpoints.Count; i++)
        {
            ws1.Cell(i + 2, 1).Value = NormalizeMethod(codeEndpoints[i].Method);
            ws1.Cell(i + 2, 2).Value = codeEndpoints[i].Route;
            ws1.Cell(i + 2, 3).Value = codeEndpoints[i].File;
        }
        ws1.Columns().AdjustToContents();

        // Sheet 2 - Endpointهای Swagger
        var ws2 = wb.AddWorksheet("SwaggerEndpoints");
        ws2.Cell(1, 1).Value = "Method";
        ws2.Cell(1, 2).Value = "Route";

        for (int i = 0; i < swaggerEndpoints.Count; i++)
        {
            ws2.Cell(i + 2, 1).Value = NormalizeMethod(swaggerEndpoints[i].Method);
            ws2.Cell(i + 2, 2).Value = swaggerEndpoints[i].Route;
        }
        ws2.Columns().AdjustToContents();

        // Sheet 3 - کمبودها
        var ws3 = wb.AddWorksheet("MissingInSwagger");
        ws3.Cell(1, 1).Value = "Method";
        ws3.Cell(1, 2).Value = "Route";
        ws3.Cell(1, 3).Value = "File";

        for (int i = 0; i < missing.Count; i++)
        {
            ws3.Cell(i + 2, 1).Value = NormalizeMethod(missing[i].Method);
            ws3.Cell(i + 2, 2).Value = missing[i].Route;
            ws3.Cell(i + 2, 3).Value = missing[i].File;
        }
        ws3.Columns().AdjustToContents();

        wb.SaveAs(excelPath);

        Console.WriteLine($"✅ DONE → Output saved to {excelPath}");
    }
}
