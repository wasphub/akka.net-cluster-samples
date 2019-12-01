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

            // SETUP 1: All local
            var worker = service.System.ActorOf(Props.Create(() => new Worker()));
            var watcher = service.System.ActorOf(Props.Create(() => new Watcher()));

            // Simple actor dealing with user input and submission to workers
            var input = service.System.ActorOf(Props.Create(() => new Input(worker, watcher)));

            input.Tell(Read.Instance);

            Console.WriteLine("[Submitter process].");
            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                await service.StopAsync();
            };
            service.TerminationHandle.Wait();
        }
    }

    class Input : ReceiveActor
    {
        public Input(IActorRef worker, IActorRef watcher)
        {
            Receive<Read>(m =>
            {
                Console.WriteLine("Enter a string to process:");
                var input = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(input))
                    worker.Tell(new Command(input, watcher));

                Self.Tell(Read.Instance);
            });
        }
    }

    class Read
    {
        Read() { }

        public static Read Instance { get; } = new Read();
    }
}
