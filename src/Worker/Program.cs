using Akka.Actor;
using Akka.Routing;
using Common.Hosting;
using System;

namespace Worker
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new Host();
            service.Start(new GenericActorSystemHostFactory());

            var props = Props.Create<Common.Actors.Worker>().WithRouter(FromConfig.Instance);
            service.System.ActorOf(props, "workers");

            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                await service.StopAsync();
            };
            service.TerminationHandle.Wait();
        }
    }
}
