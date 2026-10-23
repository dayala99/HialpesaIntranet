using Intranet.Entity;
using Intranet.Service;
namespace Intranet.Api.Tests;
public sealed class DyeingFilterTests
{
    [Theory]
    [InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)]
    public void RequiredFieldsAreCheckedForEachMode(int mode) => Assert.Throws<ArgumentException>(() => DyeingService.Validate(new DyeingFilter { Mode = mode }));
    [Theory]
    [InlineData(3)][InlineData(7)]
    public void ReversedDatesAreRejected(int mode) => Assert.Throws<ArgumentException>(() => DyeingService.Validate(new DyeingFilter { Mode = mode, Client = "00001", Fiber = "A", From = new(2026,10,22), To = new(2026,10,21) }));
    [Theory]
    [InlineData(3)][InlineData(7)]
    public void SameDayRangeIsAllowed(int mode) => DyeingService.Validate(new DyeingFilter { Mode = mode, Client = "00001", Fiber = "A", From = new(2026,10,22), To = new(2026,10,22) });
    [Fact] public void AlphanumericBatchIsPreserved() => DyeingService.Validate(new DyeingFilter { Batch = "P2843" });
    [Fact] public void NegativePageIsRejected() => Assert.Throws<ArgumentException>(() => DyeingService.Validate(new DyeingFilter { Batch = "P2843", Page = -1 }));
}
