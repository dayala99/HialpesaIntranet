using Intranet.Entity;
namespace Intranet.Api.Tests;
public sealed class ImportProvisionTests {
 [Theory][InlineData("R","provision")][InlineData("P","reverse")]public void SameButtonSelectsOriginalGestionOperation(string status,string action)=>Assert.Equal(action,ImportControlProvisioning.ActionForStatus(status));
 [Theory][InlineData("L")][InlineData("A")][InlineData("")]public void OtherStatesCannotChangeAccounting(string status)=>Assert.Throws<ArgumentException>(()=>ImportControlProvisioning.ActionForStatus(status));
 [Fact]public void RepeatedRequestCannotReverseTheCompletedProvision()=>Assert.Throws<RegistrationConflictException>(()=>ImportControlProvisioning.ValidateSnapshot("P",new('A',64),new(new('A',64),"provision")));
 [Fact]public void ChangedSnapshotIsRejected()=>Assert.Throws<RegistrationConflictException>(()=>ImportControlProvisioning.ValidateSnapshot("R",new('B',64),new(new('A',64),"provision")));
 [Fact]public void ValidProvisionAndReversalPassSnapshotCheck(){ImportControlProvisioning.ValidateSnapshot("R",new('A',64),new(new('A',64),"provision"));ImportControlProvisioning.ValidateSnapshot("P",new('B',64),new(new('B',64),"reverse"));}
}
