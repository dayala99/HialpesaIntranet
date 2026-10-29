using System.Text.Json;
namespace Intranet.Service;
public static class ProviderBrowserReply
{
 public static async Task<string?> ReadAsync(TextReader reader,CancellationToken ct)
 {
  var timeout=TimeSpan.FromSeconds(180);
  while(true){
   var line=await reader.ReadLineAsync(ct).AsTask().WaitAsync(timeout,ct);
   if(line==null)return null;
   string? status;try{using var json=JsonDocument.Parse(line);status=json.RootElement.TryGetProperty("status",out var value)?value.GetString():null;}catch(JsonException){return line;}
   if(status is "awaiting-color-confirmation" or "awaiting-mismatch-acknowledgment" or "awaiting-completion-acknowledgment"){timeout=TimeSpan.FromMinutes(15);continue;}
   if(status=="color-confirmed"){timeout=TimeSpan.FromSeconds(180);continue;}
   return line;
  }
 }
}
