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

            Props writerProps = Props.Create<ConsoleWriterActor>();
            var writer = MyActorSystem.ActorOf(writerProps, "consoleWriterActor");

            Props validationActorProps = Props.Create(() => new ValidationActor(writer));
            var validation = MyActorSystem.ActorOf(validationActorProps, "validationActor");

            Props readerProps = Props.Create<ConsoleReaderActor>(validation);
            var reader = MyActorSystem.ActorOf(readerProps, "ConsoleReaderActor");
            

            // tell console reader to begin
            reader.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }
    #endregion
}
