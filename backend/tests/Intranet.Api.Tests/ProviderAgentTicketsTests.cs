using Intranet.Api.Infrastructure;
namespace Intranet.Api.Tests;
public sealed class ProviderAgentTicketsTests
{
 [Fact]public void ReceiptIsFalseUntilAgentRedeems(){var tickets=new ProviderAgentTickets();var t=tickets.Issue([1]);Assert.False(tickets.WasReceived(t.Token));tickets.Redeem(t.Token);Assert.True(tickets.WasReceived(t.Token));Assert.Null(tickets.Redeem(t.Token));Assert.True(tickets.WasReceived(t.Token));}
 [Fact]public void TicketCanBeConsumedOnlyOnce(){var tickets=new ProviderAgentTickets();var t=tickets.Issue([1,2,3]);Assert.Matches("^[A-F0-9]{64}$",t.Token);Assert.Equal(new byte[]{1,2,3},tickets.Redeem(t.Token));Assert.Null(tickets.Redeem(t.Token));}
 [Fact]public void ExpiredTicketIsRejectedAndRemoved(){var tickets=new ProviderAgentTickets();var t=tickets.Issue([1]);Assert.Null(tickets.Redeem(t.Token,t.ExpiresAt));Assert.Null(tickets.Redeem(t.Token));}
 [Fact]public void UnknownTokenDoesNotConsumeAnotherTicket(){var tickets=new ProviderAgentTickets();var t=tickets.Issue([9]);Assert.Null(tickets.Redeem(new string('A',64)));Assert.NotNull(tickets.Redeem(t.Token));}
 [Fact]public async Task ParallelRedemptionsHaveOneWinner(){var tickets=new ProviderAgentTickets();var t=tickets.Issue([1]);var results=await Task.WhenAll(Enumerable.Range(0,20).Select(_=>Task.Run(()=>tickets.Redeem(t.Token))));Assert.Single(results.Where(x=>x is not null));}
}

