using System.Net;
using System.Text;
using ECommerceApp.Application.Interfaces;       // IJwtTokenService
using ECommerceApp.Core.Interfaces;             // IGenericRepository<T>
using ECommerceApp.Infrastructure.Data;         // ApplicationDbContext
using ECommerceApp.Infrastructure.Repositories; // GenericRepository<T>
using ECommerceApp.Infrastructure.Services;     // JwtTokenService
using ECommerceApp.Infrastructure.Whats;        // IWhatsSessionStore, InMemoryWhatsSessionStore
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using ECommerceApp.Infrastructure.Email;




var builder = WebApplication.CreateBuilder(args);

// ---------- PostgreSQL + EF Core ----------
var cs = builder.Configuration.GetConnectionString("Default")
         ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");
var dsb = new NpgsqlDataSourceBuilder(cs);
dsb.EnableDynamicJson();
var dataSource = dsb.Build();

builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(dataSource));

// ---------- Controllers ----------
builder.Services.AddControllers();

// ---------- CORS ----------
const string CorsPolicy = "ng";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(CorsPolicy, p =>
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

// ---------- HttpClient factory (named: "whats") ----------
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("whats", c =>
{
    var baseUrl = builder.Configuration["WhatsGps:BaseUrl"] ?? "https://www.whatsgps.com";
    c.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    c.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
        "text/html,application/xhtml+xml,application/xml;q=0.9,application/json;q=0.8,*/*;q=0.7");
    c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ECommerceApp/1.0 (+http://localhost)");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false,
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
});

// ---------- Whats session store (in-memory) ----------
builder.Services.AddSingleton<IWhatsSessionStore, InMemoryWhatsSessionStore>();

// ---------- JWT ----------
var jwtKey = builder.Configuration["Auth:Jwt:Key"] ?? builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("JWT key missing. Set Auth:Jwt:Key or Jwt:Key.");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
if (keyBytes.Length < 32)
    throw new InvalidOperationException($"JWT key too short: {keyBytes.Length} bytes. HS256 requires >= 32 bytes.");

var signingKey = new SymmetricSecurityKey(keyBytes);

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
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddAuthorization();

// ---------- Services & Repos ----------
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// ---------- Email ----------
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();



// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Make schema IDs unique for nested/duplicate class names
    c.CustomSchemaIds(t =>
        // Use full name and replace '+' (nested type sep) with '_'
        (t.FullName ?? t.Name).Replace('+', '_'));

    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ECommerceApp API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste RAW JWT here (no 'Bearer ' prefix).",
        Name = "Authorization",
        In = ParameterLocation.Header,
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


var app = builder.Build();

// ---------- Pipeline ----------
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
