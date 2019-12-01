using Akka.Actor;
using Common;
using System;

namespace Submitter
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new Host();
            service.Start(new GenericActorSystemHostFactory());

            Console.WriteLine("[Submitter process].");
            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                await service.StopAsync();
            };
            service.TerminationHandle.Wait();
        }
    }
}
