import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5246';

const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
}, (error) => {
  return Promise.reject(error);
});

export const authService = {
  login: async (username: string, password: string) => {
    const response = await api.post('/api/auth/login', { username, password });
    if (response.data.token) {
      localStorage.setItem('token', response.data.token);
    }
    return response.data;
  },
  logout: () => {
    localStorage.removeItem('token');
  },
  isAuthenticated: () => {
    return !!localStorage.getItem('token');
  },
  updateProfile: async (currentPassword: string, newUsername?: string, newPassword?: string) => {
    const response = await api.post('/api/auth/profile', { currentPassword, newUsername, newPassword });
    if (response.data.token) {
      localStorage.setItem('token', response.data.token);
    }
    if (response.data.username) {
        localStorage.setItem('username', response.data.username);
    }
    return response.data;
  }
};

export default api;
