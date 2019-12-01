using Akka.Actor;
using System.Threading.Tasks;

namespace Beacon
{
    class Host
    {
        private readonly string _actorSystemName;

        public ActorSystem System { get; private set; }

        public Host() : this(actorSystemName: null) { }

        public Host(IHostFactory factory)
        {
            Start(factory);
        }

        public Host(string actorSystemName)
        {
            _actorSystemName = actorSystemName;
        }

        public void Start(IHostFactory factory, int port = 0)
        {
            System = factory.Launch(_actorSystemName, port);
        }

        public Task TerminationHandle => System.WhenTerminated;

        public async Task StopAsync()
        {
            var cluster = Akka.Cluster.Cluster.Get(System);
            await cluster.LeaveAsync();

            await CoordinatedShutdown.Get(System).Run(new StopReason());
        }

        class StopReason : CoordinatedShutdown.Reason { }
    }
}
