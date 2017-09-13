using System;
using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class FileObserver : IDisposable
    {
        private readonly IActorRef _tailActor;
        private readonly string _absoluteFileName;
        private readonly string _fileDirectory;
        private readonly string _fileName;
        private FileSystemWatcher _watcher;

        public FileObserver(IActorRef tailActor, string absoluteFileName)
        {
            _tailActor = tailActor;
            _absoluteFileName = absoluteFileName;
            _fileDirectory = Path.GetDirectoryName(absoluteFileName);
            _fileName = Path.GetFileName(absoluteFileName);
        }

        public void Start()
        {
            _watcher = new FileSystemWatcher(_fileDirectory, _fileName)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Error += OnFileError;

            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileError(object sender, ErrorEventArgs e)
        {
            _tailActor.Tell(new TailActor.FileError(_fileName, e.GetException().Message), ActorRefs.NoSender);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            _tailActor.Tell(new TailActor.FileWrite(e.Name), ActorRefs.NoSender);
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}