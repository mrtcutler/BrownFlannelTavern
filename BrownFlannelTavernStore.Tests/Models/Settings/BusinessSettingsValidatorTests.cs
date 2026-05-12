using BrownFlannelTavernStore.Models.Settings;
using BrownFlannelTavernStore.Tests.TestHelpers;
using FluentAssertions;

namespace BrownFlannelTavernStore.Tests.Models.Settings;

public class BusinessSettingsValidatorTests
{
    private static readonly BusinessSettingsValidator Validator = new();

    [Fact]
    public void Validate_DefaultValid_Succeeds()
    {
        var result = Validator.Validate(null, TestBusiness.Default());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingName_Fails()
    {
        var settings = TestBusiness.Default();
        settings.Name = "";

        var result = Validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(nameof(BusinessSettings.Name)));
    }

    [Fact]
    public void Validate_MissingTagline_Fails()
    {
        var settings = TestBusiness.Default();
        settings.Tagline = "";

        var result = Validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(nameof(BusinessSettings.Tagline)));
    }

    [Fact]
    public void Validate_PickupEnabled_MissingLocationName_Fails()
    {
        var settings = TestBusiness.Default();
        settings.Pickup.LocationName = null;

        var result = Validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Pickup:LocationName"));
    }

    [Fact]
    public void Validate_PickupDisabled_DoesNotRequirePickupFields()
    {
        var settings = TestBusiness.Default();
        settings.Pickup.Enabled = false;
        settings.Pickup.LocationName = null;
        settings.Pickup.AddressLine1 = null;
        settings.Pickup.City = null;
        settings.Pickup.State = null;
        settings.Pickup.PostalCode = null;
        settings.Pickup.Hours = null;

        var result = Validator.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_MultipleErrors_AllReported()
    {
        var settings = TestBusiness.Default();
        settings.Name = "";
        settings.Tagline = "";
        settings.Pickup.City = null;

        var result = Validator.Validate(null, settings);

        result.Failed.Should().BeTrue();
        result.Failures.Should().NotBeNull().And.HaveCountGreaterThan(1);
    }

    [Fact]
    public void FormattedAddress_ProducesCleanString()
    {
        var settings = TestBusiness.Default();

        var formatted = settings.Pickup.FormattedAddress();

        formatted.Should().Be("175 S Venoy Rd, Westland, MI 48186");
    }

    [Fact]
    public void FullStoreName_CombinesNameAndSuffix()
    {
        var settings = TestBusiness.Default();

        settings.FullStoreName.Should().Be("Brown Flannel Tavern Store");
    }

    [Fact]
    public void FullStoreName_EmptySuffix_ReturnsNameOnly()
    {
        var settings = TestBusiness.Default();
        settings.StoreNameSuffix = "";

        settings.FullStoreName.Should().Be("Brown Flannel Tavern");
    }
}
