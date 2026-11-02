using Intranet.Data;
using Intranet.Entity;
using Intranet.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
namespace Intranet.Api.Tests;
public sealed class AnalisisRbPendingTests {
 [Theory][InlineData(2," j1907 ","J1907")][InlineData(1,"J1907",null)][InlineData(3,"J1907",null)][InlineData(2,"",null)][InlineData(2,"F-J1907",null)]public void OnlyNpDetailRowsAreIncluded(int type,string value,string? expected)=>Assert.Equal(expected,AnalisisRbPendingRepository.NormalizeNp(type,value));
 [Theory][InlineData("oc","OK",true)][InlineData("oc","N/A",false)][InlineData("oc","FALTA",false)][InlineData("tariff","D",true)][InlineData("tariff","P",false)][InlineData("sheet","SI",true)][InlineData("sheet","NO",false)][InlineData("sheet","",false)]public void StatusIconsMatchReport(string kind,string value,bool expected)=>Assert.Equal(expected,AnalisisRbPendingRepository.StatusReady([value],kind));
 [Fact]public void AnyPendingLineKeepsNpRed()=>Assert.False(AnalisisRbPendingRepository.StatusReady(["OK","FALTA"],"oc"));
 [Fact]public async Task LoadingIsSharedAndRefreshCreatesOneNewReport(){var repo=new Repo();using var provider=new ServiceCollection().AddSingleton<IAnalisisRbPendingRepository>(repo).BuildServiceProvider();var cache=new AnalisisRbPendingCache(provider.GetRequiredService<IServiceScopeFactory>(),new Lifetime(),NullLogger<AnalisisRbPendingCache>.Instance);Assert.Equal("loading",cache.Read(false).Status);Assert.Equal("loading",cache.Read(true).Status);await repo.Entered.Task.WaitAsync(TimeSpan.FromSeconds(3));Assert.Equal(1,repo.Calls);repo.Complete.SetResult(new AnalisisRbPendingResult([new("J1907",438)],954,866,DateTime.UtcNow));for(var i=0;i<300&&cache.Read(false).Status=="loading";i++)await Task.Delay(10);Assert.Equal("ready",cache.Read(false).Status);Assert.Single(cache.Read(false).Result!.Rows);Assert.Equal(1,repo.Calls);Assert.Equal("loading",cache.Read(true).Status);}
 private sealed class Repo:IAnalisisRbPendingRepository{public int Calls;public TaskCompletionSource<bool> Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);public TaskCompletionSource<AnalisisRbPendingResult> Complete=new(TaskCreationOptions.RunContinuationsAsynchronously);public Task<AnalisisRbPendingResult> Read(CancellationToken ct){Interlocked.Increment(ref Calls);Entered.TrySetResult(true);return Complete.Task;}}
 private sealed class Lifetime:IHostApplicationLifetime{public CancellationToken ApplicationStarted=>CancellationToken.None;public CancellationToken ApplicationStopping=>CancellationToken.None;public CancellationToken ApplicationStopped=>CancellationToken.None;public void StopApplication(){}}
}

