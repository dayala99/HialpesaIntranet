using Intranet.Entity;
using Intranet.Service;
using Microsoft.Extensions.Logging.Abstractions;
namespace Intranet.Api.Tests;
public sealed class PcpMailTests {
 [Fact]public async Task EmptyQueueStopsWithoutSending(){var repo=new Repo(new PcpMailDispatch(false,false,null));var count=await Worker(repo).DispatchBatch(default);Assert.Equal(0,count);Assert.Equal(1,repo.Calls);}
 [Fact]public async Task EachPendingNotificationIsProcessedOncePerBatch(){var repo=new Repo(new PcpMailDispatch(true,true,null),new PcpMailDispatch(true,true,null),new PcpMailDispatch(false,false,null));Assert.Equal(2,await Worker(repo).DispatchBatch(default));Assert.Equal(3,repo.Calls);}
 [Fact]public async Task QueueFailureDoesNotDiscardOtherPendingNotifications(){var repo=new Repo(new PcpMailDispatch(true,false,"Sin acceso al perfil"),new PcpMailDispatch(true,true,null),new PcpMailDispatch(false,false,null));Assert.Equal(2,await Worker(repo).DispatchBatch(default));}
 [Fact]public async Task BatchIsBounded(){var repo=new Repo();Assert.Equal(20,await Worker(repo).DispatchBatch(default));Assert.Equal(20,repo.Calls);}
 [Fact]public async Task ShutdownDoesNotSend(){var repo=new Repo();using var ct=new CancellationTokenSource();ct.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Worker(repo).DispatchBatch(ct.Token));Assert.Equal(0,repo.Calls);}
 private static PcpMailDispatcher Worker(Repo repo)=>new(repo,NullLogger<PcpMailDispatcher>.Instance);
 private sealed class Repo(params PcpMailDispatch[] results):IPcpMailRepository {public int Calls;public Task<PcpMailDispatch> Dispatch(CancellationToken ct){var index=Calls++;return Task.FromResult(index<results.Length?results[index]:new PcpMailDispatch(true,true,null));}}
}
