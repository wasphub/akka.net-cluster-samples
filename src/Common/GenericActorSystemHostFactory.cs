using Akka.Actor;
using Akka.Configuration;
using System.IO;

namespace Common
{
    public class GenericActorSystemHostFactory : IHostFactory
    {
        public ActorSystem Launch(string systemName = null, int port = 0)
        {
            var clusterConfig = ConfigurationFactory.ParseString(File.ReadAllText("akka.hocon"));

            var beaconConfig = clusterConfig.GetConfig("startup");
            systemName = beaconConfig?.GetString("actorsystem", systemName) ?? systemName ?? "cluster";

            return ActorSystem.Create(systemName, clusterConfig);
        }
    }
}
