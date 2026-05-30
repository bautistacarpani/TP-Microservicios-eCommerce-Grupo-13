# TP Microservicios eCommerce — Grupo 13

Sistema de E-Commerce basado en arquitectura de microservicios, implementado en C# con .NET Core 8.

## Integrantes

| Integrante | Servicios |
|---|---|
| Dani | Products.API, Cart.API |
| Bauti | Orders.API |
| Pau | Users.API, Notifications.API |

---

## Requisitos

- .NET 8 SDK — [Descargar](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

---

## Cómo ejecutar el proyecto

### 1. Clonar el repositorio

```bash
git clone https://github.com/bautistacarpani/TP-Microservicios-eCommerce-Grupo-13.git
cd TP-Microservicios-eCommerce-Grupo-13
```

### 2. Levantar cada servicio

Cada servicio se levanta por separado en su propia terminal.

> **Importante:** Products.API debe estar corriendo antes que Cart.API y Orders.API.

**Products.API**
```bash
cd EcommerceMicroservicios/Products.API
dotnet run
```
Swagger: http://localhost:5046/swagger — Health UI: http://localhost:5046/health-ui

**Cart.API**
```bash
cd EcommerceMicroservicios/Cart.API
dotnet run
```
Swagger: http://localhost:5252/swagger — Health UI: http://localhost:5252/health-ui

**Users.API**
```bash
cd EcommerceMicroservicios/Users.API
dotnet run
```

**Orders.API**
```bash
cd EcommerceMicroservicios/Orders.API
dotnet run
```

**Notifications.API**
```bash
cd EcommerceMicroservicios/Notifications.API
dotnet run
```

---
