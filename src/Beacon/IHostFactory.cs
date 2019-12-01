using Akka.Actor;

namespace Beacon
{
    interface IHostFactory
    {
        ActorSystem Launch(string systemName = null, int port = 0);
    }
}
