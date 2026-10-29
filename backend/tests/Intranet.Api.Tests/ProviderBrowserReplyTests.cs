using Intranet.Service;
namespace Intranet.Api.Tests;
public sealed class ProviderBrowserReplyTests
{
 [Fact]public async Task WaitsForBrowserColorConfirmation(){using var reader=new StringReader("{\"status\":\"awaiting-color-confirmation\"}\n{\"status\":\"color-confirmed\"}\n{\"status\":\"signed-in\"}\n");Assert.Contains("signed-in",await ProviderBrowserReply.ReadAsync(reader,default));}
 [Theory][InlineData("awaiting-mismatch-acknowledgment","failed")][InlineData("awaiting-completion-acknowledgment","po-filled")][InlineData("awaiting-color-confirmation","cancelled")]
 public async Task WaitsForUserThenReturnsTerminalStatus(string waiting,string terminal){using var reader=new StringReader($"{{\"status\":\"{waiting}\"}}\n{{\"status\":\"{terminal}\"}}\n");Assert.Contains(terminal,await ProviderBrowserReply.ReadAsync(reader,default));}
 [Fact]public async Task EndOfStreamDoesNotLoop(){using var reader=new StringReader("");Assert.Null(await ProviderBrowserReply.ReadAsync(reader,default));}
}
