using NAudio.Wave;
using NAudio.Dsp;
using System;

namespace Muse.Utils;

public sealed class SpectrumAnalyzer : IDisposable
{
    private WasapiLoopbackCapture? capture;
    private readonly int fftLength = 1024; // Must be a power of 2
    private readonly SampleAggregator sampleAggregator;
    
    public float[] SpectrumData { get; private set; } = new float[10];

    public SpectrumAnalyzer()
    {
        sampleAggregator = new SampleAggregator(fftLength);
        sampleAggregator.FftCalculated += OnFftCalculated;

        try
        {
            capture = new WasapiLoopbackCapture();
            capture.DataAvailable += OnDataAvailable;
            capture.StartRecording();
        }
        catch (Exception ex)
        {
            // Fallback or log error. On some systems loopback might not be available.
            Console.Error.WriteLine($"SpectrumAnalyzer failed: {ex.Message}");
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (capture == null) return;

        int bytesPerSample = capture.WaveFormat.BitsPerSample / 8;
        int samplesRecorded = e.BytesRecorded / bytesPerSample;
        int channels = capture.WaveFormat.Channels;

        for (int i = 0; i < samplesRecorded; i += channels)
        {
            float sample = 0;
            if (capture.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                sample = BitConverter.ToSingle(e.Buffer, i * bytesPerSample);
            }
            else if (capture.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                if (capture.WaveFormat.BitsPerSample == 16)
                    sample = BitConverter.ToInt16(e.Buffer, i * bytesPerSample) / 32768f;
                else if (capture.WaveFormat.BitsPerSample == 24)
                    sample = ((((e.Buffer[i * bytesPerSample + 2] << 16) | (e.Buffer[i * bytesPerSample + 1] << 8) | e.Buffer[i * bytesPerSample]) << 8) >> 8) / 8388608f;
            }

            sampleAggregator.Add(sample);
        }
    }

    private void OnFftCalculated(object? sender, FftEventArgs e)
    {
        int bins = 10;
        float[] newSpectrum = new float[bins];
        int usableBins = fftLength / 2;

        for (int i = 0; i < bins; i++)
        {
            // Use a bit more linear mapping for low frequency bins to ensure they show up
            int startBin = (int)(usableBins * Math.Pow((double)i / bins, 2));
            int endBin = (int)(usableBins * Math.Pow((double)(i + 1) / bins, 2));
            if (endBin <= startBin) endBin = startBin + 1;

            float max = 0;
            for (int j = startBin; j < endBin && j < e.Result.Length; j++)
            {
                float magnitude = (float)Math.Sqrt(e.Result[j].X * e.Result[j].X + e.Result[j].Y * e.Result[j].Y);
                if (magnitude > max) max = magnitude;
            }

            // High sensitivity boost + Logarithmic scaling
            // Even small sounds should show up now
            float val = (float)Math.Clamp(Math.Log10(max * 100 + 1) * 100, 0, 100);
            newSpectrum[i] = val;
        }

        SpectrumData = newSpectrum;
    }

    public void Dispose()
    {
        var localCapture = capture;
        capture = null;

        if (localCapture == null)
        {
            return;
        }

        localCapture.DataAvailable -= OnDataAvailable;

        try
        {
            localCapture.StopRecording();
        }
        finally
        {
            localCapture.Dispose();
        }
    }
}

public class SampleAggregator
{
    public event EventHandler<FftEventArgs>? FftCalculated;
    private readonly Complex[] fftBuffer;
    private readonly int fftLength;
    private int fftPos;

    public SampleAggregator(int fftLength)
    {
        this.fftLength = fftLength;
        this.fftBuffer = new Complex[fftLength];
    }

    public void Add(float value)
    {
        if (fftPos < fftLength)
        {
            fftBuffer[fftPos].X = (float)(value * FastFourierTransform.HammingWindow(fftPos, fftLength));
            fftBuffer[fftPos].Y = 0;
            fftPos++;
        }

        if (fftPos >= fftLength)
        {
            FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2.0), fftBuffer);
            FftCalculated?.Invoke(this, new FftEventArgs(fftBuffer));
            fftPos = 0;
        }
    }
}

public class FftEventArgs : EventArgs
{
    public Complex[] Result { get; }
    public FftEventArgs(Complex[] result) => Result = result;
}
