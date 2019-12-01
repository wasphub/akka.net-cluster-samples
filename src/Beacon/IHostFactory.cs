using Akka.Actor;

namespace Beacon
{
    public interface IHostFactory
    {
        ActorSystem Launch(string systemName = null, int port = 0);
    }
}
