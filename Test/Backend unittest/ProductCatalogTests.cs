using backend.Models;
using Xunit;

namespace UbhayaFashions.Tests;

public class ProductCatalogTests
{
    [Fact]
    public void Normalize_UsesCanonicalNamesAndDropsDuplicates()
    {
        var stored = ProductColorCatalog.Normalize(" pink, Pink, navy blue ");

        Assert.Equal("Pink,Navy Blue", stored);
    }

    [Theory]
    [InlineData("Maroon,Gold", "Gold", true)]
    [InlineData("Maroon,Gold", "Red", false)]
    [InlineData("Pink, Beige", "pink", true)]
    [InlineData("Blue,Black", "Black", true)]
    [InlineData("", "Pink", false)]
    [InlineData("Pink", "", true)]
    public void Matches_ComparesWholeColorNames(string stored, string selected, bool expected)
    {
        Assert.Equal(expected, ProductColorCatalog.Matches(stored, selected));
    }

    [Fact]
    public void PriceCeiling_GrowsPastTheDefaultWhenAProductCostsMore()
    {
        Assert.Equal(10000, ProductColorCatalog.PriceCeiling([3299m, 6499m]));
        Assert.Equal(25000, ProductColorCatalog.PriceCeiling([18450m, 25000m]));
    }

    [Fact]
    public void PriceCeiling_UsesTheFallbackWhenThereAreNoPrices()
    {
        Assert.Equal(10000, ProductColorCatalog.PriceCeiling([]));
    }
}
