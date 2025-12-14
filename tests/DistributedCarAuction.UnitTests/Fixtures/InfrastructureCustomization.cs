namespace DistributedCarAuction.UnitTests.Fixtures;

using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

/// <summary>
/// AutoFixture customization for infrastructure layer tests.
/// Includes AutoMoq for automatic mock generation.
/// </summary>
public class InfrastructureCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Add domain customizations first
        fixture.Customize(new DomainCustomization());
        
        // Add AutoMoq for automatic mock generation
        fixture.Customize(new AutoMoqCustomization { ConfigureMembers = true });
    }
}

/// <summary>
/// AutoData attribute configured with infrastructure customizations.
/// Use this for tests that need mocked dependencies.
/// </summary>
public class AutoMoqDataAttribute : AutoDataAttribute
{
    public AutoMoqDataAttribute()
        : base(() => new Fixture().Customize(new InfrastructureCustomization()))
    {
    }
}

