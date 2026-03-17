import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/api';

export default function Login() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        try {
            await authService.login(username, password);
            navigate('/');
        } catch (err: any) {
            setError(err.response?.data?.message || 'Invalid credentials');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-[#0b0f19] p-4 text-slate-900 dark:text-slate-100">
            <div className="glass-panel w-full max-w-md p-8 rounded-2xl animate-fade-in shadow-2xl relative overflow-hidden">
                <div className="absolute top-0 left-0 w-full h-1 bg-linear-to-r from-brand-400 to-brand-600"></div>
                
                <div className="flex flex-col items-center mb-8">
                    <div className="w-16 h-16 rounded-xl bg-linear-to-br from-brand-400 to-brand-600 flex items-center justify-center text-white font-bold text-3xl shadow-lg shadow-brand-500/30 mb-4">
                        P
                    </div>
                    <h1 className="text-2xl font-bold tracking-tight">PolancoWatch</h1>
                    <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">Sign in to your dashboard</p>
                </div>

                <form onSubmit={handleSubmit} className="space-y-5">
                    {error && (
                        <div className="p-3 rounded-lg bg-red-50 dark:bg-red-500/10 border border-red-200 dark:border-red-500/20 text-red-600 dark:text-red-400 text-sm text-center font-medium">
                            {error}
                        </div>
                    )}
                    
                    <div>
                        <label className="block text-sm font-medium mb-1.5 text-slate-700 dark:text-slate-300">Username</label>
                        <input
                            type="text"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            className="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-white/10 bg-white dark:bg-dark-900/50 focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500 outline-none transition-all dark:text-white"
                            placeholder="admin"
                            required
                        />
                    </div>
                    
                    <div>
                        <label className="block text-sm font-medium mb-1.5 text-slate-700 dark:text-slate-300">Password</label>
                        <input
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-white/10 bg-white dark:bg-dark-900/50 focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500 outline-none transition-all dark:text-white"
                            placeholder="••••••••"
                            required
                        />
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        className="w-full py-2.5 px-4 rounded-xl bg-linear-to-r from-brand-500 to-brand-600 hover:from-brand-600 hover:to-brand-700 text-white font-semibold shadow-lg shadow-brand-500/30 transition-all disabled:opacity-70 disabled:cursor-not-allowed transform hover:-translate-y-0.5"
                    >
                        {loading ? 'Authenticating...' : 'Sign In'}
                    </button>
                </form>
            </div>
        </div>
    );
}
