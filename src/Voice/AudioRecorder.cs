using System;
using NAudio.Wave;
using System.Threading.Tasks;

namespace Voice
{
    public class AudioRecorder
    {
        private WaveInEvent? waveIn;
        private WaveFileWriter? writer;
        private readonly string outputFilePath;
        private TaskCompletionSource<bool>? _stoppedTcs;
        public Task RecordingStoppedTask => _stoppedTcs?.Task ?? Task.CompletedTask;

        public AudioRecorder(string outputFile = "temp.wav")
        {
            outputFilePath = outputFile;
        }

        public void StartRecording()
        {
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1) // 16kHz mono
            };
            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;
            writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat);
            _stoppedTcs = new TaskCompletionSource<bool>();
            waveIn.StartRecording();
        }

        public void StopRecording()
        {
            waveIn?.StopRecording();
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            writer?.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            writer?.Dispose();
            writer = null;
            waveIn?.Dispose();
            waveIn = null;
            _stoppedTcs?.TrySetResult(true);
        }
    }
} 