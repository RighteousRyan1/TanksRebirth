using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Framework;

public struct ComputerSpecs {
    public GPU GPU;
    public CPU CPU;
    public RAM RAM;

    private static string GetHardwareData(string hwclass, string syntax) {
        using var searcher = new ManagementObjectSearcher($"SELECT * FROM {hwclass}");

        foreach (var obj in searcher.Get())
            return $"{obj[syntax]}";
        return "Data not retrieved.";
    }
    public static ComputerSpecs GetSpecs(out bool error) {
        error = false;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            error = true;
            return default;
        }
        
        try {
            return new ComputerSpecs {
                GPU = GetGPUInformation(),
                CPU = GetCpuInformation(),
                RAM = GetRamInformation()
            };
        } catch {
            error = true;
            return default;
        }
    }

    private static GPU GetGPUInformation() {
        var gpuName = GetHardwareData("Win32_VideoController", "Name");
        var gpuDriverVersion = Version.Parse(GetHardwareData("Win32_VideoController", "DriverVersion"));
        var gpuVram = uint.Parse(GetHardwareData("Win32_VideoController", "AdapterRAM"));
        return new GPU {
            VRAM = gpuVram,
            Name = gpuName,
            DriverVersion = gpuDriverVersion,
        };
    }

    private static CPU GetCpuInformation() {
        var cpuCores = int.Parse(GetHardwareData("Win32_Processor", "NumberOfCores"));
        var cpuName = GetHardwareData("Win32_Processor", "Name");
        var cpuThreads = int.Parse(GetHardwareData("Win32_Processor", "ThreadCount"));
        return new CPU {
            Cores = cpuCores,
            Threads = cpuThreads,
            Name = cpuName
        };
    }

    private static RAM GetRamInformation() {
        var ramTotalStickPhysical = ulong.Parse(GetHardwareData("Win32_ComputerSystem", "TotalPhysicalMemory"));
        var ram1xStickPhysical = ulong.Parse(GetHardwareData("Win32_PhysicalMemory", "Capacity"));
        var ramClockSpeed = uint.Parse(GetHardwareData("Win32_PhysicalMemory", "ConfiguredClockSpeed"));
        var ramSpeed = uint.Parse(GetHardwareData("Win32_PhysicalMemory", "Speed"));
        var ramManufacturer = GetHardwareData("Win32_PhysicalMemory", "Manufacturer");
        var type = uint.Parse(GetHardwareData("Win32_PhysicalMemory", "SMBIOSMemoryType"));
        var ramType = GetRamType(type);
        return new RAM {
            TotalPhysical = ramTotalStickPhysical,
            StickPhysical = ram1xStickPhysical,
            ClockSpeed = ramClockSpeed,
            Manufacturer = ramManufacturer,
            Speed = ramSpeed,
            Type = ramType
        };
    }
    
    private static string GetRamType(uint type) {
        var ramType = type switch {
            0x0 => "Unknown",
            0x1 => "Other",
            0x2 => "DRAM",
            0x3 => "Synchronous DRAM",
            0x4 => "Cache DRAM",
            0x5 => "EDO",
            0x6 => "EDRAM",
            0x7 => "VRAM",
            0x8 => "SRAM",
            0x9 => "RAM",
            0xa => "ROM",
            0xb => "Flash",
            0xc => "EEPROM",
            0xd => "FEPROM",
            0xe => "EPROM",
            0xf => "CDRAM",
            0x10 => "3DRAM",
            0x11 => "SDRAM",
            0x12 => "SGRAM",
            0x13 => "RDRAM",
            0x14 => "DDR",
            0x15 => "DDR2",
            0x16 => "DDR2 FB-DIMM",
            0x17 => "Undefined 23",
            0x18 => "DDR3",
            0x19 => "FBD2",
            0x1a => "DDR4",
            _ => "Undefined",
        };
        return ramType;
    }
}

public struct GPU {
    public uint VRAM;
    public Version DriverVersion;
    public string Name;

    public override string ToString() => $"{Name}";
}

public struct CPU {
    public int Cores;
    public int Threads;
    public string Name;

    public override string ToString() => $"{Name}";
}

public struct RAM {
    public ulong TotalPhysical;
    public ulong StickPhysical;
    public uint ClockSpeed;
    public string Manufacturer;
    public uint Speed;
    public string Type;
    public override string ToString() {
        var gb = MemoryParser.FromGigabytes(TotalPhysical);
        var mem = MathF.Ceiling(gb);
        return $"{Manufacturer} {mem}GB {Type} @{ClockSpeed}hz";
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

        RamInGB = (uint)MemoryParser.FromGigabytes(ram.TotalPhysical);
    }
    /// <summary></summary>
    /// <param name="takeActions">Whether or not to take ingame action for things like lowering graphics settings.</param>
    /// <returns>The response to computer specs.</returns>
    public void Analyze(bool takeActions, out string ramResponse, out string gpuResponse, out string cpuResponse) {
        List<Action> actionsToTake = new();

        ramResponse = null;
        gpuResponse = null;
        cpuResponse = null;

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

        var gpuModelNum = int.Parse(GpuModel.Split(' ').First(str => int.TryParse(str, out var x)));

        switch (GpuMake) {
            // gtx titan moment
            case "NVIDIA" when GpuModel.Contains("RTX"):
                gpuResponse = "RTX card detected. Highly sufficient GPU.";
                break;
            case "NVIDIA" when GpuModel.Contains("GTX"): {
                gpuResponse = gpuModelNum switch {
                    < 750 => "Lower-end GTX card detected, FPS will be substantial under normal conditions.",
                    >= 750 and <= 1000 => "Mid-range GTX card detected. FPS will be good normal conditions.",
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

        // User requested to take no actions.
        if (!takeActions) 
            return;
        
        // TODO: Write the required actions to take according to our analysis.

        actionsToTake.ForEach(action => action?.Invoke());
    }

    public override string ToString() => $"CPU: {CpuMake}:{CpuModel} | GPU: {GpuMake}:{GpuModel}";
}

public static class MemoryParser {
    public static ulong FromBits(ulong bytes) => bytes * 8;
    public static float FromKilobytes(ulong bytes) => bytes / 1024f;
    public static float FromMegabytes(ulong bytes) => bytes / 1024f / 1024f;
    public static float FromGigabytes(ulong bytes) => bytes / 1024f / 1024f / 1024f;
    public static float FromTerabytes(ulong bytes) => bytes / 1024f / 1024f / 1024f / 1024f;
}