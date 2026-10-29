using PulseBar.Core;

namespace PulseBar.Core.Tests;

public class GpuEnginesTests
{
    private const string Igpu = "0x00000000_0x0000A1B2_phys_0";
    private const string Dgpu = "0x00000000_0x0000D1B4_phys_0";

    [Fact]
    public void AdapterIdFromEngineInstance() =>
        Assert.Equal(Dgpu, GpuEngines.AdapterId("pid_1234_luid_0x00000000_0x0000D1B4_phys_0_eng_0_engtype_3D"));

    [Fact]
    public void AdapterIdFromMemoryInstance() =>
        Assert.Equal(Dgpu, GpuEngines.AdapterId("luid_0x00000000_0x0000D1B4_phys_0"));

    [Fact]
    public void AdapterIdUnknown() => Assert.Null(GpuEngines.AdapterId("_Total"));

    [Fact]
    public void UtilizationIsBusiestEngineTypeSummedAcrossProcesses()
    {
        var samples = new (string, double)[]
        {
            ($"pid_1_luid_{Dgpu}_eng_0_engtype_3D", 30),
            ($"pid_2_luid_{Dgpu}_eng_0_engtype_3D", 25),
            ($"pid_2_luid_{Dgpu}_eng_5_engtype_VideoDecode", 40),
            ($"pid_9_luid_{Igpu}_eng_0_engtype_3D", 5),
        };
        var result = GpuEngines.UtilizationByAdapter(samples);
        Assert.Equal(55, result[Dgpu]);
        Assert.Equal(5, result[Igpu]);
    }

    [Fact]
    public void UtilizationCapsAt100()
    {
        var samples = new (string, double)[]
        {
            ($"pid_1_luid_{Dgpu}_eng_0_engtype_3D", 80),
            ($"pid_2_luid_{Dgpu}_eng_0_engtype_3D", 70),
        };
        Assert.Equal(100, GpuEngines.UtilizationByAdapter(samples)[Dgpu]);
    }

    [Fact]
    public void PickAdapterPrefersMostDedicatedMemory()
    {
        var memory = new (string, double)[]
        {
            ($"luid_{Igpu}", 128e6),
            ($"luid_{Dgpu}", 1.1e9),
        };
        Assert.Equal(Dgpu, GpuEngines.PickAdapter(memory));
    }
}
