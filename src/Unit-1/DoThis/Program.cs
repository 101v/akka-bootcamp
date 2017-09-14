using System;
﻿using Akka.Actor;

namespace WinTail
{
    #region Program
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            // initialize MyActorSystem
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            var writer = MyActorSystem.ActorOf(Props.Create<ConsoleWriterActor>(), "consoleWriterActor");

            MyActorSystem.ActorOf(Props.Create(() => new TailCoordinatorActor()), "tailCoordinatorActor");

            MyActorSystem.ActorOf(Props.Create(() => new FileValidatorActor(writer)), "validationActor");

            var reader = MyActorSystem.ActorOf(Props.Create<ConsoleReaderActor>(), "ConsoleReaderActor");
            

            // tell console reader to begin
            reader.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }
    #endregion
}
