# Trading System Demo - Backend

<div align="center">
  <img src="https://img.shields.io/badge/version-1.0.0-green.svg" alt="Version 1.0.0">
  <img src="https://img.shields.io/badge/license-Proprietary-blue.svg" alt="License">
  <img src="https://img.shields.io/badge/.NET-9.0-purple.svg" alt=".NET 9.0">
  <img src="https://img.shields.io/badge/MongoDB-Latest-green.svg" alt="MongoDB">
</div>

## 📋 Overview

This directory contains the microservices backend for the Trading System Demo application. Built with .NET 9.0, it implements a comprehensive set of services for a complete trading platform.

## 🏗️ Architecture

The backend follows a microservice architecture pattern with:

- **Independent Services**: Each domain has its own dedicated microservice
- **Shared Libraries**: Common models and utilities shared across services
- **MongoDB Database**: NoSQL database for flexible schema and scalability
- **RESTful APIs**: Standard API patterns with JWT authentication
- **WebSocket Support**: Real-time data streaming for market data and notifications

## 🔌 Services Overview

| Service | Description | Primary Responsibility |
|---------|-------------|------------------------|
| **IdentityService** | User authentication service | User registration, login, authentication |
| **AccountService** | Account management service | Balance management, deposits, withdrawals |
| **MarketDataService** | Market data provider | Price feeds, order book data, historical data |
| **TradingService** | Trading operations | Order placement, cancellation, history |
| **RiskService** | Risk management | Risk assessment, trading limits |
| **NotificationService** | User notifications | Alerts, status updates, system messages |
| **MatchMakingService** | Order matching | Order matching engine, trade execution |
| **CommonLib** | Shared library | Models, utilities, and common services |

## 🛠️ Technologies

| Technology | Purpose |
|------------|---------|
| **.NET 9.0** | Latest .NET framework for optimal performance |
| **ASP.NET Core** | Web API framework |
| **MongoDB** | NoSQL database |
| **JWT Authentication** | Secure token-based authentication |
| **Swagger/OpenAPI** | API documentation |
| **Docker** | Containerization for deployment |
| **SignalR** | Real-time communication |

## 🚀 Development Setup

### 📋 Prerequisites

- .NET 9.0 SDK
- MongoDB (local or connection string)
- Docker (optional, for containerized development)

### 🔧 Getting Started

1. Restore dependencies:
   ```bash
   dotnet restore backend.sln
   ```

2. Build the solution:
   ```bash
   dotnet build backend.sln
   ```

3. Run individual services:
   ```bash
   # Example for running the Identity Service
   cd IdentityService
   dotnet run
   ```

4. Run with Docker:
   ```bash
   # From the backend directory
   docker-compose up -d
   ```

### 📁 Project Structure

Each service follows this standard structure:

```
ServiceName/
├── Controllers/       - API endpoint controllers
├── Services/          - Business logic implementation
├── Repositories/      - Data access layer (optional)
├── Properties/        - Service configuration
├── Program.cs         - Application entry point
├── ServiceName.csproj - Project file
└── appsettings.json   - Configuration settings
```

The `CommonLib` contains shared resources:

```
CommonLib/
├── Models/          - Domain models shared across services
│   ├── Account/     - Account domain models
│   ├── Identity/    - Identity domain models
│   ├── Market/      - Market data models
│   ├── Notification/- Notification models
│   ├── Risk/        - Risk management models
│   └── Trading/     - Trading models
├── Extensions/      - Extension methods
├── Services/        - Shared service implementations
└── Properties/      - Configuration
```

## 📚 API Documentation

Each service exposes a Swagger UI endpoint at `/swagger` when running:

| Service | Swagger URL |
|---------|-------------|
| IdentityService | http://localhost:5001/swagger |
| AccountService | http://localhost:5002/swagger |
| MarketDataService | http://localhost:5003/swagger |
| TradingService | http://localhost:5004/swagger |
| RiskService | http://localhost:5005/swagger |
| NotificationService | http://localhost:5006/swagger |

## 📏 Development Standards

This project follows a set of development standards:

| Standard | Description |
|----------|-------------|
| **RESTful API Design** | Consistent endpoint patterns |
| **Model-First Approach** | Shared models in CommonLib |
| **Repository Pattern** | Data access abstraction |
| **Dependency Injection** | Service registration and configuration |
| **JWT Authentication** | Secure identity management |
| **Exception Handling** | Consistent error responses |
| **Logging** | Structured logging throughout services |

## 🧪 Testing

Run unit and integration tests:

```bash
dotnet test backend.sln
```

The `SimulationTest` project provides simulation testing for trading scenarios:

```bash
cd SimulationTest
dotnet run
```

## 🚢 Deployment

### 🐳 Docker Deployment

Individual service Dockerfiles are available in each service directory.

Build and run with Docker Compose:

```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### ☸️ Kubernetes Deployment

Kubernetes configuration files are available in the `scripts/docker/helm/trading-system` directory.

## 📜 License

Copyright © 2024-2025 EggyByte Technology. All rights reserved.

This project is proprietary software. No part of this project may be copied, modified, or distributed without the express written permission of EggyByte Technology.

---

<div align="center">
  <p>Developed by EggyByte Technology • 2024-2025</p>
</div> 