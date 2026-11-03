using System.ComponentModel.DataAnnotations;
using LadiesDressStore.Web.Controllers;
using LadiesDressStore.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NUnit.Framework;

namespace UbhayaFashions.Frontend.Tests;

[TestFixture]
public class FrontendProductModelTests
{
    [Test]
    public void ProductViewModel_DerivesLowestPriceAndDiscount()
    {
        var model = new ProductViewModel
        {
            Variants =
            [
                new ProductVariantViewModel { Size = "M", Price = 1499m, Discount = 10m, Stock = 2 },
                new ProductVariantViewModel { Size = "L", Price = 1299m, Discount = 15m, Stock = 3 }
            ]
        };

        Assert.Multiple(() =>
        {
            Assert.That(model.Price, Is.EqualTo(1299m));
            Assert.That(model.Discount, Is.EqualTo(15m));
            Assert.That(model.Stock, Is.EqualTo(5));
            Assert.That(model.Sizes, Is.EqualTo("M,L"));
        });
    }

    [Test]
    public void ProductViewModel_EmptyVariants_ReturnsSafeDefaults()
    {
        var model = new ProductViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(model.Price, Is.Zero);
            Assert.That(model.Discount, Is.Zero);
            Assert.That(model.Stock, Is.Zero);
            Assert.That(model.Sizes, Is.Empty);
        });
    }
}

[TestFixture]
public class FrontendColorCatalogTests
{
    [Test]
    public void Format_OrdersKnownColorsAndDeduplicates()
    {
        var result = ProductColorCatalog.Format(["pink", "Pink", "navy blue", "black"]);

        Assert.That(result, Is.EqualTo("Black,Pink,Navy Blue"));
    }

    [TestCase("Red,Maroon", "red", true)]
    [TestCase("Maroon", "Red", false)]
    [TestCase("Blue|Gold/White", "gold", true)]
    [TestCase(null, "Pink", false)]
    public void Parse_RecognizesOnlyWholeColorTokens(string? input, string selected, bool expected)
    {
        Assert.That(ProductColorCatalog.Parse(input).Contains(selected), Is.EqualTo(expected));
    }

    [Test]
    public void Format_NullCollection_ReturnsEmptyString()
    {
        Assert.That(ProductColorCatalog.Format(null), Is.Empty);
    }
}

[TestFixture]
public class ContactViewModelValidationTests
{
    [Test]
    public void ValidContactDetails_PassValidation()
    {
        var model = new ContactViewModel
        {
            Name = "Test Customer",
            Email = "customer@example.com",
            Subject = "Order enquiry",
            Message = "Need help with an order."
        };

        var errors = Validate(model);

        Assert.That(errors, Is.Empty);
    }

    [TestCase("", "customer@example.com", "Subject", "Message")]
    [TestCase("Name", "not-an-email", "Subject", "Message")]
    [TestCase("Name", "", "Subject", "Message")]
    [TestCase("Name", "customer@example.com", "", "Message")]
    [TestCase("Name", "customer@example.com", "Subject", "")]
    public void MissingOrInvalidContactFields_FailValidation(
        string name, string email, string subject, string message)
    {
        var model = new ContactViewModel { Name = name, Email = email, Subject = subject, Message = message };

        Assert.That(Validate(model), Is.Not.Empty);
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
