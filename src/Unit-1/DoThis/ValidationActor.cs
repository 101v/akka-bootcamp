using Akka.Actor;

namespace WinTail
{
    class ValidationActor : UntypedActor
    {
        private readonly IActorRef _writer;

        public ValidationActor(IActorRef writer)
        {
            _writer = writer;
        }
        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                _writer.Tell(new Messages.NullInputError("No input received."));
            }
            else
            {
                var valid = IsValid(msg);
                if (valid)
                {
                    _writer.Tell(new Messages.InputSuccess("Thank you! Message was valid."));
                }
                else
                {
                    _writer.Tell(new Messages.ValidationError("Input: input had odd number of characters."));
                }
            }

            Sender.Tell(new Messages.ContinueProcessing());
        }

        private bool IsValid(string message)
        {
            return message.Length % 2 == 0;
        }

    }
}