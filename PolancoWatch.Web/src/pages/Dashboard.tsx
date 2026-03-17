import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMetrics } from '../hooks/useMetrics';
import { authService } from '../services/api';
import { Activity, Cpu, HardDrive, Network, Server, MemoryStick } from 'lucide-react';
import { MetricChart } from '../components/MetricChart';

export default function Dashboard() {
    const navigate = useNavigate();
    const { metrics, isConnected, alerts, cpuHistory, memoryHistory, networkInHistory, networkOutHistory } = useMetrics();

    useEffect(() => {
        if (!authService.isAuthenticated()) {
            navigate('/login');
        }
    }, [navigate]);

    return (
        <div className="min-h-screen bg-obsidian-950 text-slate-300 font-sans selection:bg-brand-primary/30 flex-1 pl-0 lg:pl-20 xl:pl-72 transition-all duration-500">
            {/* Background Texture Overlay */}
            <div className="fixed inset-0 pointer-events-none opacity-[0.03] z-0" 
                 style={{ backgroundImage: `url("data:image/svg+xml,%3Csvg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='noiseFilter'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.65' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23noiseFilter)'/%3E%3C/svg%3E")` }}>
            </div>

            <main className="max-w-7xl mx-auto px-6 lg:px-8 py-12 relative z-10 space-y-8">
                <header className="flex justify-between items-center mb-12">
                     <div className="flex flex-col">
                        <span className="text-[10px] uppercase tracking-[0.2em] text-slate-400 font-bold mb-1">Live Console</span>
                        <h1 className="text-4xl font-black text-white tracking-tighter">System <span className="text-brand-primary">Overview</span></h1>
                    </div>

                    <div className="flex items-center gap-6">
                        <div className="flex flex-col text-right">
                            <span className="text-[10px] uppercase tracking-widest text-slate-400 font-bold mb-0.5">Connection Status</span>
                            {isConnected ? (
                                <span className="text-xs font-bold text-brand-secondary flex items-center justify-end gap-2 drop-shadow-[0_0_8px_rgba(34,211,238,0.4)]">
                                    <span className="w-1.5 h-1.5 rounded-full bg-brand-secondary animate-pulse"></span>
                                    ONLINE
                                </span>
                            ) : (
                                <span className="text-xs font-bold text-amber-400 flex items-center justify-end gap-2">
                                    <span className="w-1.5 h-1.5 rounded-full bg-amber-400 animate-bounce"></span>
                                    RECONNECTING
                                </span>
                            )}
                        </div>
                    </div>
                </header>

                {/* System Info Header */}
                <div className="glass-panel rounded-4xl p-8 flex flex-col md:flex-row items-center justify-between gap-6 border-glow">

                    <div className="flex items-center gap-6">
                        <div className="w-16 h-16 bg-brand-primary/10 text-brand-primary rounded-2xl flex items-center justify-center border border-brand-primary/20 shadow-[0_0_20px_rgba(139,92,246,0.1)]">
                            <Server size={32} strokeWidth={1.5} />
                        </div>
                        <div>
                            <div className="flex items-center gap-3">
                                <h1 className="text-2xl font-bold text-white tracking-tight">{metrics?.systemInfo?.hostname || 'PROBING HOST...'}</h1>
                                <span className="px-2 py-0.5 rounded bg-brand-secondary/10 text-brand-secondary text-[10px] font-bold border border-brand-secondary/20 uppercase tracking-widest">Master Node</span>
                            </div>
                            <p className="text-sm text-slate-300 font-semibold mt-2 flex items-center gap-4">
                                <span className="flex items-center gap-1.5"><Activity size={14} className="text-brand-secondary" /> {metrics?.systemInfo?.osVersion}</span>
                                <span className="w-1 h-1 rounded-full bg-slate-600"></span>
                                <span className="font-mono text-xs opacity-90">Kern: {metrics?.systemInfo?.kernelVersion}</span>
                            </p>
                        </div>
                    </div>
                    <div className="flex flex-col items-center md:items-end bg-white/10 py-4 px-8 rounded-2xl border border-white/10 min-w-[200px] shadow-inner">
                        <p className="text-[10px] text-slate-300 uppercase tracking-[0.2em] font-black mb-2">System Uptime</p>
                        <p className="text-4xl font-black font-mono text-white tracking-tighter text-glow-primary">
                            {metrics?.systemInfo?.uptime?.split('.')[0] || '00:00:00'}
                        </p>
                    </div>
                </div>

                {/* Primary Metrics Grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
                    {/* CPU Card */}
                    <div className="glass-card rounded-4xl p-8 relative overflow-hidden group border-white/5">
                        <div className="absolute top-0 right-0 w-40 h-40 bg-brand-primary/10 rounded-full blur-[80px] -mr-16 -mt-16 transition-all group-hover:bg-brand-primary/20"></div>
                        <div className="flex justify-between items-start mb-6 relative z-10">
                            <div>
                                <p className="text-[10px] font-black text-slate-300 uppercase tracking-widest flex items-center gap-2 mb-3">
                                    <Cpu size={14} className="text-brand-primary" /> Processor Load
                                </p>
                                <div className="flex items-baseline gap-1">
                                    <h3 className="text-6xl font-black text-white font-mono tracking-tighter transition-transform group-hover:scale-105 duration-500 drop-shadow-2xl">
                                        {metrics?.cpu?.totalUsagePercentage?.toFixed(1) || 0}
                                    </h3>
                                    <span className="text-2xl font-black text-brand-primary opacity-80">%</span>
                                </div>
                            </div>
                        </div>
                        <div className="h-24 -mx-2">
                             <MetricChart data={cpuHistory} color="#a78bfa" domain={[0, 100]} formatter={(v) => `${v.toFixed(0)}%`} />
                        </div>
                    </div>

                    {/* RAM Card */}
                    <div className="glass-card rounded-4xl p-8 relative overflow-hidden group border-white/5">
                        <div className="absolute top-0 right-0 w-40 h-40 bg-brand-secondary/10 rounded-full blur-[80px] -mr-16 -mt-16 transition-all group-hover:bg-brand-secondary/20"></div>
                        <div className="flex justify-between items-start mb-6 relative z-10">
                            <div>
                                <p className="text-[10px] font-black text-slate-300 uppercase tracking-widest flex items-center gap-2 mb-3">
                                    <MemoryStick size={14} className="text-brand-secondary" /> Memory Volality
                                </p>
                                <div className="flex items-baseline gap-1">
                                    <h3 className="text-6xl font-black text-white font-mono tracking-tighter transition-transform group-hover:scale-105 duration-500 text-glow-secondary drop-shadow-2xl">
                                        {metrics?.memory?.usagePercentage?.toFixed(1) || 0}
                                    </h3>
                                    <span className="text-2xl font-black text-brand-secondary opacity-80">%</span>
                                </div>
                                <div className="text-[10px] text-white font-black font-mono mt-4 uppercase tracking-tighter bg-white/10 px-3 py-1.5 rounded-lg border border-white/5">
                                    {((metrics?.memory?.usedRamBytes || 0) / 1024 / 1024 / 1024).toFixed(2)}G / {((metrics?.memory?.totalRamBytes || 0) / 1024 / 1024 / 1024).toFixed(2)}G
                                </div>
                            </div>
                        </div>
                        <div className="h-24 -mx-2">
                            <MetricChart data={memoryHistory} color="#22d3ee" domain={[0, 100]} formatter={(v) => `${v.toFixed(0)}%`} />
                        </div>
                    </div>

                    {/* Network Card */}
                    <div className="glass-card rounded-4xl p-8 relative overflow-hidden group lg:col-span-2 border-white/5">
                        <div className="absolute top-0 right-0 w-60 h-60 bg-brand-primary/10 rounded-full blur-[100px] -mr-32 -mt-32 transition-all"></div>
                        <div className="flex justify-between items-start mb-8 relative z-10">
                            <p className="text-[10px] font-black text-slate-300 uppercase tracking-[0.2em] flex items-center gap-2">
                                <Network size={14} className="text-brand-secondary" /> Real-time Throughput
                            </p>
                        </div>
                        <div className="grid grid-cols-2 gap-10 relative z-10">
                            <div>
                                <div className="flex items-center gap-2 mb-3">
                                    <span className="w-2 h-2 rounded-full bg-brand-secondary animate-pulse shadow-[0_0_12px_rgba(34,211,238,0.8)]"></span>
                                    <p className="text-[10px] text-slate-300 uppercase tracking-widest font-black">Inbound</p>
                                </div>
                                <div className="flex items-baseline gap-1 mb-4">
                                    <p className="text-5xl font-black font-mono text-white tracking-tighter drop-shadow-2xl">
                                        {((metrics?.networks?.[0]?.incomingBytesPerSecond || 0) / 1024 / 1024).toFixed(2)}
                                    </p>
                                    <span className="text-sm font-black text-brand-secondary">MB/s</span>
                                </div>
                                <div className="h-20">
                                    <MetricChart data={networkInHistory} color="#22d3ee" domain={['auto', 'auto']} formatter={(v) => `${v.toFixed(1)}`} />
                                </div>
                            </div>
                            <div>
                                <div className="flex items-center gap-2 mb-3 text-right justify-end">
                                    <p className="text-[10px] text-slate-300 uppercase tracking-widest font-black">Outbound</p>
                                    <span className="w-2 h-2 rounded-full bg-brand-primary animate-pulse shadow-[0_0_12px_rgba(167,139,250,0.8)]"></span>
                                </div>
                                <div className="flex items-baseline gap-1 mb-4 justify-end">
                                    <p className="text-5xl font-black font-mono text-white tracking-tighter drop-shadow-2xl">
                                        {((metrics?.networks?.[0]?.outgoingBytesPerSecond || 0) / 1024 / 1024).toFixed(2)}
                                    </p>
                                    <span className="text-sm font-black text-brand-primary">MB/s</span>
                                </div>
                                <div className="h-20">
                                    <MetricChart data={networkOutHistory} color="#a78bfa" domain={['auto', 'auto']} formatter={(v) => `${v.toFixed(1)}`} />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                    {/* Disks List */}
                    <div className="glass-card rounded-4xl p-8 lg:col-span-2 border-white/5">
                        <div className="flex justify-between items-center mb-8">
                            <h3 className="text-sm font-black text-white uppercase tracking-[0.2em] flex items-center gap-3">
                                <HardDrive size={18} className="text-brand-secondary" /> Volume Health
                            </h3>
                            <span className="text-[10px] text-slate-400 font-bold uppercase tracking-widest">{metrics?.disks?.length || 0} ACTIVE DRIVES</span>
                        </div>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            {metrics?.disks?.map((disk, idx) => (
                                <div key={idx} className="p-6 rounded-2xl bg-white/2 border border-white/4 hover:bg-white/4 transition-all group">
                                    <div className="flex justify-between items-center mb-4">
                                        <div className="flex flex-col">
                                            <span className="text-xs font-bold text-white tracking-widest uppercase">{disk.mountPoint}</span>
                                            <span className="text-[10px] text-slate-400 font-mono mt-1 font-bold">SATA-SSD / NVME</span>
                                        </div>
                                        <span className={`text-2xl font-black font-mono ${disk.usagePercentage > 85 ? 'text-brand-accent' : 'text-white'}`}>{disk.usagePercentage}%</span>
                                    </div>
                                    <div className="w-full bg-white/10 rounded-full h-2 overflow-hidden shadow-inner">
                                        <div className={`h-full rounded-full transition-all duration-1000 ease-out ${disk.usagePercentage > 85 ? 'bg-brand-accent shadow-[0_0_10px_#fb7185]' : 'bg-brand-secondary shadow-[0_0_10px_#22d3ee]'}`} 
                                             style={{ width: `${disk.usagePercentage}%` }}></div>
                                    </div>
                                    <div className="flex justify-between text-[11px] text-slate-300 mt-4 font-black font-mono uppercase tracking-tighter bg-white/5 p-2 rounded-lg">
                                        <span>USED: {((disk.usedSpaceBytes || 0) / 1024 / 1024 / 1024).toFixed(1)}GB</span>
                                        <span>TOTAL: {((disk.totalSpaceBytes || 0) / 1024 / 1024 / 1024).toFixed(1)}GB</span>
                                    </div>
                                </div>
                            ))}
                        </div>
                        {!metrics?.disks?.length && (
                            <div className="flex flex-col items-center justify-center py-12 text-slate-600">
                                <Activity className="animate-pulse mb-4 opacity-20" size={48} />
                                <p className="text-xs font-bold uppercase tracking-widest">Awaiting Disk Telemetry</p>
                            </div>
                        )}
                    </div>

                    {/* Alerts / Events Feed */}
                    <div className="glass-card rounded-4xl p-8 border-white/5">
                        <div className="flex items-center gap-3 mb-8">
                            <Activity size={18} className="text-brand-accent animate-pulse" />
                            <h3 className="text-sm font-black text-white uppercase tracking-[0.2em]">Signal Log</h3>
                        </div>
                        <div className="space-y-4 max-h-[400px] overflow-y-auto pr-2 custom-scrollbar">
                            {alerts.map((alert, idx) => (
                                <div key={idx} className="p-4 rounded-xl bg-brand-accent/10 border border-brand-accent/20 text-white text-[11px] font-black font-mono uppercase tracking-tighter animate-fade-in relative overflow-hidden group">
                                    <div className="absolute inset-y-0 left-0 w-1.5 bg-brand-accent"></div>
                                    <p className="relative z-10 leading-relaxed drop-shadow-sm">{alert}</p>
                                </div>
                            ))}
                            {!alerts.length && (
                                <div className="flex flex-col items-center justify-center py-16 opacity-40">
                                    <div className="w-16 h-1.5 border-b border-dashed border-slate-400 mb-6"></div>
                                    <p className="text-[10px] font-black uppercase tracking-[0.3em] text-slate-300 text-center leading-relaxed">System Status:<br/>Nominal</p>
                                </div>
                            )}
                        </div>
                    </div>
                </div>

            </main>
        </div>
    );
}
