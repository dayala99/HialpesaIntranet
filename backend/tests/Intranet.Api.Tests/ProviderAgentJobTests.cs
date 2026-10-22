using Intranet.Entity;
using Intranet.Service;
using Intranet.Data;
using System.Security.Cryptography;
using System.Text.Json;
namespace Intranet.Api.Tests;
public sealed class ProviderAgentJobTests
{
 [Fact] public void SignedJobVerifiesAndTamperingFails()
 {
  using var rsa=RSA.Create(2048);
  var bytes=ProviderAgentJob.Create(new{po="PO-00054154",rows=new[]{new{purchased=135}}},rsa.ExportPkcs8PrivateKeyPem());
  using var envelope=JsonDocument.Parse(bytes);
  var data=Convert.FromBase64String(envelope.RootElement.GetProperty("payload").GetString()!);
  var signature=Convert.FromBase64String(envelope.RootElement.GetProperty("signature").GetString()!);
  Assert.True(rsa.VerifyData(data,signature,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1));
  data[0]^=1;
  Assert.False(rsa.VerifyData(data,signature,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1));
  Assert.DoesNotContain("PRIVATE KEY",System.Text.Encoding.UTF8.GetString(bytes));
 }
}
