using System;
using System.IO;
using ForensicAuditor.Core.Interfaces;
using ForensicAuditor.Core.Models;

namespace ForensicAuditor.Engine.Monitors
{
    public class FileSystemIntegrityMonitor : IFileMonitor
    {
        private FileSystemWatcher? _fsw;
        private readonly string _targetPath;

        public event Action<FileEvent>? OnFileChanged;

        public FileSystemIntegrityMonitor(string targetPath)
        {
            _targetPath = targetPath;
        }

        public void StartMonitoring()
        {
            if (!Directory.Exists(_targetPath)) return;

            _fsw = new FileSystemWatcher(_targetPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _fsw.Changed += (s, e) => HandleFileSystemEvent(e, "Modified");
            _fsw.Created += (s, e) => HandleFileSystemEvent(e, "Created");
            _fsw.Deleted += (s, e) => HandleFileSystemEvent(e, "Deleted");
            _fsw.Renamed += (s, e) => OnFileChanged?.Invoke(new FileEvent
            {
                FilePath = e.FullPath,
                FileName = e.Name ?? string.Empty,
                OldFilePath = e.OldFullPath,
                Action = "Renamed"
            });
        }

        private void HandleFileSystemEvent(FileSystemEventArgs e, string action)
        {
            OnFileChanged?.Invoke(new FileEvent
            {
                FilePath = e.FullPath,
                FileName = e.Name ?? string.Empty,
                Action = action
            });
        }

        public void StopMonitoring()
        {
            if (_fsw != null)
            {
                _fsw.EnableRaisingEvents = false;
                _fsw.Dispose();
                _fsw = null;
            }
        }
    }
}
