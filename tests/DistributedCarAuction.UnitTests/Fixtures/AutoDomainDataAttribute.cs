namespace DistributedCarAuction.UnitTests.Fixtures;

using AutoFixture;
using AutoFixture.Xunit2;

/// <summary>
/// AutoData attribute configured with domain customizations.
/// Use this for tests that need auto-generated domain entities.
/// </summary>
public class AutoDomainDataAttribute : AutoDataAttribute
{
    public AutoDomainDataAttribute()
        : base(() => new Fixture().Customize(new DomainCustomization()))
    {
    }
}

/// <summary>
/// InlineAutoData attribute configured with domain customizations.
/// Use this for theory tests that combine inline data with auto-generated data.
/// </summary>
public class InlineAutoDomainDataAttribute : InlineAutoDataAttribute
{
    public InlineAutoDomainDataAttribute(params object[] values)
        : base(new AutoDomainDataAttribute(), values)
    {
    }
}

