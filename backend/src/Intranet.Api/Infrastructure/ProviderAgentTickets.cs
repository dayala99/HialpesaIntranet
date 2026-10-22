using System.Security.Cryptography;
namespace Intranet.Api.Infrastructure;
public sealed class ProviderAgentTickets
{
 private sealed record Entry(byte[] Data,DateTimeOffset Expires);
 private readonly Dictionary<string,Entry> tickets=[];
 private readonly Dictionary<string,DateTimeOffset> received=[];
 private readonly object gate=new();
 public (string Token,DateTimeOffset ExpiresAt) Issue(byte[] data)
 {
  lock(gate){
   var now=DateTimeOffset.UtcNow;
   foreach(var key in tickets.Where(x=>x.Value.Expires<=now).Select(x=>x.Key).ToArray())tickets.Remove(key);
   foreach(var key in received.Where(x=>x.Value<=now).Select(x=>x.Key).ToArray())received.Remove(key);
   if(tickets.Count>=100)throw new ArgumentException("Hay demasiadas aperturas pendientes. Espera dos minutos e inténtalo nuevamente.");
   var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));var expires=now.AddMinutes(2);
   tickets.Add(token,new(data,expires));return(token,expires);
  }
 }
 public bool WasReceived(string token){lock(gate){return received.TryGetValue(token,out var expires)&&expires>DateTimeOffset.UtcNow;}}
 public byte[]? Redeem(string token,DateTimeOffset? now=null)
 {
  lock(gate){if(!tickets.Remove(token,out var entry)||entry.Expires<=(now??DateTimeOffset.UtcNow))return null;received[token]=entry.Expires;return entry.Data;}
 }
}

