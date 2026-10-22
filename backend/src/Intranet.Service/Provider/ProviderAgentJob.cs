using Microsoft.Extensions.Hosting;
using Intranet.Entity;
using System.Security.Cryptography;
using System.Text.Json;
namespace Intranet.Service;

public static class ProviderAgentJob
{
    public static byte[] Create(object payload, string privateKey)
    {
        using var rsa = RSA.Create(); rsa.ImportFromPem(privateKey);
        var data = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return JsonSerializer.SerializeToUtf8Bytes(new { payload = Convert.ToBase64String(data), signature = Convert.ToBase64String(rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) });
    }
}
