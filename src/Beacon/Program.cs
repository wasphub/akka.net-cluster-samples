using System;

namespace Beacon
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length != 2)
                throw new ArgumentException("You need to specify the actor system name and port number the Beacon will listen too");

            var service = new Host(args[0]);
            service.Start(new BeaconHostFactory(), int.Parse(args[1]));

            Console.WriteLine("[Beacon process].");
            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                await service.StopAsync();
            };
            service.TerminationHandle.Wait();
        }
    }
}
