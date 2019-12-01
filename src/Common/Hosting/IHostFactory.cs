using Akka.Actor;

namespace Common.Hosting
{
    public interface IHostFactory
    {
        ActorSystem Launch(string systemName = null, int port = 0);
    }
}
