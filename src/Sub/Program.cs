using Akka.Actor;
using Akka.Routing;
using Common.Commands;
using Common.Hosting;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Sub
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();

            builder
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets<Settings>();

            Configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();

            services
                .Configure<Settings>(Configuration.GetSection(nameof(Settings)))
                .AddOptions();

            var serviceProvider = services.BuildServiceProvider();

            var settings = serviceProvider.GetService<IOptions<Settings>>();
            var service = new Host();
            service.Start(new GenericActorSystemHostFactory());

            var props = Props.Create<Subscriber>().WithRouter(FromConfig.Instance);
            var subscriber = service.System.ActorOf(props, "watchers");

            subscriber.Tell(new Start(settings.Value.Broker, settings.Value.ConnectionString));

            Console.WriteLine("[Sub process].");
            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) => { await service.StopAsync(); };
            service.TerminationHandle.Wait();
        }
    }

    class Subscriber : ReceiveActor
    {
        IConsumer<Ignore, string> _consumer;

        public Subscriber()
        {
            string topic = "writer";
            string caCertLocation = ".\\cacert.pem";
            string consumerGroup = "$Default";

            Receive<Start>(m =>
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = m.Broker,
                    SecurityProtocol = SecurityProtocol.SaslSsl,
                    SocketTimeoutMs = 60000,                //this corresponds to the Consumer config `request.timeout.ms`
                    SessionTimeoutMs = 30000,
                    SaslMechanism = SaslMechanism.Plain,
                    SaslUsername = "$ConnectionString",
                    SaslPassword = m.ConnectionString,
                    SslCaLocation = caCertLocation,
                    GroupId = consumerGroup,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    BrokerVersionFallback = "1.0.0",        //Event Hubs for Kafka Ecosystems supports Kafka v1.0+, a fallback to an older API will fail
                                                            //Debug = "security,broker,protocol"    //Uncomment for librdkafka debugging information
                };

                _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
                _consumer.Subscribe(topic);

                Self.Tell(new Pull());

                Become(Ready);
            });

            void Ready()
            {
                Receive<Pull>(_ =>
                {
                    var cr = _consumer.Consume();

                    if (cr != null)
                        Console.WriteLine($"I got '{cr.Value}' from kafka!");

                    Self.Tell(new Pull());
                });
            }
        }

        class Pull { }
    }
}
