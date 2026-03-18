using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using PolancoWatch.Application.Interfaces;
using PolancoWatch.Domain.Models;

namespace PolancoWatch.Infrastructure.Services;

using System.Management;

using Microsoft.Extensions.Logging;

#pragma warning disable CA1416
public class SystemMetricsCollector : IMetricsCollector
{
    private ulong _previousTotalTime;
    private ulong _previousIdleTime;

    private readonly ILogger<SystemMetricsCollector> _logger;
    private PerformanceCounter? _winCpuCounter;
    private PerformanceCounter? _winMemCounter;

    public SystemMetricsCollector(ILogger<SystemMetricsCollector> logger)
    {
        _logger = logger;
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
            await ParseDockerLinuxAsync(snapshot.DockerContainers);
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

    public async Task<(bool Success, string Message)> KillProcessAsync(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            process.Kill(true); // Kill entire process tree
            await process.WaitForExitAsync();
            return (true, $"Process {pid} ({process.ProcessName}) terminated successfully.");
        }
        catch (ArgumentException)
        {
            return (false, $"Process {pid} was not found. It may have already exited.");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5) // Access Denied
        {
            return (false, "Access Denied. PolancoWatch does not have sufficient permissions to terminate this process. Try running the service as Administrator/Root.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to kill process {pid}: {ex.Message}");
            return (false, $"System Error: {ex.Message}");
        }
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
            var sw = Stopwatch.StartNew();
            // Use WMI for high-performance formatted counters (this is pre-calculated by OS)
            using var searcher = new ManagementObjectSearcher(
                "SELECT IDProcess, Name, PercentProcessorTime, WorkingSetPrivate FROM Win32_PerfFormattedData_PerfProc_Process " +
                "WHERE Name != '_Total' AND Name != 'Idle'");
            
            searcher.Options.Timeout = TimeSpan.FromSeconds(2);
            int coreCount = Environment.ProcessorCount;
            var allProcesses = new List<ProcessMetrics>();

            using (var results = searcher.Get())
            {
                foreach (ManagementObject obj in results)
                {
                    try
                    {
                        var pidObj = obj["IDProcess"];
                        var nameObj = obj["Name"];
                        var cpuObj = obj["PercentProcessorTime"];
                        var memObj = obj["WorkingSetPrivate"];

                        if (pidObj == null) continue;

                        allProcesses.Add(new ProcessMetrics
                        {
                            ProcessId = Convert.ToInt32(pidObj),
                            Name = nameObj?.ToString() ?? "Unknown",
                            CpuUsagePercentage = Math.Round(Convert.ToDouble(cpuObj ?? 0) / coreCount, 1),
                            MemoryUsageBytes = Convert.ToInt64(memObj ?? 0)
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing WMI object: {ex.Message}");
                    }
                }
            }

            if (!allProcesses.Any())
            {
                _logger.LogWarning("WMI query returned no processes. Falling back to Process.GetProcesses().");
                throw new Exception("Empty WMI result"); // Trigger fallback in catch block
            }

            _logger.LogDebug("WMI Process Collection took {Elapsed}ms for {Count} items.", sw.ElapsedMilliseconds, allProcesses.Count);

            var topCpu = allProcesses.OrderByDescending(p => p.CpuUsagePercentage).Take(15);
            var topMem = allProcesses.OrderByDescending(p => p.MemoryUsageBytes).Take(15);

            var merged = topCpu.Concat(topMem)
                .GroupBy(p => p.ProcessId)
                .Select(g => g.First())
                .OrderByDescending(p => p.CpuUsagePercentage)
                .ToList();

            processes.AddRange(merged);
        } 
        catch (Exception ex) 
        {
            Debug.WriteLine($"WMI Process Collection Error: {ex.Message}");
            // Fallback to basic process list if WMI fails
            try {
                var running = Process.GetProcesses()
                    .OrderByDescending(p => p.WorkingSet64)
                    .Take(15);
                    
                foreach (var p in running)
                {
                    processes.Add(new ProcessMetrics
                    {
                        ProcessId = p.Id,
                        Name = p.ProcessName,
                        MemoryUsageBytes = p.WorkingSet64,
                        CpuUsagePercentage = 0
                    });
                }
            } catch { }
        }
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
                Arguments = "-eo pid,comm,%cpu,rss --sort=-%cpu",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return;

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int coreCount = Environment.ProcessorCount;
            
            var allProcs = new List<ProcessMetrics>();
            // Skip the header (index 0)
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    if (int.TryParse(parts[0], out int pid) && 
                        double.TryParse(parts[parts.Length - 2], out double cpu) && 
                        long.TryParse(parts[parts.Length - 1], out long rss))
                    {
                        string name = string.Join(" ", parts.Skip(1).Take(parts.Length - 3));
                        allProcs.Add(new ProcessMetrics
                        {
                            ProcessId = pid,
                            Name = name,
                            CpuUsagePercentage = Math.Round(cpu / coreCount, 2),
                            MemoryUsageBytes = rss * 1024
                        });
                    }
                }
            }

            var merged = allProcs.OrderByDescending(p => p.CpuUsagePercentage).Take(15)
                .Concat(allProcs.OrderByDescending(p => p.MemoryUsageBytes).Take(15))
                .GroupBy(p => p.ProcessId)
                .Select(g => g.First())
                .ToList();

            processes.AddRange(merged);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Linux processes via 'ps' command.");
        }
    }

    private async Task ParseDockerLinuxAsync(List<DockerContainerMetrics> dockerContainers)
    {
        try
        {
            // 1. Get basic info (ID, Name, Image, Status, State)
            var psPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps --format \"{{.ID}}|{{.Names}}|{{.Image}}|{{.Status}}|{{.State}}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var psProcess = Process.Start(psPsi);
            if (psProcess == null) return;

            string psOutput = await psProcess.StandardOutput.ReadToEndAsync();
            await psProcess.WaitForExitAsync();

            var lines = psOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var containerMap = new Dictionary<string, DockerContainerMetrics>();

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length >= 5)
                {
                    var container = new DockerContainerMetrics
                    {
                        ContainerId = parts[0],
                        Name = parts[1],
                        Image = parts[2],
                        Status = parts[3],
                        State = parts[4]
                    };
                    containerMap[container.ContainerId] = container;
                    dockerContainers.Add(container);
                }
            }

            if (!containerMap.Any()) return;

            // 2. Get resource usage (CPU, Memory)
            var statsPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "stats --no-stream --format \"{{.ID}}|{{.CPUPerc}}|{{.MemUsage}}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var statsProcess = Process.Start(statsPsi);
            if (statsProcess == null) return;

            string statsOutput = await statsProcess.StandardOutput.ReadToEndAsync();
            await statsProcess.WaitForExitAsync();

            var statsLines = statsOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in statsLines)
            {
                var parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    var id = parts[0];
                    if (containerMap.TryGetValue(id, out var container))
                    {
                        // Parse CPU (e.g., "0.05%")
                        if (double.TryParse(parts[1].Replace("%", "").Trim(), out double cpu))
                        {
                            container.CpuPercentage = cpu;
                        }

                        // Parse Memory (e.g., "1.2MiB / 7.6GiB")
                        var memPart = parts[2].Split('/')[0].Trim();
                        container.MemoryUsageBytes = ParseDockerMemory(memPart);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to collect Docker metrics: {Message}", ex.Message);
        }
    }

    private long ParseDockerMemory(string memString)
    {
        try
        {
            var parts = memString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1) return 0;

            string valueStr = "";
            string unit = "";

            foreach (var c in parts[0])
            {
                if (char.IsDigit(c) || c == '.') valueStr += c;
                else unit += c;
            }

            if (!double.TryParse(valueStr, out double value)) return 0;

            unit = unit.ToUpper();
            if (unit.Contains("G")) return (long)(value * 1024 * 1024 * 1024);
            if (unit.Contains("M")) return (long)(value * 1024 * 1024);
            if (unit.Contains("K")) return (long)(value * 1024);
            if (unit.Contains("B")) return (long)value;

            return (long)value;
        }
        catch { return 0; }
    }
}
#pragma warning restore CA1416
