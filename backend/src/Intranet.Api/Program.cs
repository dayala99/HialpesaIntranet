using Intranet.Api.Infrastructure;
using Intranet.Api.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true).AddEnvironmentVariables();
var mode = builder.Configuration["Gestion:Mode"] ?? "Demo";
if (mode is not ("Demo" or "SqlServer")) throw new InvalidOperationException("Gestion:Mode debe ser Demo o SqlServer.");
if (!builder.Environment.IsDevelopment() && mode == "Demo") throw new InvalidOperationException("El modo Demo solo se permite en Development.");
var demo = builder.Environment.IsDevelopment() && mode == "Demo";
builder.Services.Configure<GestionOptions>(builder.Configuration.GetSection("Gestion"));
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
builder.Services.AddAuthorization(options => options.FallbackPolicy = demo
    ? new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build()
    : new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("LocalAngular", policy => policy
    .WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<GestionConnectionFactory>();
builder.Services.AddScoped<GestionIdentity>();
builder.Services.AddScoped<AviosRepository>();
builder.Services.AddSingleton<QuantityCalculator>();
builder.Services.AddScoped<RegistrationReadRepository>();
builder.Services.AddScoped<IOrderWriteGateway, SqlOrderWriteGateway>();
builder.Services.AddScoped<OrderRegistrationService>();
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context => {
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var denied = exception is UnauthorizedAccessException;
    var conflict = exception is RegistrationConflictException;
    var business = exception is ArgumentException || exception is Microsoft.Data.SqlClient.SqlException { Number: 50000 };
    context.Response.StatusCode = denied ? 403 : conflict ? 409 : business ? 400 : 500;
    if (!denied) app.Logger.LogError(exception, "Error en la API. TraceId: {TraceId}", context.TraceIdentifier);
    await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = context.Response.StatusCode, Title = denied ? "Tu cuenta no tiene permiso para esta operación." : conflict || business ? exception!.Message : "No se pudo completar la operación. Revisa la conexión o contacta a soporte.", Extensions = { ["traceId"] = context.TraceIdentifier } });
}));
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
if (app.Environment.IsDevelopment()) app.UseCors("LocalAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", mode, databaseWritesEnabled = builder.Configuration.GetValue<bool>("Gestion:EnableOrderRegistration") })).AllowAnonymous();
app.MapControllers();
app.Run();
public partial class Program { }
