using System.ComponentModel.DataAnnotations;
using Intranet.Api.Contracts;
using Intranet.Api.Infrastructure;
using Intranet.Api.Services;

namespace Intranet.Api.Tests;

public sealed class OrderRegistrationTests
{
    private static readonly OrderReference Reference=new("002","000123");
    private static PendingRequirement Candidate(string key="key",decimal required=11)=>new(key,"001","J2453",1,"Negro","PT","PT000574","Ticket","","","S","","EST-1","",required,QuantityCalculator.RoundRequiredQuantity(required),3.25m);
    private static AddOrderDetailsRequest AddRequest()=>new(){ProductionOrder="J2453",ItemCode="PT000574",RequirementKeys=["key"],UnitPrice=3.25m};
    private static CreateOrderRequest CreateRequest()=>new(){SupplierCode="000000000281",ConditionCode="001",DiscountCode="001",CurrencyCode="USD",DeliveryLocationCode="001",CostCenterCode="001",ManufacturingGroup="VNC-001",DeliveryStart=DateTime.Today,DeliveryEnd=DateTime.Today.AddDays(3)};

    [Fact] public async Task HeaderAndGroupAreCommittedTogether()
    {
        var fake=new FakeSession();var result=await new OrderRegistrationService(new FakeGateway(fake)).Create(CreateRequest(),"tester","DOMAIN\\tester",default);
        Assert.Equal(Reference,result);Assert.Equal(new[]{"header","group","commit","dispose"},fake.Events);
    }
    [Fact] public async Task GroupFailureDisposesWithoutCommit()
    {
        var fake=new FakeSession{FailGroup=true};
        await Assert.ThrowsAsync<InvalidOperationException>(()=>new OrderRegistrationService(new FakeGateway(fake)).Create(CreateRequest(),"tester","DOMAIN\\tester",default));
        Assert.False(fake.Committed);Assert.Contains("dispose",fake.Events);
    }
    [Fact] public async Task PurchaseIsRoundedWhileRequiredAndExplicitPriceArePreserved()
    {
        var fake=new FakeSession();var result=await new OrderRegistrationService(new FakeGateway(fake)).Add(Reference,AddRequest(),"tester","DOMAIN\\tester",default);
        Assert.Equal(1,result.ItemsAdded);Assert.Equal(11,fake.Inserted.Single().Required);
        Assert.Equal(("001",15m,3.25m),fake.Adjusted.Single());Assert.True(fake.Committed);
    }
    [Theory][InlineData(11,15)][InlineData(16,20)][InlineData(28,30)][InlineData(10,10)][InlineData(20,20)][InlineData(15,15)]
    [InlineData(130.49,130)][InlineData(130.5,135)][InlineData(155.4,155)][InlineData(155.5,160)]
    public async Task SavedQuantityUsesBusinessRule(decimal required,decimal expected)
    {
        var fake=new FakeSession{CandidateRow=Candidate(required:required)};
        await new OrderRegistrationService(new FakeGateway(fake)).Add(Reference,AddRequest(),"tester","DOMAIN\\tester",default);
        Assert.Equal(expected,fake.Adjusted.Single().Quantity);
    }
    [Fact] public async Task AuthorizedOrderCannotReceiveNewDetails()
    {
        var fake=new FakeSession{Status="A"};
        await Assert.ThrowsAsync<RegistrationConflictException>(()=>new OrderRegistrationService(new FakeGateway(fake)).Add(Reference,AddRequest(),"tester","DOMAIN\\tester",default));
        Assert.Empty(fake.Inserted);Assert.False(fake.Committed);
    }
    [Fact] public async Task ChangedBalanceRejectsStaleSelection()
    {
        var fake=new FakeSession{CandidateRow=Candidate("new-key")};
        await Assert.ThrowsAsync<RegistrationConflictException>(()=>new OrderRegistrationService(new FakeGateway(fake)).Add(Reference,AddRequest(),"tester","DOMAIN\\tester",default));
        Assert.Empty(fake.Inserted);Assert.False(fake.Committed);
    }
    [Fact] public async Task FailedAdjustmentPreventsCommit()
    {
        var fake=new FakeSession{FailAdjustment=true};
        await Assert.ThrowsAsync<InvalidOperationException>(()=>new OrderRegistrationService(new FakeGateway(fake)).Add(Reference,AddRequest(),"tester","DOMAIN\\tester",default));
        Assert.False(fake.Committed);Assert.Contains("dispose",fake.Events);
    }
    [Fact] public async Task RepeatedRequirementKeysAreRejectedBeforeOpeningSession()
    {
        var fake=new FakeSession();var request=AddRequest();request.RequirementKeys.Add("key");
        await Assert.ThrowsAsync<ArgumentException>(()=>new OrderRegistrationService(new FakeGateway(fake)).Add(Reference,request,"tester","DOMAIN\\tester",default));
        Assert.Empty(fake.Events);
    }
    [Fact] public void ExistingProceduresReceiveExactParametersIncludingPrice()
    {
        var header=RegistrationParameters.Header(CreateRequest());Assert.Equal("I",header.Get<string>("OPC_MANT"));Assert.Equal("01",header.Get<string>("COD_CLAORDCOMP"));Assert.Equal("N",header.Get<string>("FLG_TRANSACCION"));
        var detail=RegistrationParameters.Detail(Reference,"000",Candidate());Assert.Equal("C",detail.Get<string>("ACCION"));Assert.Equal(11,detail.Get<decimal>("CANTIDAD"));Assert.Equal("N",detail.Get<string>("FLG_TRANSACCION"));
        var purchase=RegistrationParameters.Purchase(Reference,"001",15,3.25m);Assert.Equal(15,purchase.Get<decimal>("Can_Comprada"));Assert.Equal(3.25m,purchase.Get<decimal>("pre_unitario"));
    }
    [Fact] public void InvalidDeliveryIntervalFailsModelValidation()
    {
        var request=CreateRequest();request.DeliveryEnd=request.DeliveryStart.AddDays(-1);
        Assert.False(Validator.TryValidateObject(request,new ValidationContext(request),new List<ValidationResult>(),true));
    }

    private sealed class FakeGateway(FakeSession session):IOrderWriteGateway
    {
        public Task<IOrderWriteSession> Begin(string gestionUser,string windowsUser,CancellationToken ct)=>Task.FromResult<IOrderWriteSession>(session);
    }
    private sealed class FakeSession:IOrderWriteSession
    {
        public List<string> Events {get;}=[];
        public List<PendingRequirement> Inserted {get;}=[];
        public List<(string Sequence,decimal Quantity,decimal Price)> Adjusted {get;}=[];
        public bool Committed,FailGroup,FailAdjustment;
        public string Status="P";
        public PendingRequirement CandidateRow=Candidate();
        public Task<OrderReference> CreateHeader(CreateOrderRequest request,CancellationToken ct){Events.Add("header");return Task.FromResult(Reference);}
        public Task LinkGroup(OrderReference order,string group,CancellationToken ct){Events.Add("group");if(FailGroup)throw new InvalidOperationException("fake group error");return Task.CompletedTask;}
        public Task<OrderRegistrationHeader> LockOrder(OrderReference order,CancellationToken ct){Events.Add("lock");return Task.FromResult(new OrderRegistrationHeader(order.Series,order.Number,"P",Status,"01","VNC-001"));}
        public Task<IReadOnlyList<PendingRequirement>> Pending(OrderRegistrationHeader order,string op,string item,CancellationToken ct)=>Task.FromResult<IReadOnlyList<PendingRequirement>>([CandidateRow]);
        public Task<IReadOnlyList<SavedItem>> Items(OrderReference order,CancellationToken ct)=>Task.FromResult<IReadOnlyList<SavedItem>>(Inserted.Count==0?[]:[new("001",Inserted.Sum(r=>r.Required),0)]);
        public Task InsertRequirement(OrderReference order,string previous,PendingRequirement requirement,CancellationToken ct){Inserted.Add(requirement);return Task.CompletedTask;}
        public Task SetPurchase(OrderReference order,string sequence,decimal quantity,decimal unitPrice,CancellationToken ct){if(FailAdjustment)throw new InvalidOperationException("fake adjustment error");Adjusted.Add((sequence,quantity,unitPrice));return Task.CompletedTask;}
        public Task Commit(CancellationToken ct){Committed=true;Events.Add("commit");return Task.CompletedTask;}
        public ValueTask DisposeAsync(){Events.Add("dispose");return ValueTask.CompletedTask;}
    }
}
