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

        byte[] buffer = e.Buffer;
        int bytesRecorded = e.BytesRecorded;
        int bufferOffset = 0;

        while (bufferOffset < bytesRecorded)
        {
            float sample = BitConverter.ToSingle(buffer, bufferOffset);
            sampleAggregator.Add(sample);
            bufferOffset += capture.WaveFormat.BlockAlign;
        }
    }

    private void OnFftCalculated(object? sender, FftEventArgs e)
    {
        // Process FFT results into 10 bins for the equalizer
        int bins = 10;
        float[] newSpectrum = new float[bins];
        
        // We only care about the first half of the FFT (up to Nyquist frequency)
        int usableBins = fftLength / 2;
        int samplesPerBin = usableBins / bins;

        for (int i = 0; i < bins; i++)
        {
            float max = 0;
            for (int j = 0; j < samplesPerBin; j++)
            {
                int index = i * samplesPerBin + j;
                if (index < e.Result.Length)
                {
                    float magnitude = (float)Math.Sqrt(e.Result[index].X * e.Result[index].X + e.Result[index].Y * e.Result[index].Y);
                    if (magnitude > max) max = magnitude;
                }
            }
            // Logarithmic scaling for better visualization
            newSpectrum[i] = (float)Math.Clamp(Math.Log10(max + 1) * 100, 0, 100);
        }

        SpectrumData = newSpectrum;
    }

    public void Dispose()
    {
        capture?.StopRecording();
        capture?.Dispose();
        capture = null;
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
