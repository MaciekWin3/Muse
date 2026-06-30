namespace Muse.Utils;

public sealed class SpectrumAnalyzer : IDisposable
{
    public float[] SpectrumData { get; private set; } = new float[10];

    public void Dispose()
    {
    }
}
