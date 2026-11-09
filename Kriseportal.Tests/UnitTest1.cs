using System.ComponentModel.DataAnnotations;
using Kriseportal.Web.Models;

namespace Kriseportal.Tests;

public class NeedViewModelTests
{
    private static List<ValidationResult> Validate(NeedViewModel model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);

        Validator.TryValidateObject(
            model,
            context,
            results,
            validateAllProperties: true
        );

        return results;
    }

    private static NeedViewModel CreateValidModel()
    {
        return new NeedViewModel
        {
            Title = "Behov for vann",
            CategoryId = 1,
            Quantity = 10,
            Unit = "Liter",
            NeededFrom = DateTime.Today,
            Municipality = "Kristiansand",
            ContactName = "Testperson",
            ContactPhone = "12345678",
            Description = "Vi trenger drikkevann.",
            Priority = "High",
            Latitude = 58.1467,
            Longitude = 7.9956
        };
    }

    [Fact]
    public void MissingTitle_ShouldFailValidation()
    {
        var model = CreateValidModel();
        model.Title = "";

        var results = Validate(model);

        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(model.Title)));
    }

    [Fact]
    public void TitleTooLong_ShouldFailValidation()
    {
        var model = CreateValidModel();
        model.Title = new string('A', 151);

        var results = Validate(model);

        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(model.Title)));
    }

    [Fact]
    public void QuantityBelowOne_ShouldFailValidation()
    {
        var model = CreateValidModel();
        model.Quantity = 0;

        var results = Validate(model);

        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(model.Quantity)));
    }

    [Fact]
    public void MissingCategory_ShouldFailValidation()
    {
        var model = CreateValidModel();
        model.CategoryId = 0;

        var results = Validate(model);

        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(model.CategoryId)));
    }

    [Fact]
    public void InvalidLatitude_ShouldFailValidation()
    {
        var model = CreateValidModel();
        model.Latitude = 100;

        var results = Validate(model);

        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(model.Latitude)));
    }

    [Fact]
    public void InvalidLongitude_ShouldFailValidation()
    {
        var model = CreateValidModel();
        model.Longitude = 200;

        var results = Validate(model);

        Assert.Contains(results, r =>
            r.MemberNames.Contains(nameof(model.Longitude)));
    }

    [Fact]
    public void ValidModel_ShouldPassValidation()
    {
        var model = CreateValidModel();

        var results = Validate(model);

        Assert.Empty(results);
    }
}