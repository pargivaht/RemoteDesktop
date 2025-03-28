using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client2
{
    public class SystemInfoList
    {
        // General System Info
        public string Title { get; set; }
        public string OS { get; set; }
        public string SystemManufacturer { get; set; }
        public string SystemModel { get; set; }
        public string UserName { get; set; }
        public string MachineName { get; set; }
        public string BootTime { get; set; }
        public string Uptime { get; set; }
        public string SerialNumber { get; set; }
        public string BiosVersion { get; set; }
        public string BiosVendor { get; set; }

        // CPU Info
        public string CPU { get; set; }
        public int CPUCores { get; set; }
        public int CPUThreads { get; set; }
        public string CPUClockSpeed { get; set; }
        public string CPUBoostClock { get; set; }
        public string Virtualization { get; set; }

        // RAM Info
        public string RAM { get; set; }
        public string RAMSpeed { get; set; }
        public string RAMManufacturer { get; set; }

        // GPU Info
        public string GPU { get; set; }
        public string GPUMemory { get; set; }
        public string GPUDriverVersion { get; set; }
        public string GPUFps { get; set; }

        // Storage Info
        public string Drives { get; set; }


        // Motherboard Info
        public string MotherboardModel { get; set; }
        public string MotherboardManufacturer { get; set; }


        // Network Info
        public string IPAddress { get; set; }
        public string PublicIPAddress { get; set; }
        public string MACAddress { get; set; }
        public string NetworkAdapter { get; set; }
        public string NetworkSpeed { get; set; }


        // Battery Info
        public string BatteryStatus { get; set; }
        public string BatteryPercentage { get; set; }
        public string BatteryCapacity { get; set; }

        // Peripheral Info

        public string DisplayResolution { get; set; }
        public string ConnectedDisplays { get; set; }

        // Security Info
        public string InstalledAntivirus { get; set; }

        // Virtualization Info
        public string IsVirtualizationEnabled { get; set; }
    }
}
