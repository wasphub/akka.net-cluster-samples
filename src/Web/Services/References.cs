using Akka.Actor;
using Akka.Routing;
using Common.Commands;
using Common.Hosting;
using Microsoft.AspNetCore.SignalR;

namespace Web.Services
{
    public class References
    {
        public IActorRef Worker { get; }
        public IActorRef Watcher { get; }

        public References(Host host, IHubContext<ResultsHub> context)
        {
            Worker = host.System.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "worker-router");

            host.System.ActorOf(Props.Create<WebWatcher>().WithRouter(FromConfig.Instance), "watchers");

            Watcher = host.System.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "watcher-router");

            Watcher.Tell(context);
        }

        class WebWatcher : ReceiveActor
        {
            private IHubContext<ResultsHub> _context;

            public WebWatcher()
            {
                Receive<IHubContext<ResultsHub>>(m => { _context = m; });

                Receive<Done>(m =>
                {
                    _context.Clients.All.SendAsync("Result", $"Worker '{m.Name}' has processed '{m.Value}'");
                });
            }
        }
    }
}
