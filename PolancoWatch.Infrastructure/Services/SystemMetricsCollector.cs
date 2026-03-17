using System.Diagnostics;
using System.Runtime.InteropServices;
using PolancoWatch.Application.Interfaces;
using PolancoWatch.Domain.Models;

namespace PolancoWatch.Infrastructure.Services;

using System.Management;

#pragma warning disable CA1416
public class SystemMetricsCollector : IMetricsCollector
{
    private ulong _previousTotalTime;
    private ulong _previousIdleTime;

    private PerformanceCounter? _winCpuCounter;
    private PerformanceCounter? _winMemCounter;

    public SystemMetricsCollector()
    {
        if (OperatingSystem.IsWindows())
        {
            try {
                _winCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _winCpuCounter.NextValue();
                
                _winMemCounter = new PerformanceCounter("Memory", "Available Bytes");
            } catch (Exception) { }
        }
    }

    public async Task<ServerMetricsSnapshot> CollectMetricsAsync()
    {
        var snapshot = new ServerMetricsSnapshot();

        if (OperatingSystem.IsLinux())
        {
            await ParseCpuLinuxAsync(snapshot.Cpu);
            await ParseMemoryLinuxAsync(snapshot.Memory);
            await ParseNetworkLinuxAsync(snapshot.Networks);
            await ParseSystemInfoLinuxAsync(snapshot.SystemInfo);
            await ParseDiskAsync(snapshot.Disks);
            await ParseProcessesLinuxAsync(snapshot.TopProcesses);
        }
        else if (OperatingSystem.IsWindows())
        {
            await ParseCpuWindowsAsync(snapshot.Cpu);
            await ParseMemoryWindowsAsync(snapshot.Memory);
            await ParseNetworkWindowsAsync(snapshot.Networks);
            await ParseSystemInfoWindowsAsync(snapshot.SystemInfo);
            await ParseDiskAsync(snapshot.Disks);
            await ParseProcessesWindowsAsync(snapshot.TopProcesses);
        }

        return snapshot;
    }

    private async Task ParseCpuWindowsAsync(CpuMetrics metric)
    {
        if (_winCpuCounter != null)
        {
            metric.TotalUsagePercentage = Math.Round(_winCpuCounter.NextValue(), 2);
        }
        await Task.CompletedTask;
    }

    private async Task ParseMemoryWindowsAsync(MemoryMetrics metric)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    metric.TotalRamBytes = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                }

                if (_winMemCounter != null)
                {
                    metric.FreeRamBytes = (long)_winMemCounter.NextValue();
                    metric.UsedRamBytes = metric.TotalRamBytes - metric.FreeRamBytes;
                }
            }
            catch {
                // Fallback to GC info if WMI fails
                var gcInfo = GC.GetGCMemoryInfo();
                metric.TotalRamBytes = gcInfo.TotalAvailableMemoryBytes;
            }
        }

        if (metric.TotalRamBytes > 0)
        {
            metric.UsagePercentage = Math.Round(((double)metric.UsedRamBytes / metric.TotalRamBytes) * 100, 2);
        }
        await Task.CompletedTask;
    }

    private async Task ParseNetworkWindowsAsync(List<NetworkMetrics> networks)
    {
        var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
        foreach (var ni in interfaces)
        {
            if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
            {
                var stats = ni.GetIPStatistics();
                networks.Add(new NetworkMetrics
                {
                    InterfaceName = ni.Name,
                    IncomingBytesPerSecond = stats.BytesReceived,
                    OutgoingBytesPerSecond = stats.BytesSent
                });
            }
        }
        await Task.CompletedTask;
    }

    private async Task ParseSystemInfoWindowsAsync(SystemInfoMetrics metric)
    {
        metric.Hostname = Environment.MachineName;
        metric.OsVersion = RuntimeInformation.OSDescription;
        metric.KernelVersion = Environment.OSVersion.VersionString;
        metric.Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        await Task.CompletedTask;
    }

    private async Task ParseProcessesWindowsAsync(List<ProcessMetrics> processes)
    {
        try
        {
            var running = Process.GetProcesses()
                .OrderByDescending(p => p.WorkingSet64)
                .Take(10);
                
            foreach (var p in running)
            {
                processes.Add(new ProcessMetrics
                {
                    ProcessId = p.Id,
                    Name = p.ProcessName,
                    MemoryUsageBytes = p.WorkingSet64,
                    CpuUsagePercentage = 0 // CPU per process requires another PerformanceCounter instance per process
                });
            }
        } catch { }
        await Task.CompletedTask;
    }

    private async Task ParseCpuLinuxAsync(CpuMetrics metric)
    {
        string statContent = await File.ReadAllTextAsync("/proc/stat");
        string firstLine = statContent.Split('\n')[0];
        string[] parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        ulong currentTotalTime = 0;
        for (int i = 1; i < parts.Length; i++)
        {
            currentTotalTime += ulong.Parse(parts[i]);
        }
        
        ulong currentIdleTime = ulong.Parse(parts[4]);

        ulong deltaTotal = currentTotalTime - _previousTotalTime;
        ulong deltaIdle = currentIdleTime - _previousIdleTime;

        if (deltaTotal > 0)
        {
           metric.TotalUsagePercentage = Math.Round((1.0 - ((double)deltaIdle / deltaTotal)) * 100, 2);
        }

        _previousTotalTime = currentTotalTime;
        _previousIdleTime = currentIdleTime;
    }

    private async Task ParseMemoryLinuxAsync(MemoryMetrics metric)
    {
        string[] memInfoLines = await File.ReadAllLinesAsync("/proc/meminfo");
        
        foreach (var line in memInfoLines)
        {
            if (line.StartsWith("MemTotal:")) metric.TotalRamBytes = ParseMemInfoValue(line);
            else if (line.StartsWith("MemAvailable:")) metric.FreeRamBytes = ParseMemInfoValue(line);
        }

        metric.UsedRamBytes = metric.TotalRamBytes - metric.FreeRamBytes;
        if (metric.TotalRamBytes > 0)
        {
            metric.UsagePercentage = Math.Round(((double)metric.UsedRamBytes / metric.TotalRamBytes) * 100, 2);
        }
    }

    private long ParseMemInfoValue(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && long.TryParse(parts[1], out long valueKb)) return valueKb * 1024;
        return 0;
    }
    
    private async Task ParseNetworkLinuxAsync(List<NetworkMetrics> networks)
    {
        string[] devLines = await File.ReadAllLinesAsync("/proc/net/dev");
        for (int i = 2; i < devLines.Length; i++)
        {
            var line = devLines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            var parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) continue;
            
            var interfaceName = parts[0].Trim();
            if (interfaceName == "lo") continue;

            var values = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (values.Length >= 9)
            {
                networks.Add(new NetworkMetrics
                {
                    InterfaceName = interfaceName,
                    IncomingBytesPerSecond = long.Parse(values[0]),
                    OutgoingBytesPerSecond = long.Parse(values[8])
                });
            }
        }
    }
    
    private async Task ParseSystemInfoLinuxAsync(SystemInfoMetrics metric)
    {
        if (File.Exists("/proc/sys/kernel/hostname")) metric.Hostname = (await File.ReadAllTextAsync("/proc/sys/kernel/hostname")).Trim();
        if (File.Exists("/proc/version")) metric.KernelVersion = (await File.ReadAllTextAsync("/proc/version")).Split(' ')[2];
        if (File.Exists("/etc/os-release"))
        {
            var lines = await File.ReadAllLinesAsync("/etc/os-release");
            var prettyNameLine = lines.FirstOrDefault(l => l.StartsWith("PRETTY_NAME="));
            if (prettyNameLine != null) metric.OsVersion = prettyNameLine.Split('=')[1].Trim('"');
        }
        if (File.Exists("/proc/uptime"))
        {
            var uptimeText = await File.ReadAllTextAsync("/proc/uptime");
            metric.Uptime = TimeSpan.FromSeconds(double.Parse(uptimeText.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]));
        }
    }

    private async Task ParseDiskAsync(List<DiskMetrics> disks)
    {
        try
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    if (drive.Name.StartsWith("/snap") || drive.Name.StartsWith("/sys") || drive.Name.StartsWith("/run") || drive.Name.StartsWith("/dev"))
                        continue;

                    var total = drive.TotalSize;
                    var free = drive.TotalFreeSpace;
                    var used = total - free;
                    double percentage = total > 0 ? Math.Round(((double)used / total) * 100, 2) : 0;

                    disks.Add(new DiskMetrics
                    {
                        MountPoint = drive.Name,
                        TotalSpaceBytes = total,
                        FreeSpaceBytes = free,
                        UsedSpaceBytes = used,
                        UsagePercentage = percentage
                    });
                }
            }
        } catch { }
        await Task.CompletedTask;
    }

    private async Task ParseProcessesLinuxAsync(List<ProcessMetrics> processes)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ps",
                Arguments = "-eo pid,comm,%cpu,rss --sort=-%cpu | head -n 11",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return;

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4 && int.TryParse(parts[0], out int pid) && double.TryParse(parts[2], out double cpu) && long.TryParse(parts[3], out long rss))
                {
                    processes.Add(new ProcessMetrics
                    {
                        ProcessId = pid,
                        Name = parts[1],
                        CpuUsagePercentage = Math.Round(cpu, 2),
                        MemoryUsageBytes = rss * 1024
                    });
                }
            }
        } catch { }
    }
}
#pragma warning restore CA1416
