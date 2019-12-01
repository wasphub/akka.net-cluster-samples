﻿using Akka.Actor;
using System;

namespace Common
{
    public class Worker : ReceiveActor
    {
        public Worker()
        {
            string name = Guid.NewGuid().ToString();

            ReceiveAsync<Command>(async m =>
            {
                Console.WriteLine($"[{name}]: Processing of '{m.Input}' starting...");

                var processed = await Functions.Reverse(m.Input);

                Console.WriteLine($"[{name}]: '{m.Input}' > '{processed}'");

                m.Sender.Tell(new Done(name, processed));
            });
        }
    }
}
