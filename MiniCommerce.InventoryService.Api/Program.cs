using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiniCommerce.InventoryService.Api.Grpc;
using MiniCommerce.InventoryService.Application.Interfaces;
using MiniCommerce.InventoryService.Application.Mapping;
using MiniCommerce.InventoryService.Application.Services;
using MiniCommerce.InventoryService.Domain.Interfaces;
using MiniCommerce.InventoryService.Infrastructure.Data;
using MiniCommerce.InventoryService.Infrastructure.Messaging;
using MiniCommerce.InventoryService.Infrastructure.Repositories;
using MiniCommerce.InventoryService.Infrastructure.Storage;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ?? ADD JWT AUTH HERE
var jwt = config.GetSection("JwtSettings");
var secret = jwt["Secret"];
var issuer = jwt["Issuer"];
var audience = jwt["Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;   // BFF runs behind gateway, HTTP is fine
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });



builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Force kestrel to use HTTP/2
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5044, o =>
    {
        o.Protocols = HttpProtocols.Http1;
    });
    options.ListenAnyIP(5001, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });
});

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=inventorydb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAutoMapper(typeof(ProductMappingProfile).Assembly);


// Background consumer
builder.Services.AddHostedService<OrderEventsConsumer>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IFileStorage, AzureBlobStorageService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<InventoryGrpcService>();
app.MapGet("/", () => "Inventory gRPC Service running.");

app.Run();
