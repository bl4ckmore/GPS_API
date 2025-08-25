using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using ECommerceApp.Infrastructure.Data;
using ECommerceApp.Core.Interfaces;
using ECommerceApp.Infrastructure.Repositories;
using ECommerceApp.Application.Mapping;
using ECommerceApp.Application.Interfaces;
using ECommerceApp.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog ----
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

// ---- Web host ports ----
builder.WebHost.UseUrls("http://localhost:5000");

// ---- Services ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection.");

    options.UseNpgsql(cs, npg =>
    {
        npg.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        npg.CommandTimeout(180);
    });
#if DEBUG
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
#endif
});

// Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// MediatR & AutoMapper
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(ECommerceApp.Application.Features.Products.Queries.GetProductsQuery).Assembly
    );
});
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// ---- WhatsGPS base ----
var gpsBase = (Environment.GetEnvironmentVariable("GPS_API_BASE")
    ?? builder.Configuration["ExternalApis:WhatsGpsBase"]
    ?? "https://whatsgps.com/").Trim();
if (!gpsBase.EndsWith("/")) gpsBase += "/";

// Named HttpClient for WhatsGPS
builder.Services.AddHttpClient("whats", c =>
{
    c.BaseAddress = new Uri(gpsBase);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    c.DefaultRequestHeaders.UserAgent.ParseAdd("ECommerceApp/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ---- AuthN/AuthZ (JWT) ----
builder.Services.AddMemoryCache();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
  .AddAuthentication("Bearer")
  .AddJwtBearer(options =>
  {
      options.RequireHttpsMetadata = false; // set true behind TLS in prod
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwtIssuer,
          ValidAudience = jwtAudience,
          IssuerSigningKey = signingKey
      };
  });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(ClaimTypes.Role, "Admin") ||
            ctx.User.HasClaim("roleId", "2")));
});

// App services (token + vendor session)
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IWhatsSessionStore, WhatsSessionStore>();

// ---- Build ----
var app = builder.Build();

// ---- Pipeline ----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
// app.UseHttpsRedirection();
app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Startup logs
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine($"✅ API running. URLs: {string.Join(", ", app.Urls)} | ENV: {app.Environment.EnvironmentName}");
    Console.WriteLine($"ℹ️ WhatsGPS Base: {gpsBase}");
});

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await context.Database.ExecuteSqlRawAsync(@"CREATE SCHEMA IF NOT EXISTS public;");
        await context.Database.MigrateAsync();
        Console.WriteLine("✅ Database migrated");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration error: {ex.Message}\n{ex}");
        throw;
    }
}

await app.RunAsync();
