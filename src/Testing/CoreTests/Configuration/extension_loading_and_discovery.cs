using System.Linq;
using Lamar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using TestingSupport;
using Wolverine;
using Wolverine.Runtime;
using Xunit;

namespace CoreTests.Configuration;

public class extension_loading_and_discovery : IntegrationContext
{
    public extension_loading_and_discovery(DefaultApp @default) : base(@default)
    {
    }

    [Fact]
    public void the_application_still_wins()
    {
        var options = new WolverineOptions();
        options.Handlers.DisableConventionalDiscovery();
        options.Include<OptionalExtension>();
        options.Services.For<IColorService>().Use<BlueService>();

        using var runtime = WolverineHost.For(options);
        runtime.Get<IColorService>()
            .ShouldBeOfType<BlueService>();
    }

    [Fact]
    public void try_find_extension_miss()
    {
        var options = new WolverineOptions();
        options.Handlers.DisableConventionalDiscovery();
        //options.Include<OptionalExtension>();

        using var host = WolverineHost.For(options);
        var runtime = host.Services.GetRequiredService<IWolverineRuntime>();
        runtime.TryFindExtension<OptionalExtension>().ShouldBeNull();
    }

    [Fact]
    public void try_find_extension_hit()
    {
        var options = new WolverineOptions();
        options.Handlers.DisableConventionalDiscovery();
        options.Include<OptionalExtension>();

        using var host = WolverineHost.For(options);
        var runtime = host.Services.GetRequiredService<IWolverineRuntime>();
        runtime.TryFindExtension<OptionalExtension>().ShouldBeOfType<OptionalExtension>().ShouldNotBeNull();
    }

    [Fact]
    public void try_find_extension_hit_2()
    {
        var options = new WolverineOptions();
        options.Handlers.DisableConventionalDiscovery();
        options.Services.AddSingleton<IWolverineExtension>(new OptionalExtension());

        using var host = new HostBuilder()
            .UseWolverine(opts => { opts.Handlers.DisableConventionalDiscovery(); })
            .ConfigureServices(services => services.AddSingleton<IWolverineExtension, OptionalExtension>())
            .Start();

        var runtime = host.Services.GetRequiredService<IWolverineRuntime>();
        runtime.TryFindExtension<OptionalExtension>().ShouldBeOfType<OptionalExtension>().ShouldNotBeNull();
    }

    [Fact]
    public void will_apply_an_extension()
    {
        #region sample_explicitly_add_extension

        var registry = new WolverineOptions();
        registry.Include<OptionalExtension>();

        #endregion

        registry.Handlers.DisableConventionalDiscovery();

        using (var runtime = WolverineHost.For(registry))
        {
            runtime.Get<IColorService>()
                .ShouldBeOfType<RedService>();
        }
    }

    [Fact]
    public void will_only_apply_extension_once()
    {
        var registry = new WolverineOptions();
        registry.Include<OptionalExtension>();
        registry.Include<OptionalExtension>();
        registry.Include<OptionalExtension>();
        registry.Include<OptionalExtension>();

        using (var host = WolverineHost.For(registry))
        {
            host.Get<IContainer>().Model.For<IColorService>().Instances
                .Count().ShouldBe(1);
        }
    }

    [Fact]
    public void picks_up_on_handlers_from_extension()
    {
        with(x => x.Include<MyExtension>());

        var handlerChain = chainFor<ExtensionMessage>();
        handlerChain.Handlers.Single()
            .HandlerType.ShouldBe(typeof(ExtensionThing));
    }
}

public interface IColorService
{
}

public class RedService : IColorService
{
}

public class BlueService : IColorService
{
}

public class OptionalExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Services.For<IColorService>().Use<RedService>();
    }
}

public class MyExtension : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Handlers.IncludeType<ExtensionThing>();
    }
}

public class ExtensionMessage
{
}

public class ExtensionThing
{
    public void Handle(ExtensionMessage message)
    {
    }
}
