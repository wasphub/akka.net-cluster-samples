using Akka.Actor;
using Akka.Configuration;
using Common;
using System;
using System.IO;
using System.Linq;

namespace Beacon
{
    class BeaconHostFactory : IHostFactory
    {
        public ActorSystem Launch(string systemName = null, int port = 0)
        {
            var clusterConfig = ConfigurationFactory.ParseString(File.ReadAllText("akka.hocon"));

            var beaconConfig = clusterConfig.GetConfig("startup");
            systemName = systemName ?? beaconConfig?.GetString("actorsystem") ?? "cluster";

            var remoteConfig = clusterConfig.GetConfig("akka.remote");
            port = port == 0 ? remoteConfig.GetInt("dot-netty.tcp.port") : port;
            if (port == 0) throw new ConfigurationException("Need to specify an explicit port for Beacon. Found an undefined port or a port value of 0 in App.config.");

            var ipAddress = remoteConfig.GetString("dot-netty.tcp.public-hostname") ?? "127.0.0.1";

            var selfAddress = new Address("akka.tcp", systemName, ipAddress.Trim(), port).ToString();

            Console.WriteLine($"[Beacon] ActorSystem: {systemName}; IP: {ipAddress}; PORT: {port}");
            Console.WriteLine($"[Beacon] Performing pre-boot sanity check. Should be able to parse address [{selfAddress}]");

            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes");
            if (!seeds.Contains(selfAddress))
            {
                seeds.Add(selfAddress);
            }

            var quoted = string.Join(",", seeds.Select(s => $@"""{s}"""));
            var seedsConfig = $"akka.cluster.seed-nodes = [{quoted}]";

            var finalConfig = ConfigurationFactory.ParseString($@"
akka.remote.dot-netty.tcp.public-hostname = {ipAddress} 
akka.remote.dot-netty.tcp.port = {port}")
                .WithFallback(ConfigurationFactory.ParseString(seedsConfig))
                .WithFallback(clusterConfig);

            var actorSystem = ActorSystem.Create(systemName, finalConfig);

            return actorSystem;
        }
    }
}
