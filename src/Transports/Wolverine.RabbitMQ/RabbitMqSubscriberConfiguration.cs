using System;
using Wolverine.Configuration;
using Wolverine.RabbitMQ.Internal;
using Wolverine.Runtime.Interop.MassTransit;

namespace Wolverine.RabbitMQ
{
    public class
        RabbitMqSubscriberConfiguration : SubscriberConfiguration<RabbitMqSubscriberConfiguration, RabbitMqEndpoint>
    {
        public RabbitMqSubscriberConfiguration(RabbitMqEndpoint endpoint) : base(endpoint)
        {
        }

        /// <summary>
        ///     Configure raw properties of this RabbitMqEndpoint. Advanced usages
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        public RabbitMqSubscriberConfiguration Advanced(Action<RabbitMqEndpoint> configure)
        {
            configure(_endpoint);
            return this;
        }

        /// <summary>
        /// Configure this Rabbit MQ endpoint for interoperability with MassTransit
        /// </summary>
        /// <param name="configure">Optionally configure the JSON serialization for MassTransit</param>
        /// <returns></returns>
        public RabbitMqSubscriberConfiguration UseMassTransitInterop(Action<IMassTransitInterop>? configure = null)
        {
            _endpoint.UseMassTransitInterop(configure);
            return this;
        }
    }
}
