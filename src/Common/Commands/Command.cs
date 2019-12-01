using Akka.Actor;

namespace Common.Commands
{
    public class Command
    {
        public string Input { get; }
        public IActorRef Sender { get; }

        public Command(string input, IActorRef sender)
        {
            Input = input;
            Sender = sender;
        }
    }
}
