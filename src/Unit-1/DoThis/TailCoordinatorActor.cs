using System;
using Akka.Actor;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        #region Message types

        public class StartTail
        {
            public string FilePath { get; }
            public IActorRef ReporterActor { get; }

            public StartTail(string filePath, IActorRef reporterActor)
            {
                FilePath = filePath;
                ReporterActor = reporterActor;
            }

            public class StopTail
            {
                public string FilePath { get; }

                public StopTail(string filePath)
                {
                    FilePath = filePath;
                }
            }
        }

        #endregion
        protected override void OnReceive(object message)
        {
            if (message is StartTail msg)
            {
                Context.ActorOf(Props.Create(() => new TailActor(msg.ReporterActor, msg.FilePath)));
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10,
                TimeSpan.FromSeconds(30),
                x =>
                {
                    switch (x)
                    {
                        case ArithmeticException _:
                            return Directive.Resume;
                        case NotSupportedException _:
                            return Directive.Stop;
                        default:
                            return Directive.Restart;
                    }
                }
                );
        }
    }
}