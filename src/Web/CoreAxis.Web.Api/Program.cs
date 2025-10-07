using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel.Observability;
using CoreAxis.Modules.WalletModule.Api;
using CoreAxis.Modules.ProductOrderModule.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("webApi-v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CoreAxis Web API",
        Version = "v1",
        Description = "Main Web API for CoreAxis application"
    });
});

// Add Entity Framework
builder.Services.AddDbContext<DynamicFormDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Observability: register ProblemDetails services (extensible/no-op for now)
builder.Services.AddCoreAxisProblemDetails();

// Add WalletModule
builder.Services.AddWalletModuleApi(builder.Configuration);

// Add ProductOrderModule (Product API)
builder.Services.AddProductOrderModuleApi(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// Insert Correlation middleware early so headers/context propagate to downstream components
app.UseCoreAxisCorrelation();
// Map exceptions uniformly to RFC7807 Problem+JSON with correlation
app.UseCoreAxisProblemDetails();
app.UseAuthorization();
app.MapControllers();

app.Run();