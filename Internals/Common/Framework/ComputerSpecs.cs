using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TanksRebirth.Internals.Common.Framework;

public struct ComputerSpecs : IEquatable<ComputerSpecs>
{
    public GPU GPU;
    public CPU CPU;
    public RAM RAM;

    private Computer _sysComputer;

    public static ComputerSpecs GetSpecs(out bool error) {
        error = false;

        try {
            ComputerSpecs specs = new();


            specs._sysComputer = new Computer {
                IsGpuEnabled = true,
                IsCpuEnabled = true,
                IsMemoryEnabled = true
            };
            specs._sysComputer.Open();

            specs.GPU = specs.GetGpuInfo();
            specs.CPU = specs.GetCpuInfo();
            specs.RAM = specs.GetRamInfo();

            return specs;
        }
        catch when (!Debugger.IsAttached) {
            error = true;
            return default;
        }
    }

    private GPU GetGpuInfo() {
        foreach (var hardware in _sysComputer.Hardware) {
            if (hardware.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel) {
                hardware.Update();
                var name = CleanGpuName(hardware.Name);
                var vramSensor = hardware.Sensors.FirstOrDefault(s =>
                    s.Name == "GPU Memory Total");

                var gpu = new GPU {
                    Name = name,
                    VRAM = vramSensor != null ? (uint)vramSensor.Value.GetValueOrDefault() : 0,
                };

                //Console.WriteLine("GPU Sensors:\n " + string.Join("\n", hardware.Sensors.Select(x => x.Name)));

                return gpu;
            }
        }

        throw new Exception("No suitable GPU found.");
    }

    private CPU GetCpuInfo() {
        foreach (var hardware in _sysComputer.Hardware) {
            if (hardware.HardwareType == HardwareType.Cpu) {
                hardware.Update();

                string name = CleanCpuName(hardware.Name);
                int coreCount = hardware.Sensors
                    .Where(s => s.SensorType == SensorType.Load && s.Name == "")
                    .Select(s => s.Name)
                    .Distinct()
                    .Count();

                //Console.WriteLine("CPU Sensors:\n " + string.Join("\n", hardware.Sensors.Select(x => x.Name)));

                int threadCount = Environment.ProcessorCount;

                return new CPU {
                    Name = name,
                    CoreCount = coreCount,
                    Threads = threadCount
                };
            }
        }

        throw new Exception("No CPU found.");
    }

    private RAM GetRamInfo() {
        var mem = _sysComputer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory) ?? throw new Exception("No memory hardware found.");

        mem.Update();

        //Console.WriteLine("RAM Sensors:\n " + string.Join("\n", mem.Sensors.Select(x => x.Name)));

        // oh well, processing power gone, but for a good cause.
        // also this code is placeholder and def doesn't work with LibreHardwareMonitorLib

        float? used = mem.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Used")?.Value;
        float? available = mem.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Available")?.Value;

        var firstValidStick = _sysComputer.SMBios.MemoryDevices.First(x => x.ConfiguredSpeed > 0 && x.Speed > 0 && x.Size > 0);

        var ram = new RAM {
            TotalPhysical = used.HasValue && available.HasValue ? 
            (ulong)MemoryParser.To(MemoryParser.Size.Gigabytes, MemoryParser.Size.Bytes, (ulong)MathF.Round(used.Value + available.Value)) : 0ul,
            // no safety checks because how tf are they running this game without physical memory?
            Manufacturer = firstValidStick.ManufacturerName,
            Speed = firstValidStick.ConfiguredSpeed,
            Type = firstValidStick.Type
        };

        return ram;
    }

    private static string CleanGpuName(string name) {
        return name?.Replace("(", "").Split(')')[0].Trim() ?? "Unknown";
    }

    private static string CleanCpuName(string name) {
        return name?
            .Replace("Processor", "", StringComparison.OrdinalIgnoreCase)
            .Replace("CPU", "", StringComparison.OrdinalIgnoreCase)
            .Trim() ?? "Unknown";
    }

    public readonly bool Equals(ComputerSpecs other) => GPU.Equals(other.GPU) && CPU.Equals(other.CPU) && RAM.Equals(other.RAM);
    public override readonly bool Equals(object? obj) => obj is ComputerSpecs other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(GPU, CPU, RAM);
    public static bool operator ==(ComputerSpecs left, ComputerSpecs right) => left.Equals(right);
    public static bool operator !=(ComputerSpecs left, ComputerSpecs right) => !left.Equals(right);
}

public struct GPU : IEquatable<GPU> {
    public uint VRAM;
    // normally System.Version
    // LH doesn't allow getting this :(
    // public string? DriverVersion;
    public string Name;

    public override readonly string ToString() {
        var gb = MemoryParser.To(MemoryParser.Size.Megabytes, MemoryParser.Size.Gigabytes, VRAM);
        var gbRounded = MathF.Round(gb);
        return $"{Name} (VRAM: {gbRounded} GB)";
    }

    public readonly bool Equals(GPU other) {
        return VRAM == other.VRAM && Name == other.Name;
    }

    public override readonly bool Equals(object? obj) {
        return obj is GPU other && Equals(other);
    }

    public override readonly int GetHashCode() {
        return HashCode.Combine(VRAM, Name);
    }

    public static bool operator ==(GPU left, GPU right) {
        return left.Equals(right);
    }

    public static bool operator !=(GPU left, GPU right) {
        return !left.Equals(right);
    }
}
public struct CPU : IEquatable<CPU> {
    public int CoreCount;
    public int Threads;
    public string Name;

    public override readonly string ToString() => $"{Name} (Core Count: {CoreCount})";

    public readonly bool Equals(CPU other) {
        return CoreCount == other.CoreCount && Threads == other.Threads && Name == other.Name;
    }

    public override bool Equals(object? obj) {
        return obj is CPU other && Equals(other);
    }

    public override readonly int GetHashCode() {
        return HashCode.Combine(CoreCount, Threads, Name);
    }

    public static bool operator ==(CPU left, CPU right) {
        return left.Equals(right);
    }

    public static bool operator !=(CPU left, CPU right) {
        return !left.Equals(right);
    }
}
public struct RAM : IEquatable<RAM> {
    public ulong TotalPhysical;

    // one would assume these wouldn't be different on a per-stick basis...
    public string? Manufacturer;
    public uint Speed;
    public MemoryType Type;

    public override readonly string ToString() {
        var gb = MemoryParser.To(MemoryParser.Size.Bytes, MemoryParser.Size.Gigabytes, TotalPhysical);
        var mem = MathF.Ceiling(gb); // vs ceiling?
        return $"{Manufacturer} {mem}GB {Type} @{Speed}hz";
    }

    public readonly bool Equals(RAM other) {
        return TotalPhysical == other.TotalPhysical && Manufacturer == other.Manufacturer && Speed == other.Speed && Type == other.Type;
    }

    public override readonly bool Equals(object? obj) {
        return obj is RAM other && Equals(other);
    }

    public override readonly int GetHashCode() {
        return HashCode.Combine(TotalPhysical, Manufacturer, Speed, Type);
    }

    public static bool operator ==(RAM left, RAM right) {
        return left.Equals(right);
    }

    public static bool operator !=(RAM left, RAM right) {
        return !left.Equals(right);
    }
}

/// <summary>
/// Analyzes computer specs and retrieves a performance level.
/// </summary>
public struct SpecAnalysis {
    public string GpuMake;
    public string GpuModel;

    public string CpuMake;
    public string CpuModel;

    public uint RamInGB;

    public SpecAnalysis(GPU gpu, CPU cpu, RAM ram) {
        var gpuInfo = gpu.Name.Split(' ');
        GpuMake = gpuInfo[0];
        GpuModel = string.Join(" ", gpuInfo[1..]).Trim();

        var cpuInfo = cpu.Name.Split(' ');
        CpuMake = cpuInfo[0];
        CpuModel = string.Join(" ", cpuInfo[1..]).Trim();

        RamInGB = (uint)Math.Round(MemoryParser.To(MemoryParser.Size.Bytes, MemoryParser.Size.Gigabytes, ram.TotalPhysical));
    }

    /// <summary></summary>
    /// <param name="takeActions">Whether or not to take ingame action for things like lowering graphics settings.</param>
    /// <param name="ramResponse">The response to the given RAM specs.</param>
    /// <param name="gpuResponse">The response to the given GPU specs.</param>
    /// <param name="cpuResponse">The response to the given CPU specs.</param>
    public readonly void Analyze(bool takeActions, out string ramResponse, out string gpuResponse, out string cpuResponse) {
        List<Action> actionsToTake = new();

        ramResponse = gpuResponse = cpuResponse = string.Empty;

        try {
            switch (RamInGB) {
                case <= 2:
                    ramResponse = "2GB or less of memory. Can cause performance issues.";
                    break;
                case > 2 and <= 4:
                    ramResponse = "4GB or less of memory. Decent potential for performance issues.";
                    break;
                case > 4 and < 8:
                    ramResponse = "Less than 8GB of memory. Issues may occur depending on how many processes are open.";
                    break;
                case >= 8:
                    ramResponse = "Sufficient memory.";
                    break;
            }
        }
        catch {
            ramResponse = "Error";
        }

        try {
            switch (CpuMake) {
                // TODO: Finish code to obtain CPU name.
                case "AMD": {
                    var split = CpuModel.Split(' ');
                    var cpuGen = int.Parse(split.First(str => int.TryParse(str, out var x)));

                    cpuResponse = "AMD CPU detected.";
                    break;
                }
                case "Intel": {
                    var split = CpuModel.Split(' ');
                    var cpuModelNum = int.Parse(split.First(str => int.TryParse(str, out var x)));
                    cpuResponse = "Intel CPU detected.";
                    break;
                }
            }
        }
        catch {
            cpuResponse = "Error";
        }

        try {
            var gpuModelNum = int.Parse(GpuModel.Split(' ').First(str => int.TryParse(str, out var x)));

            switch (GpuMake) {
                // gtx titan moment
                case "NVIDIA" when GpuModel.Contains("RTX"):
                    gpuResponse = "RTX card detected. GPU will never be a problem.";
                    break;
                case "NVIDIA" when GpuModel.Contains("GTX"): {
                    gpuResponse = gpuModelNum switch {
                        < 750 => "Lower-end GTX card detected, FPS will usually be decent.",
                        >= 750 and <= 1000 => "Mid-range GTX card detected. FPS will usually be good.",
                        _ => "High-end GTX Card detected. FPS will be great under normal conditions."
                    };
                    break;
                }
                case "NVIDIA" when GpuModel.Contains("GT") && !GpuModel.Contains("GTX"): {
                    gpuResponse = "Low-end graphics card detected (GT). Expect GPU bottlenecks.";
                    break;
                }
                case "AMD":
                    gpuResponse = "AMD GPU detected.";
                    break;
                case "Intel":
                    gpuResponse = "Intel GPU detected. Intel GPUs remain untested.";
                    break;
            }
        }
        catch {
            gpuResponse = "Error";
        }

        // User requested to take no actions.
        if (!takeActions)
            return;

        // TODO: Write the required actions to take according to our analysis.

        actionsToTake.ForEach(action => action?.Invoke());
    }

    public override readonly string ToString() => $"CPU: {CpuMake}:{CpuModel}\nGPU: {GpuMake}:{GpuModel}\nRAM: {RamInGB}GB";
}

public static class MemoryParser {
    // we go all the way because why the FUCK not
    public enum Size : int {
        Bytes       = 0x001,
        Kilobytes   = 0x002,
        Megabytes   = 0x003,
        Gigabytes   = 0x004,
        Terabytes   = 0x005,
        Petabytes   = 0x006,
        Exabytes    = 0x007,
        Zettabytes  = 0x008,
        Yottabytes  = 0x009,
        Ronnabytes  = 0x00A,
        Quettabytes = 0x00B
    }
    public static ulong FromBits(ulong bytes)      => bytes * 8;

    public static float To(Size from, Size to, float value) {
        int exponentDiff = from - to;
        return value * MathF.Pow(1024, exponentDiff);
    }
}