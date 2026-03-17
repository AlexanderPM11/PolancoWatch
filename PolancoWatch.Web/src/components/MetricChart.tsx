import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

interface DataPoint {
    timestamp: Date;
    value: number;
}

interface MetricChartProps {
    data: DataPoint[];
    color: string;
    domain?: [number, number] | ['auto', 'auto'];
    formatter?: (value: number) => string;
}

export function MetricChart({ data, color, domain = [0, 100], formatter = (val) => `${val}%` }: MetricChartProps) {
    const gradientId = `color-${color.replace('#', '')}`;

    return (
        <div className="h-full w-full">
            <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={data} margin={{ top: 10, right: 0, left: -30, bottom: 0 }}>
                    <defs>
                        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor={color} stopOpacity={0.4}/>
                            <stop offset="95%" stopColor={color} stopOpacity={0}/>
                        </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#ffffff" opacity={0.05} />
                    <XAxis 
                        dataKey="timestamp" 
                        hide={true}
                    />
                    <YAxis 
                        domain={domain} 
                        tick={{ fontSize: 9, fill: 'rgba(255,255,255,0.3)', fontFamily: 'JetBrains Mono' }}
                        tickFormatter={formatter}
                        axisLine={false}
                        tickLine={false}
                        width={30}
                        ticks={[0, 50, 100]}
                    />
                    <Tooltip
                        contentStyle={{ 
                            backgroundColor: 'rgba(10, 14, 20, 0.95)', 
                            border: '1px solid rgba(255, 255, 255, 0.1)',
                            borderRadius: '12px',
                            color: '#fff',
                            boxShadow: '0 10px 25px -5px rgba(0, 0, 0, 0.7)',
                            padding: '10px 14px',
                            fontSize: '12px',
                            fontWeight: '600',
                            fontFamily: 'JetBrains Mono, monospace',
                            backdropFilter: 'blur(10px)'
                        }}
                        itemStyle={{ color: color, padding: 0 }}
                        labelStyle={{ display: 'none' }}
                        cursor={{ stroke: 'rgba(255,255,255,0.2)', strokeWidth: 1 }}
                        formatter={(value: any) => [formatter(value), '']}
                    />
                    <Area 
                        type="monotone" 
                        dataKey="value" 
                        stroke={color} 
                        strokeWidth={2.5} 
                        fillOpacity={1} 
                        fill={`url(#${gradientId})`}
                        isAnimationActive={false}
                        activeDot={{ r: 5, fill: color, stroke: '#05070a', strokeWidth: 3 }}
                    />
                </AreaChart>
            </ResponsiveContainer>
        </div>
    );
}
