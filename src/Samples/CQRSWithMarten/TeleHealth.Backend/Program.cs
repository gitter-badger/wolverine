using Marten;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Oakton;
using TeleHealth.Common;

var builder = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddMarten(opts =>
            {
                opts.Connection(ConnectionSource.ConnectionString);

                opts.Projections.Add<AppointmentProjection>(ProjectionLifecycle.Inline);
                opts.Projections.SelfAggregate<ProviderShift>(ProjectionLifecycle.Inline);

                opts.Projections.Add<BoardViewProjection>(ProjectionLifecycle.Async);
            })
            .AddAsyncDaemon(DaemonMode.HotCold);
    });

return await builder.RunOaktonCommands(args);
