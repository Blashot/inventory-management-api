using NetArchTest.Rules;
using Shouldly;

namespace ArchitectureTests.Layers;

public class LayerTests : BaseTest
{
    [Fact]
    public void Domain_Should_NotHaveDependencyOnApplication()
    {
        TestResult result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn("Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void DomainLayer_ShouldNotHaveDependencyOn_InfrastructureLayer()
    {
        TestResult result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void DomainLayer_ShouldNotHaveDependencyOn_PresentationLayer()
    {
        TestResult result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(PresentationAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void ApplicationLayer_ShouldNotHaveDependencyOn_InfrastructureLayer()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void ApplicationLayer_ShouldNotHaveDependencyOn_PresentationLayer()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(PresentationAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void InfrastructureLayer_ShouldNotHaveDependencyOn_PresentationLayer()
    {
        TestResult result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn(PresentationAssembly.GetName().Name)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    // --- Positive dependency assertions ---

    [Fact]
    public void InfrastructureLayer_Should_DependOn_ApplicationLayer()
    {
        // Infrastructure implements Application contracts (IApplicationDbContext, IHolidayCalendar, etc.)
        // Verifies the intended upward dependency is in place.
        bool dependsOnApplication = InfrastructureAssembly
            .GetReferencedAssemblies()
            .Any(a => a.Name == ApplicationAssembly.GetName().Name);

        dependsOnApplication.ShouldBeTrue(
            "Infrastructure must reference Application to implement its contracts.");
    }

    [Fact]
    public void InfrastructureLayer_Should_DependOn_DomainLayer()
    {
        bool dependsOnDomain = InfrastructureAssembly
            .GetReferencedAssemblies()
            .Any(a => a.Name == DomainAssembly.GetName().Name);

        dependsOnDomain.ShouldBeTrue(
            "Infrastructure must reference Domain to work with domain entities.");
    }
}
