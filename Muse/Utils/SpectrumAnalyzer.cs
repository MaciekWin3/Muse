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

        // Use a WaveBuffer to handle samples correctly
        var buffer = new WaveBuffer(e.Buffer);
        int samplesRead = e.BytesRecorded / 4; // Assuming 32-bit float for WasapiLoopback

        if (capture.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                sampleAggregator.Add(buffer.FloatBuffer[i]);
            }
        }
        else if (capture.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
        {
            // Fallback for 16-bit PCM if loopback uses that
            for (int i = 0; i < e.BytesRecorded / 2; i++)
            {
                sampleAggregator.Add(buffer.ShortBuffer[i] / 32768f);
            }
        }
    }

    private void OnFftCalculated(object? sender, FftEventArgs e)
    {
        // Process FFT results into 10 bins for the equalizer
        int bins = 10;
        float[] newSpectrum = new float[bins];
        
        // We only care about the first half of the FFT (up to Nyquist frequency)
        int usableBins = fftLength / 2;
        
        // Use logarithmic binning for a more "musical" distribution
        // (bass gets fewer bins, treble gets more)
        for (int i = 0; i < bins; i++)
        {
            int startBin = (int)Math.Pow(usableBins, (double)i / bins);
            int endBin = (int)Math.Pow(usableBins, (double)(i + 1) / bins);
            if (endBin <= startBin) endBin = startBin + 1;

            float sum = 0;
            int count = 0;
            for (int j = startBin; j < endBin && j < e.Result.Length; j++)
            {
                float magnitude = (float)Math.Sqrt(e.Result[j].X * e.Result[j].X + e.Result[j].Y * e.Result[j].Y);
                sum += magnitude;
                count++;
            }
            
            float avg = count > 0 ? sum / count : 0;
            // Boost lower frequencies and apply logarithmic scaling
            float boost = 1.0f + (bins - i) * 0.5f;
            newSpectrum[i] = (float)Math.Clamp(Math.Log10(avg * boost + 1) * 80, 0, 100);
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
