using Intranet.Api.Contracts;
using Intranet.Api.Services;

namespace Intranet.Api.Tests;

public sealed class QuantityCalculatorTests
{
    private static MatrixRow Row(decimal quantity, string np = "NP-1") => new() { Factory="001", ProductionOrder=np, Style="EST-1", Color="Negro", Size="S", Quantity=quantity };
    private static ArticleRule Article(string code, int multiple) => new() { ItemCode=code, Description=code, Multiple=multiple };

    [Theory]
    [InlineData(21,5,25)]
    [InlineData(25,5,25)]
    [InlineData(11,5,15)]
    [InlineData(16,5,20)]
    [InlineData(28,5,30)]
    [InlineData(10,5,10)]
    [InlineData(20,5,20)]
    [InlineData(15,5,15)]
    [InlineData(5,5,5)]
    [InlineData(1,5,5)]
    [InlineData(29,5,30)]
    [InlineData(0,5,0)]
    [InlineData(12.48,5,15)]
    [InlineData(121.35,5,125)]
    [InlineData(121.65,5,125)]
    [InlineData(126.67,5,130)]
    [InlineData(130.5,5,135)]
    [InlineData(130.49,5,130)]
    [InlineData(155.5,5,160)]
    [InlineData(155.40,5,155)]
    [InlineData(130.49999,5,130)]
    [InlineData(130.50001,5,135)]
    [InlineData(155.49999,5,155)]
    [InlineData(155.50001,5,160)]
    [InlineData(129.99,5,130)]
    [InlineData(134.99,5,135)]
    [InlineData(0.49,5,0)]
    [InlineData(0.5,5,5)]
    public void UsesBusinessRoundingAndPreservesOriginalDemand(decimal required, int multiple, decimal expected)
    {
        var result = new QuantityCalculator().Calculate(new() { Rows=[Row(required)], Articles=[Article("A",multiple)] });
        Assert.Equal(expected, result.Rows.Single().Purchase);
        Assert.Equal(required, result.BaseUnits);
        Assert.Equal(expected-required, result.ExtraUnits);
    }

    [Fact] public void SeparateArticlesUseTheSameAutomaticRuleAndKeepBaseQuantity()
    {
        var result = new QuantityCalculator().Calculate(new() { Rows=[Row(21)], Articles=[Article("A",5),Article("B",5)] });
        Assert.Equal(21, result.BaseUnits);
        Assert.Equal(50, result.PurchaseUnits);
        Assert.Equal(8, result.ExtraUnits);
    }
    [Fact] public void SameSizeAndColorFromDifferentOrdersRemainSeparate()
    {
        var result = new QuantityCalculator().Calculate(new() { Rows=[Row(21,"NP-1"),Row(21,"NP-2")], Articles=[Article("A",5)] });
        Assert.Equal(2,result.Rows.Count);
        Assert.Equal(50,result.PurchaseUnits);
    }
    [Fact] public void DuplicatedSourceRowsAreRejected() => Assert.Throws<ArgumentException>(() => new QuantityCalculator().Calculate(new() { Rows=[Row(21),Row(21)], Articles=[Article("A",5)] }));
    [Fact] public void InvalidMultipleIsRejected() => Assert.Throws<ArgumentException>(() => new QuantityCalculator().Calculate(new() { Rows=[Row(21)], Articles=[Article("A",0)] }));
    [Fact] public void ManualMultipleTenIsRejected() => Assert.Throws<ArgumentException>(() => new QuantityCalculator().Calculate(new() { Rows=[Row(11)], Articles=[Article("A",10)] }));
    [Fact] public void DuplicateArticlesAreRejected() => Assert.Throws<ArgumentException>(() => new QuantityCalculator().Calculate(new() { Rows=[Row(21)], Articles=[Article("A",5),Article("a",5)] }));
}
