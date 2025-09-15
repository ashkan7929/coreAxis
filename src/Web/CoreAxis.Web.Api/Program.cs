using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.Modules.WalletModule.Api;
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

// Add WalletModule
builder.Services.AddWalletModuleApi(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();