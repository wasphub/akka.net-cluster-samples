using Akka.Actor;
using System;

namespace Common
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
