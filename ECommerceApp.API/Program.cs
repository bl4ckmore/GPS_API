using System.Text;
using ECommerceApp.Application.Interfaces;
using ECommerceApp.Infrastructure.Auth;
using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Infrastructure.Whats;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using ECommerceApp.Infrastructure.Repositories;
using ECommerceApp.Core.Interfaces; // <-- added

var builder = WebApplication.CreateBuilder(args);

// --- PostgreSQL + EF Core with Dynamic JSON enabled (fixes jsonb -> Dictionary<string,string>) ---
var cs = builder.Configuration.GetConnectionString("Default")
         ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");
var dsb = new NpgsqlDataSourceBuilder(cs);
dsb.EnableDynamicJson();            // <— IMPORTANT for jsonb <-> Dictionary<string,string>
var dataSource = dsb.Build();

builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(dataSource));

// Controllers
builder.Services.AddControllers();

// CORS
const string CorsPolicy = "ng";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(CorsPolicy, p => p.WithOrigins(allowedOrigins)
                                    .AllowAnyHeader()
                                    .AllowAnyMethod()
                                    .AllowCredentials());
});

// === HttpClient factory + Whats named client ===
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("whats", c =>
{
    var baseUrl = builder.Configuration["WhatsGps:BaseUrl"] ?? "https://www.whatsgps.com";
    c.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    c.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
    c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ECommerceApp/1.0 (+http://localhost)");
});

// === Whats session store (in-memory) ===
builder.Services.AddSingleton<IWhatsSessionStore, InMemoryWhatsSessionStore>();

// ===== JWT (single source of truth) =====
var jwtKey = builder.Configuration["Auth:Jwt:Key"] ?? builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("JWT key missing. Set Auth:Jwt:Key or Jwt:Key.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.RequireHttpsMetadata = false; // dev
    o.SaveToken = true;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddAuthorization();

// JWT minting service
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// 🔽 OPEN-GENERIC REPOSITORY REGISTRATION (this fixes IGenericRepository<T> DI)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Swagger (Bearer)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ECommerceApp API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste RAW JWT (no 'Bearer ' prefix).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
