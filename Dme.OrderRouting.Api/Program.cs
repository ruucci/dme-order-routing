using Dme.OrderRouting.Api.Configuration;
using Dme.OrderRouting.Api.Middleware;
using Dme.OrderRouting.Api.Repositories;
using Dme.OrderRouting.Api.Repositories.Interfaces;
using Dme.OrderRouting.Api.Services;
using Dme.OrderRouting.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddSingleton<IProductRepository, CsvProductRepository>();
builder.Services.AddSingleton<ISupplierRepository, CsvSupplierRepository>();
builder.Services.AddSingleton<IZipCoverageService, ZipCoverageService>();
builder.Services.AddScoped<IOrderRoutingService, OrderRoutingService>();
builder.Services.Configure<DataFileSettings>(
    builder.Configuration.GetSection("DataFiles"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();
app.Run();