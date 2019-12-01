using Akka.Actor;
using Common.Commands;
using System;

namespace Common.Actors
{
    public class Watcher : ReceiveActor
    {
        public Watcher()
        {
            Receive<Done>(m =>
            {
                Console.WriteLine($"Worker '{m.Name}' has processed '{m.Value}'");
            });
        }
    }
}
