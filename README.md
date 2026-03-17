# PolancoWatch 🧿

PolancoWatch is a beautiful, lightweight, self-hosted system monitoring platform for Linux servers. It provides real-time telemetry streaming and an alerting system, designed with a stunning Glassmorphism UI.

![PolancoWatch Dashboard](https://via.placeholder.com/1200x800.png?text=PolancoWatch+Dashboard)

## 🚀 Features
- **Real-Time Telemetry:** CPU, Memory, Disk, and Network usage streamed via SignalR.
- **Lightweight Backend:** Built on ASP.NET Core 8 reading directly from the Linux `/proc` filesystem.
- **Modern Dashboard:** React + Vite frontend styled with Tailwind CSS v4 featuring glassmorphism and animations.
- **Alerting Engine:** Built-in threshold evaluation and event notification system out-of-the-box.
- **Easy Deployment:** Fully containerized with Docker and Docker Compose.

## 🏗️ Tech Stack
- **Backend:** C#, .NET 8, ASP.NET Core Web API, EF Core, SQLite, SignalR
- **Frontend:** TypeScript, React, Vite, Tailwind CSS, Recharts, Lucide Icons
- **Deployment:** Docker, Nginx

## 📦 Installation (Docker)

The easiest way to deploy PolancoWatch is via Docker Compose.

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/PolancoWatch.git
   cd PolancoWatch
   ```

2. Start the stack:
   ```bash
   docker-compose up -d
   ```

3. Access the Dashboard:
   - Open your browser and navigate to `http://localhost` (or your server IP).
   - **Default Credentials:**
     - Username: `admin`
     - Password: `admin`
   > ⚠️ **Important:** Please change the default admin password immediately after your first login!

## ⚙️ Architecture Overview
- **API Container:** Runs the .NET Background Services that scrape server metrics every 2.5 seconds, evaluate alert thresholds, and broadcast telemetry via SignalR.
- **Web Container:** Serves the highly optimized Vite SPA through an Nginx alpine image.

## 🤝 Contributing
Contributions are always welcome! Feel free to open an issue or submit a Pull Request.

## 📄 License
MIT
