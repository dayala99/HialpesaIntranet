using Intranet.Entity;
using Intranet.Service;
using Intranet.Data;
using System.Security.Cryptography;
namespace Intranet.Api.Tests;
public sealed class ApplicationLoginTests{
 private static ApplicationLoginOptions Options(){
  var salt=RandomNumberGenerator.GetBytes(16);var key=Rfc2898DeriveBytes.Pbkdf2("test-only-password",salt,210000,HashAlgorithmName.SHA256,32);
  return new(){Enabled=true,Username="programador04",PermissionIdentity=@"HIALPESA\programador04",PasswordHash=$"pbkdf2-sha256$210000${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}"};
 }
 [Fact]public void ValidCredentialsCreateMappedAuthenticatedSession(){var options=Options();var user=ApplicationLogin.CreatePrincipal(" PROGRAMADOR04 ","test-only-password",options);Assert.NotNull(user);Assert.True(user.Identity!.IsAuthenticated);Assert.Equal(options.PermissionIdentity,user.Identity.Name);Assert.Equal("programador04",user.FindFirst("display_name")!.Value);}
 [Theory][InlineData("")][InlineData("wrong")][InlineData("test-only-password ")]
 public void MissingOrWrongPasswordIsRejected(string password)=>Assert.Null(ApplicationLogin.CreatePrincipal("programador04",password,Options()));
 [Fact]public void OtherUserCannotUseTheConfiguredPassword()=>Assert.Null(ApplicationLogin.CreatePrincipal("other-user","test-only-password",Options()));
 [Fact]public void DisabledLoginCannotIssueSession(){var options=Options();options.Enabled=false;Assert.Null(ApplicationLogin.CreatePrincipal("programador04","test-only-password",options));}
 [Theory][InlineData("")][InlineData("invalid")][InlineData("pbkdf2-sha256$1$bad$bad")][InlineData("pbkdf2-sha256$210000$bad$bad")]
 public void InvalidHashFailsClosed(string hash)=>Assert.False(ApplicationLogin.VerifyPassword("test",hash));
}