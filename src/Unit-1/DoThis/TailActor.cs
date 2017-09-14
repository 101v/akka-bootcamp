using System.IO;
using System.Text;
using Akka.Actor;

namespace WinTail
{
    public class TailActor : UntypedActor
    {
        private readonly IActorRef _reporterActor;
        private FileObserver _fileObserver;
        private StreamReader _fileStreamReader;
        private readonly string _filePath;

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
            _filePath = filePath;
        }

        protected override void PreStart()
        {
            _fileObserver = new FileObserver(Self, Path.GetFullPath(_filePath));
            _fileObserver.Start();

            var fileStream = new FileStream(Path.GetFullPath(_filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _fileStreamReader = new StreamReader(fileStream, Encoding.UTF8);

            var text = _fileStreamReader.ReadToEnd();
            Self.Tell(new InitialRead(_filePath, text));

        }

        protected override void PostStop()
        {
            _fileObserver.Dispose();
            _fileObserver = null;
            _fileStreamReader.Close();
            _fileStreamReader.Dispose();
            base.PostStop();
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