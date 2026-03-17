import { useNavigate } from 'react-router-dom';
import { ChevronLeft, Cpu, Activity, HardDrive, Network, Terminal, Settings, Info } from 'lucide-react';

export default function Documentation() {
    const navigate = useNavigate();

    return (
        <div className="min-h-screen bg-obsidian-950 text-slate-300 font-sans selection:bg-brand-primary/30 flex-1 pl-20 lg:pl-72 transition-all duration-500">
            {/* Background Texture Overlay */}
            <div className="fixed inset-0 pointer-events-none opacity-[0.03] z-0" 
                 style={{ backgroundImage: `url("data:image/svg+xml,%3Csvg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='noiseFilter'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.65' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23noiseFilter)'/%3E%3C/svg%3E")` }}>
            </div>

            <main className="max-w-5xl mx-auto px-6 lg:px-8 py-16 relative z-10">
                <header className="mb-16">
                    <div className="flex items-center justify-between mb-8">
                        <button 
                            onClick={() => navigate('/')}
                            className="flex items-center gap-2 text-slate-400 hover:text-white transition-colors group"
                        >
                            <ChevronLeft size={20} className="group-hover:-translate-x-1 transition-transform" />
                            <span className="text-xs font-black uppercase tracking-widest">Console</span>
                        </button>
                        <div className="flex items-center gap-3 opacity-50">
                            <Terminal size={14} className="text-brand-primary" />
                            <span className="text-[10px] font-black text-white uppercase tracking-[0.3em]">REF_DOCS_v1</span>
                        </div>
                    </div>

                    <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-brand-primary/10 border border-brand-primary/20 text-brand-primary text-[10px] font-black uppercase tracking-widest mb-6">
                        <Info size={12} /> System Internals
                    </div>
                    <h1 className="text-5xl font-black text-white tracking-tighter mb-4">Metric Calculation <br/><span className="text-brand-secondary">Methodology</span></h1>
                    <p className="text-lg text-slate-400 max-w-2xl leading-relaxed">
                        PolancoWatch utilizes low-level OS interfaces to extract high-precision telemetry with minimal overhead. This page details the mathematical models and system calls used for each metric.
                    </p>
                </header>


                <div className="space-y-12">
                    {/* CPU Section */}
                    <section className="glass-panel rounded-4xl p-10 border-white/5">
                        <div className="flex items-start gap-6 mb-8">
                            <div className="w-14 h-14 rounded-2xl bg-brand-primary/10 border border-brand-primary/20 flex items-center justify-center text-brand-primary">
                                <Cpu size={28} />
                            </div>
                            <div>
                                <h2 className="text-2xl font-black text-white tracking-tight uppercase">Central Processing Unit</h2>
                                <p className="text-slate-400 text-sm mt-1">Processor Load & Utilization Analysis</p>
                            </div>
                        </div>
                        
                        <div className="grid md:grid-cols-2 gap-12">
                            <div>
                                <h3 className="text-xs font-black text-brand-secondary uppercase tracking-widest mb-4">Linux Implementation</h3>
                                <p className="text-sm leading-relaxed mb-4">
                                    Metrics are derived from <code className="text-brand-primary font-mono bg-white/5 px-1.5 py-0.5 rounded">/proc/stat</code>. The system calculates the aggregate time spent in various states (user, nice, system, idle, etc.).
                                </p>
                                <div className="bg-obsidian-950 rounded-xl p-4 border border-white/5 font-mono text-[11px] space-y-2">
                                    <div className="text-slate-500">// Calculation Logic</div>
                                    <div className="text-white">deltaTotal = currentTotal - previousTotal</div>
                                    <div className="text-white">deltaIdle = currentIdle - previousIdle</div>
                                    <div className="text-brand-secondary">Usage% = (1.0 - (deltaIdle / deltaTotal)) * 100</div>
                                </div>
                            </div>
                            <div>
                                <h3 className="text-xs font-black text-brand-secondary uppercase tracking-widest mb-4">Windows Implementation</h3>
                                <p className="text-sm leading-relaxed mb-4">
                                    Utilizes the <code className="text-brand-primary font-mono bg-white/5 px-1.5 py-0.5 rounded">PerformanceCounter</code> API specifically targeting the <code className="text-slate-300">Processor / % Processor Time / _Total</code> instance.
                                </p>
                                <div className="bg-obsidian-950 rounded-xl p-4 border border-white/5 font-mono text-[11px]">
                                    <div className="text-slate-500">// Native Windows API</div>
                                    <div className="text-white">NextValue() query against Registry/PerfData</div>
                                </div>
                            </div>
                        </div>
                    </section>

                    {/* Memory Section */}
                    <section className="glass-panel rounded-4xl p-10 border-white/5">
                        <div className="flex items-start gap-6 mb-8">
                            <div className="w-14 h-14 rounded-2xl bg-brand-secondary/10 border border-brand-secondary/20 flex items-center justify-center text-brand-secondary">
                                <Activity size={28} />
                            </div>
                            <div>
                                <h2 className="text-2xl font-black text-white tracking-tight uppercase">Random Access Memory</h2>
                                <p className="text-slate-400 text-sm mt-1">Volatile Storage & Page Cache Tracking</p>
                            </div>
                        </div>
                        
                        <div className="grid md:grid-cols-2 gap-12">
                            <div>
                                <h3 className="text-xs font-black text-brand-primary uppercase tracking-widest mb-4">Linux Implementation</h3>
                                <p className="text-sm leading-relaxed mb-4">
                                    Interprets <code className="text-brand-secondary font-mono bg-white/5 px-1.5 py-0.5 rounded">/proc/meminfo</code>. "Used" memory is defined as Total minus Available (which includes reclaimable buffers/cache).
                                </p>
                                <div className="bg-obsidian-950 rounded-xl p-4 border border-white/5 font-mono text-[11px]">
                                    <div className="text-slate-500">// Strategy</div>
                                    <div className="text-white">Total = MemTotal</div>
                                    <div className="text-white">Free = MemAvailable</div>
                                    <div className="text-brand-primary">Used = Total - Free</div>
                                </div>
                            </div>
                            <div>
                                <h3 className="text-xs font-black text-brand-primary uppercase tracking-widest mb-4">Windows Implementation</h3>
                                <p className="text-sm leading-relaxed mb-4">
                                    Queries <code className="text-brand-secondary font-mono bg-white/5 px-1.5 py-0.5 rounded">WMI (Win32_ComputerSystem)</code> for physical hardware limits and uses Performance Counters for real-time availability.
                                </p>
                                <div className="bg-obsidian-950 rounded-xl p-4 border border-white/5 font-mono text-[11px]">
                                    <div className="text-slate-500">// Strategy</div>
                                    <div className="text-white">Query TotalPhysicalMemory</div>
                                    <div className="text-brand-primary">Used = Total - AvailableBytes</div>
                                </div>
                            </div>
                        </div>
                    </section>

                    {/* Networking & Disk Section */}
                    <div className="grid md:grid-cols-2 gap-8">
                        <section className="glass-panel rounded-4xl p-10 border-white/5">
                            <div className="flex items-center gap-4 mb-6">
                                <Network className="text-brand-secondary" size={24} />
                                <h2 className="text-xl font-black text-white tracking-tight uppercase">Networking</h2>
                            </div>
                            <p className="text-sm text-slate-400 leading-relaxed mb-6">
                                Calculated by polling interface byte counters.
                            </p>
                            <ul className="space-y-4">
                                <li className="flex gap-3">
                                    <div className="w-1.5 h-1.5 rounded-full bg-brand-secondary mt-1.5"></div>
                                    <div className="text-sm">
                                        <span className="text-white font-bold">Linux:</span> Delta analysis of <code className="text-brand-secondary font-mono text-[10px] bg-white/5 px-1 rounded">/proc/net/dev</code> byte columns.
                                    </div>
                                </li>
                                <li className="flex gap-3">
                                    <div className="w-1.5 h-1.5 rounded-full bg-brand-secondary mt-1.5"></div>
                                    <div className="text-sm">
                                        <span className="text-white font-bold">Windows:</span> <code className="text-brand-secondary font-mono text-[10px] bg-white/5 px-1 rounded"> ni.GetIPStatistics()</code> native interface polling.
                                    </div>
                                </li>
                            </ul>
                        </section>

                        <section className="glass-panel rounded-4xl p-10 border-white/5">
                            <div className="flex items-center gap-4 mb-6">
                                <HardDrive className="text-brand-primary" size={24} />
                                <h2 className="text-xl font-black text-white tracking-tight uppercase">Storage</h2>
                            </div>
                            <p className="text-sm text-slate-400 leading-relaxed mb-6">
                                Cross-platform implementation using high-level drive enumerations.
                            </p>
                            <ul className="space-y-4">
                                <li className="flex gap-3">
                                    <div className="w-1.5 h-1.5 rounded-full bg-brand-primary mt-1.5"></div>
                                    <div className="text-sm">
                                        <span className="text-white font-bold">Driver:</span> <code className="text-brand-primary font-mono text-[10px] bg-white/5 px-1 rounded">DriveInfo.GetDrives()</code>
                                    </div>
                                </li>
                                <li className="flex gap-3">
                                    <div className="w-1.5 h-1.5 rounded-full bg-brand-primary mt-1.5"></div>
                                    <div className="text-sm">
                                        <span className="text-white font-bold">Filter:</span> Excludes loopbacks, snapping, and system-level virtual mounts.
                                    </div>
                                </li>
                            </ul>
                        </section>
                    </div>

                    {/* Infrastructure Footer */}
                    <footer className="pt-16 border-t border-white/5 text-center">
                        <div className="inline-flex items-center gap-3 text-slate-500 text-[10px] font-black uppercase tracking-[0.4em]">
                            <Settings size={14} className="animate-spin-slow" /> PolancoWatch Core v1.3.4 Build Final
                        </div>
                    </footer>
                </div>
            </main>
        </div>
    );
}
