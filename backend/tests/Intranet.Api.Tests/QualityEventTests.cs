using Intranet.Entity;
using Intranet.Service;
namespace Intranet.Api.Tests;
public sealed class QualityEventTests {
 static QualityEventUpdate Valid()=>new(){Batch="Y2035",EventCode="001",Auditor="O1234",Location="001",Items=[new(){Sequence=1,Kilos=2.5m}]};
 [Fact]public void ValidInsertAccepted()=>QualityEventService.Validate(Valid());
 [Fact]public void DuplicateItemsRejected(){var r=Valid();r.Items=[new(){Sequence=1},new(){Sequence=1}];Assert.Throws<ArgumentException>(()=>QualityEventService.Validate(r));}
 [Fact]public void RejectionNeedsReason(){var r=Valid();r.Status="R";Assert.Throws<ArgumentException>(()=>QualityEventService.Validate(r));r.Reason="00001";QualityEventService.Validate(r);}
 [Fact]public void CannotModifyMultipleEvents(){var r=Valid();r.Action="U";r.EventSequence=1;r.Items=[new(){Sequence=1},new(){Sequence=2}];Assert.Throws<ArgumentException>(()=>QualityEventService.Validate(r));}
 [Fact]public void KilosCannotBeSilentlyRounded(){var r=Valid();r.Items[0].Kilos=1.234m;Assert.Throws<ArgumentException>(()=>QualityEventService.Validate(r));}
 [Fact]public void DeleteRequiresEventKey(){var r=Valid();r.Action="D";Assert.Throws<ArgumentException>(()=>QualityEventService.Validate(r));}
}
