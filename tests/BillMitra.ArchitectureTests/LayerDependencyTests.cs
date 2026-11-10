using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace BillMitra.ArchitectureTests;

public class LayerDependencyTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(BillMitra.Domain.Enums.DocumentType).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(BillMitra.Application.DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(BillMitra.Infrastructure.DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly ApiAssembly = typeof(BillMitra.Api.Controllers.HealthController).Assembly;

    [Fact]
    public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("BillMitra.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Domain must not depend on Application layer.");
    }

    [Fact]
    public void DomainLayer_ShouldNotDependOn_InfrastructureLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("BillMitra.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Domain must not depend on Infrastructure layer.");
    }

    [Fact]
    public void DomainLayer_ShouldNotDependOn_ApiLayer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("BillMitra.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Domain must not depend on API layer.");
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_InfrastructureLayer()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("BillMitra.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Application must not depend on Infrastructure layer.");
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOn_ApiLayer()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("BillMitra.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Application must not depend on API layer.");
    }

    [Fact]
    public void Controllers_ShouldNotHaveDirectDatabaseConnections()
    {
        var npgsqlResult = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .ShouldNot()
            .HaveDependencyOn("Npgsql")
            .GetResult();

        var sqlClientResult = Types.InAssembly(ApiAssembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Data.SqlClient")
            .GetResult();

        npgsqlResult.IsSuccessful.Should().BeTrue("Controllers must not directly depend on Npgsql.");
        sqlClientResult.IsSuccessful.Should().BeTrue("Controllers must not directly depend on SqlClient.");
    }
}
