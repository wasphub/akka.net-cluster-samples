using Akka.Actor;
using Akka.Routing;
using Common.Commands;
using Common.Hosting;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace Pub
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

            var props = Props.Create<Publisher>().WithRouter(FromConfig.Instance);
            var publisher = service.System.ActorOf(props, "watchers");

            publisher.Tell(new Start(settings.Value.Broker, settings.Value.ConnectionString));
            
            Console.WriteLine("[Pub process].");
            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                await service.StopAsync();
            };
            service.TerminationHandle.Wait();
        }
    }

    class Start
    {
        public Start(string broker, string connectionString)
        {
            Broker = broker;
            ConnectionString = connectionString;
        }

        public string Broker { get; }
        public string ConnectionString { get; }
    }

    class Publisher : ReceiveActor
    {
        IProducer<Null, string> _producer;
        string topic = "writer";    //topic name, with Event Hubs this needs to be defined in Azure Portal

        public Publisher()
        {
            string caCertLocation = ".\\cacert.pem";

            Receive<Start>(m =>
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = m.Broker,
                    SecurityProtocol = SecurityProtocol.SaslSsl,
                    SaslMechanism = SaslMechanism.Plain,
                    SaslUsername = "$ConnectionString",
                    SaslPassword = m.ConnectionString,
                    SslCaLocation = caCertLocation,
                };

                _producer = new ProducerBuilder<Null, string>(config).Build();

                Become(Ready);
            });
        }

        void Ready()
        {
            ReceiveAsync<Done>(async m =>
            {
                try
                {
                    var dr = await _producer.ProduceAsync(topic, new Message<Null, string> { Value = m.Value });
                    Console.WriteLine($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                }
            });
        }
    }
}
