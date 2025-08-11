using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.Middleware;
using MultiDeptReportingTool.Services;
using MultiDeptReportingTool.Services.DepartmentSpecific;
using MultiDeptReportingTool.Services.Analytics;
using MultiDeptReportingTool.Services.Export;
using MultiDeptReportingTool.Services.Interfaces;
using MultiDeptReportingTool.Services.AI;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register application services
builder.Services.AddScoped<ComprehensiveDataSeedingService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString!));

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IApiSecurityService, ApiSecurityService>();
builder.Services.AddScoped<IDepartmentReportService, DepartmentReportService>();

// Register Phase 2: Enhanced RBAC and Audit Services
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<SecuritySeedingService>();

// Register Phase 4 Analytics and Export Services
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<DepartmentSpecificSeedingService>();
builder.Services.AddScoped<ComprehensiveDataSeedingService>();

// Register Phase 3: Advanced Analytics & AI Services
builder.Services.AddScoped<IMLPredictionService, MLPredictionService>();

// Register background services
builder.Services.AddHostedService<TokenCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowAngularApp");

// Add security headers middleware first
app.UseMiddleware<MultiDeptReportingTool.Middleware.SecurityHeadersMiddleware>();

// Add API security middleware
app.UseMiddleware<MultiDeptReportingTool.Middleware.ApiSecurityMiddleware>();

app.UseRouting();

// Add rate limiting middleware before authentication
app.UseRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

// Only use HTTPS redirection in production or when HTTPS port is configured
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();