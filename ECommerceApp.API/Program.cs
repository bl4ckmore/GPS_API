using System.Reflection;
using System.Security.Claims;
using System.Text;
using ECommerceApp.API.Auth;
using ECommerceApp.Application.Interfaces;
using ECommerceApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// ===== HttpClient(s)
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("whats", c =>
{
    var baseUrl = builder.Configuration["WhatsGps:BaseUrl"] ?? "https://www.whatsgps.com";
    c.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    c.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
    c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ECommerceApp/1.0 (+http://localhost)");
});

// ===== Caching
builder.Services.AddMemoryCache();

// ===== CORS
const string CorsPolicy = "ng-dev";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsPolicy, p =>
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// ===== PostgreSQL + EF Core
var cs = builder.Configuration.GetConnectionString("Default")
         ?? throw new InvalidOperationException("ConnectionStrings:Default is missing.");

var dsb = new NpgsqlDataSourceBuilder(cs);
dsb.EnableDynamicJson();
var dataSource = dsb.Build();

builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(dataSource));

// ===== Controllers
builder.Services.AddControllers();

// ===== Authentication (JWT Bearer OR Header)
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

var jwtKey = builder.Configuration["Auth:Jwt:Key"] ?? builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("JWT key missing. Set Auth:Jwt:Key or Jwt:Key.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Multi";
        options.DefaultChallengeScheme = "Multi";
    })
    .AddPolicyScheme("Multi", "BearerOrHeader", options =>
    {
        options.ForwardDefaultSelector = ctx =>
            ctx.Request.Headers.ContainsKey("Authorization")
                ? JwtBearerDefaults.AuthenticationScheme
                : HeaderAuthHandler.SchemeName;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(2),

            // IMPORTANT: use the standard Role claim; we’ll normalize other shapes below
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    })
    .AddScheme<AuthenticationSchemeOptions, HeaderAuthHandler>(
        HeaderAuthHandler.SchemeName, _ => { });

builder.Services.AddSingleton<IClaimsTransformation, RoleNormalizer>();
builder.Services.AddAuthorization();

// ===== Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ECommerceApp API", Version = "v1" });

    c.AddSecurityDefinition(HeaderAuthHandler.SchemeName, new OpenApiSecurityScheme
    {
        Name = HeaderAuthHandler.HeaderName,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Custom header auth. Set X-WhatsGPS-UserId to impersonate a user (admin if configured)."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.\nExample: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // So IFormFile appears as file upload input (and avoids the generator crash)
    c.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
});

RegisterIfFound<IJwtTokenService>(builder.Services);
RegisterIfFound<IWhatsSessionStore>(builder.Services);

var app = builder.Build();

// ===== PathBase (/API) to match your EC_API_BASE
var apiBase = "/API";
app.UsePathBase(apiBase);

// Dev redirect "/" -> "/API/swagger"
if (app.Environment.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        if (string.Equals(ctx.Request.Path, "/", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.Redirect($"{apiBase}/swagger");
            return;
        }
        await next();
    });
}

// ===== Static files
var webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRoot);
Directory.CreateDirectory(Path.Combine(webRoot, "uploads"));

app.UseStaticFiles(); // serves /API/* (because of PathBase)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(webRoot, "uploads")),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
        c.PreSerializeFilters.Add((swagger, req) =>
        {
            swagger.Servers = new List<OpenApiServer> {
                new OpenApiServer { Url = $"{req.Scheme}://{req.Host.Value}{apiBase}" }
            };
        });
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint($"{apiBase}/swagger/v1/swagger.json", "ECommerceApp API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// ===== helpers
static void RegisterIfFound<TService>(IServiceCollection services)
{
    var iface = typeof(TService);
    var impl = AppDomain.CurrentDomain
        .GetAssemblies()
        .Where(a =>
            a.FullName != null &&
            (a.FullName.StartsWith("ECommerceApp.Infrastructure", StringComparison.OrdinalIgnoreCase)
             || a.GetName().Name?.Equals("ECommerceApp.Infrastructure", StringComparison.OrdinalIgnoreCase) == true))
        .SelectMany(a =>
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
        })
        .FirstOrDefault(t => t != null && iface.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

    if (impl is not null)
        services.TryAddSingleton(iface, impl);
    else
        throw new InvalidOperationException($"No implementation of {iface.FullName} found in Infrastructure.");
}

// Normalizes roles so "Admin"/"admin" are available as ClaimTypes.Role,
// and also copies from tokens that use "role"/"roles".
public sealed class RoleNormalizer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity id || !id.IsAuthenticated)
            return Task.FromResult(principal);

        var roleValues = id.FindAll(ClaimTypes.Role).Select(c => c.Value)
                           .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var c in id.FindAll("role").Concat(id.FindAll("roles")))
            roleValues.Add(c.Value);

        var toAdd = new List<Claim>();
        foreach (var r in roleValues.ToArray())
        {
            if (!id.HasClaim(ClaimTypes.Role, r))
                toAdd.Add(new Claim(ClaimTypes.Role, r));

            var lower = r.ToLowerInvariant();
            var upper = char.ToUpperInvariant(r[0]) + r[1..].ToLowerInvariant();

            if (!id.HasClaim(ClaimTypes.Role, lower))
                toAdd.Add(new Claim(ClaimTypes.Role, lower));
            if (!id.HasClaim(ClaimTypes.Role, upper))
                toAdd.Add(new Claim(ClaimTypes.Role, upper));
        }

        if (toAdd.Count > 0) id.AddClaims(toAdd);
        return Task.FromResult(principal);
    }
}
