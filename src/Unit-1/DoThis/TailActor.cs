using System.IO;
using System.Text;
using Akka.Actor;

namespace WinTail
{
    public class TailActor : UntypedActor
    {
        private readonly IActorRef _reporterActor;
        private readonly StreamReader _fileStreamReader;

        #region Message types

        public class FileWrite
        {
            public string FileName { get; }

            public FileWrite(string fileName)
            {
                FileName = fileName;
            }
        }

        public class FileError
        {
            public string FileName { get; }
            public string Reason { get; }

            public FileError(string fileName, string reason)
            {
                FileName = fileName;
                Reason = reason;
            }
        }

        public class InitialRead
        {
            public string FileName { get; }
            public string Text { get; }

            public InitialRead(string fileName, string text)
            {
                FileName = fileName;
                Text = text;
            }
        }

        #endregion

        public TailActor(IActorRef reporterActor, string filePath)
        {
            _reporterActor = reporterActor;

            var fileObserver = new FileObserver(Self, Path.GetFullPath(filePath));
            fileObserver.Start();

            var fileStream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _fileStreamReader =  new StreamReader(fileStream, Encoding.UTF8);

            var text = _fileStreamReader.ReadToEnd();
            Self.Tell(new InitialRead(filePath, text));
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case FileWrite _:
                    var text = _fileStreamReader.ReadToEnd();
                    _reporterActor.Tell(text);
                    break;
                case FileError error:
                    _reporterActor.Tell($"Tail error: {error.Reason}");
                    break;
                case InitialRead read:
                    _reporterActor.Tell(read.Text);
                    break;
            }
        }
    }
}