using Intranet.Api.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
namespace Intranet.Api.Tests;
public sealed class SharedErrorLogTests
{
    [Fact]
    public async Task RecordsContextAndExceptionWithoutRequestSecrets()
    {
        var root = Path.Combine(Path.GetTempPath(), "intranet-log-" + Guid.NewGuid());
        try
        {
            var context = new DefaultHttpContext { TraceIdentifier = "trace-prueba", User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,"programador04")],"test")) };
            context.Request.Method = "POST"; context.Request.Path = "/api/v1/logistica/importaciones/control";
            context.Request.QueryString = new QueryString("?token=QUERY_SECRET"); context.Request.Headers.Authorization = "Bearer HEADER_SECRET";
            using var provider = new SharedErrorLogProvider(root, root, new HttpContextAccessor { HttpContext = context });
            var logger = provider.CreateLogger("Test");
            logger.LogInformation("No crear archivo");
            logger.LogError(new InvalidOperationException("password=PASSWORD_SECRET; token=TOKEN_SECRET"), "Error de guardado");
            await provider.FlushAsync();
            var text = File.ReadAllText(Assert.Single(Directory.GetFiles(root, "*.json")));
            using var document = JsonDocument.Parse(text);
            Assert.Equal("trace-prueba",document.RootElement.GetProperty("traceId").GetString());
            Assert.Equal("programador04",document.RootElement.GetProperty("user").GetString());
            Assert.Contains("InvalidOperationException",text);
            foreach(var secret in new[]{"QUERY_SECRET","HEADER_SECRET","PASSWORD_SECRET","TOKEN_SECRET"}) Assert.DoesNotContain(secret,text);
        }
        finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }
    [Fact]
    public async Task FallsBackWhenDestinationIsNotWritableDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "intranet-log-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        try
        {
            var blocked = Path.Combine(root,"not-a-directory");File.WriteAllText(blocked,"test");
            var fallback = Path.Combine(root,"fallback");
            using var provider = new SharedErrorLogProvider(blocked,fallback,new HttpContextAccessor());
            provider.CreateLogger("Test").LogError("Error que debe conservarse");
            await provider.FlushAsync();Assert.Single(Directory.GetFiles(fallback,"*.json"));
        }
        finally { Directory.Delete(root,true); }
    }
    [Fact]
    public async Task ConcurrentErrorsGetSeparateValidFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "intranet-log-" + Guid.NewGuid());
        try
        {
            using var provider = new SharedErrorLogProvider(root,root,new HttpContextAccessor());
            var logger=provider.CreateLogger("Test");
            await Task.WhenAll(Enumerable.Range(0,20).Select(i=>Task.Run(()=>logger.LogError("Error {Index}",i))));
            await provider.FlushAsync();var files=Directory.GetFiles(root,"*.json");Assert.Equal(20,files.Length);
            foreach(var file in files)using(JsonDocument.Parse(File.ReadAllText(file))){}
        }
        finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }
}
