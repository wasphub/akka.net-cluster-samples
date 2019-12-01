using Akka.Actor;
using Akka.Persistence;
using Akka.Routing;
using Common.Commands;
using Common.Hosting;
using System;
using System.Collections.Generic;

namespace Writer
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new Host();
            service.Start(new GenericActorSystemHostFactory());

            var props = Props.Create<Writer>().WithRouter(FromConfig.Instance);
            service.System.ActorOf(props, "watchers");

            Console.WriteLine("[Writer process].");
            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                await service.StopAsync();
            };
            service.TerminationHandle.Wait();
        }
    }

    class Writer : ReceivePersistentActor
    {
        readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public Writer()
        {
            Command<Done>(c => Persist(c, m =>
            {
                _counts[m.Value] = _counts.TryGetValue(m.Value, out var x) ? x + 1 : 1;

                Dump();
            }));

            Recover<Done>(m =>
            {
                _counts[m.Value] = _counts.TryGetValue(m.Value, out var x) ? x + 1 : 1;
            });
        }

        protected override void OnReplaySuccess()
        {
            Dump();
        }

        private void Dump()
        {
            Console.WriteLine("-----");
            foreach (var count in _counts)
            {
                Console.WriteLine($"{count.Key} = {count.Value}");
            }
            Console.WriteLine("-----");
        }

        public override string PersistenceId => "writer";
    }
}
