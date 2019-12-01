using Akka.Actor;

namespace Common
{
    public interface IHostFactory
    {
        ActorSystem Launch(string systemName = null, int port = 0);
    }
}
