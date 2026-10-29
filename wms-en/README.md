# WMS Warehouse Management System

A factory warehouse management system (WMS) with a decoupled frontend and backend, modular layered architecture, and ready for secondary development. The backend is built on .NET 8 WebAPI with a five-module layered architecture (`core / app / im / plugin / webapi`); the frontend is built on Vue 2.x + Element UI + Vuex + Vue Router, providing a responsive single-page admin interface.

The system covers the core business loop of a factory WMS: master data → warehouse configuration → inbound → outbound → stocktake → transfer → inventory ledger & transactions → reports → system permission management.

> The repository ships with a SQLite demo database and seed data. Everything works out of the box on first launch — no additional database installation required.

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Local Setup](#local-setup)
- [Usage Guide](#usage-guide)
- [API & Permissions](#api--permissions)
- [Plugin Development](#plugin-development)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [License](#license)

## Features

| Module | Capabilities |
| --- | --- |
| Master Data | Material categories (two levels), material master data (code/spec/unit/safety stock/reference price), supplier and customer profiles |
| Warehouse Config | Multi-warehouse management, location management (zone, location type, capacity), warehouse-location cascading |
| Inbound | Purchase/production/return/other inbound; Draft → Confirm → Execute; execution increases stock and writes inventory transactions |
| Outbound | Sales/production picking/other outbound; confirmation locks stock, execution deducts stock, cancellation automatically releases the lock |
| Stocktake | Generates book snapshots per warehouse, records counted quantities, and adjusts stock by variance on completion |
| Transfer | Cross-warehouse transfer with source deduction + destination increase in one transaction, producing two transaction records |
| Inventory | Stock ledger (on-hand/locked/available), inventory transactions (8 types), warehouse summary, below-safety-stock alerts |
| Reports | Dashboard overview, inbound/outbound trends, stock value by category, stock summary report, in/out report, top outbound materials |
| System & Permissions | Users, roles, menus, API permissions (RBAC + dynamic permission policies), operation logs, plugin management |
| Plugin System | Built-in inventory alert, Webhook notification, and ERP sync plugins; supports reflection-based loading of external DLL plugins |

Additional highlights:

- JWT authentication + dynamic permission policies, with dual menu- and button-level permission checks on both frontend and backend
- Unified response structure, unified exception handling, unified 401/403 output, and unified model validation messages
- Global soft delete, automatic audit fields, automatic operation logging
- Responsive layout for desktop and tablet; ECharts-powered dashboard

## Tech Stack

### Backend

| Component | Version | Notes |
| --- | --- | --- |
| .NET | 8.0 | ASP.NET Core WebAPI |
| EF Core | 8.0.8 | ORM with the SQLite provider |
| SQLite | - | Zero-config demo database; easily swapped for SQL Server / PostgreSQL / MySQL |
| JWT | 8.0.x | `System.IdentityModel.Tokens.Jwt` + `JwtBearer` |
| Swagger | 6.6.2 | Swashbuckle online API docs |
| PBKDF2-SHA256 | - | Password hashing with 100k iterations, built in with no extra dependencies |

### Frontend

| Component | Version | Notes |
| --- | --- | --- |
| Vue | 2.7.16 | Progressive framework (final release of the Vue 2 line) |
| Element UI | 2.15.14 | Desktop component library |
| Vuex | 3.6.2 | Centralized auth state, permissions, and menus |
| Vue Router | 3.6.5 | Hash routing + permission guards |
| Axios | 1.6.x | Unified interceptors and error handling |
| ECharts | 5.5.x | Dashboard and report charts |
| Vue CLI | 5.0.8 | Build tooling and dev server |

## Architecture

```
┌───────────────────────── Frontend SPA (Vue 2 + Element UI) ──────────────────────────┐
│  Sign In → Dashboard → Master Data / Warehouse / Inbound / Outbound / Inventory /    │
│  Stocktake / Transfer / Reports / System                                             │
└───────────────────────────────────────┬──────────────────────────────────────────────┘
                                        │  HTTP / JSON (JWT Bearer)
┌───────────────────────────────────────▼──────────────────────────────────────────────┐
│ Wms.WebApi  Controllers · Middleware · Auth policies · Swagger · Unified response/   │
│             exception/logging                                                        │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ Wms.App     Business services · DTOs · EF Core persistence · RBAC · Seed data ·      │
│             Report aggregation                                                       │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ Wms.Im      Inventory domain model · Inventory engine (reserve/lock/ship/adjust) ·   │
│             Inventory transactions                                                   │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ Wms.Plugin  Plugin contract · Plugin manager · Built-in plugins · External DLL       │
│             dynamic loading                                                          │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ Wms.Core    Shared entities · Enums · Unified result · Exceptions · JWT/password ·   │
│             Order numbers · Permission codes                                         │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

**Module dependency direction (one-way, no cycles):**

```
WebApi ──> App ──> Im ──> Core
   │        └──> Plugin ──> Core
   └──> Im / Plugin / Core
```

`Wms.Im` only defines the inventory domain model and the `IInventoryRepository` interface; the EF Core implementation lives in `Wms.App` and is injected, keeping inventory logic cohesive and the dependency direction clean.

## Project Structure

```
wms/
├── backend/
│   ├── Wms.sln
│   └── src/
│       ├── Wms.Core/                  # Core foundation layer
│       │   ├── Common/                # Entity base classes, enums, unified response, exceptions, permission codes
│       │   ├── Entities/              # Shared master data entities (materials/suppliers/warehouses/locations, etc.)
│       │   ├── Security/              # JWT issuing, PBKDF2 password hashing
│       │   └── Utils/                 # Order number generation, date ranges
│       ├── Wms.Im/                    # Inventory core domain
│       │   ├── Entities/              # Stock ledger, inventory transactions
│       │   ├── InventoryManager.cs    # Inventory engine (increase/lock/release/ship/transfer/adjust)
│       │   └── IInventoryRepository.cs
│       ├── Wms.Plugin/                # Plugin system
│       │   ├── BuiltIn/               # Inventory alert, Webhook notify, ERP sync
│       │   ├── PluginManager.cs       # Built-in registration + external DLL reflection loading
│       │   └── IWmsPlugin.cs
│       ├── Wms.App/                   # Application business layer
│       │   ├── Data/                  # DbContext, repository implementations, seed data
│       │   ├── Dtos/                  # Request/response models
│       │   ├── Entities/              # Users/roles/permissions/menus, business documents
│       │   └── Services/              # Auth, master data, warehouse, orders, inventory, reports, plugins
│       └── Wms.WebApi/                # API host
│           ├── Authorization/         # Dynamic permission policies and handler
│           ├── Controllers/           # All REST controllers
│           ├── Middleware/            # Exception handling, operation logging
│           ├── Infrastructure/        # Current signed-in user
│           └── Program.cs
├── frontend/
│   ├── public/
│   ├── src/
│   │   ├── api/                       # API wrappers organized by module
│   │   ├── assets/                    # Global styles
│   │   ├── layout/                    # Layout with dynamic sidebar
│   │   ├── router/                    # Static routes + permission guard
│   │   ├── store/                     # Vuex (user/app)
│   │   └── views/                     # Business pages
│   ├── package.json
│   └── vue.config.js
├── scripts/                           # Quick-start scripts for local development
├── .gitignore
├── LICENSE
└── README.md
```

## Local Setup

### 1. Prerequisites

| Dependency | Minimum Version | Check Command |
| --- | --- | --- |
| .NET SDK | 8.0 | `dotnet --version` |
| Node.js | 18.x (20 LTS+ recommended) | `node -v` |
| npm | 9.x | `npm -v` |
| Git | 2.x | `git --version` |

No database installation needed: the backend automatically creates `wms.db` and seeds demo data on first launch.

The database file is generated under `backend/src/Wms.WebApi/` (the application working directory) by default.

### 2. One-Command Startup (Windows PowerShell)

```powershell
# Run from the repository root; opens separate backend and frontend windows
powershell -ExecutionPolicy Bypass -File scripts/start-all.ps1
```

Or start them separately:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start-backend.ps1
powershell -ExecutionPolicy Bypass -File scripts/start-frontend.ps1
```

macOS / Linux:

```bash
chmod +x scripts/*.sh
./scripts/start-backend.sh    # terminal 1
./scripts/start-frontend.sh   # terminal 2
```

### 3. Start the Backend Manually

```bash
cd backend
dotnet restore
dotnet run --project src/Wms.WebApi/Wms.WebApi.csproj
```

Once started:

- API base URL: <http://localhost:5080>
- Swagger docs: <http://localhost:5080/swagger>
- Health check: <http://localhost:5080/api/health> (no sign-in required)

### 4. Start the Frontend Manually

```bash
cd frontend
npm install          # first time only
npm run serve
```

Open <http://localhost:8080> in your browser. The dev server proxies `/api` to `http://localhost:5080`, so no extra CORS setup is needed.

### 5. Production Build

```bash
cd frontend
npm run build         # output goes to frontend/dist
```

Host `dist` with a static server such as Nginx and reverse-proxy `/api` to the backend. The backend can also be published with `dotnet publish -c Release`.

## Usage Guide

### Demo Accounts

| Account | Password | Role | Permission Scope |
| --- | --- | --- | --- |
| `admin` | `admin123` | System Administrator | All permissions (including system management) |
| `keeper` | `keeper123` | Warehouse Keeper | Master data, warehouses, inbound/outbound/stocktake/transfer execution, inventory and reports |
| `viewer` | `viewer123` | Read-only Viewer | Query-only permissions |

> After your first sign-in, change the password via "Change Password" in the top-right corner. For production, be sure to update `Jwt.Secret` in `appsettings.json`.

### Typical Business Flow

1. **Maintain master data**: material categories → materials (set safety stock) → suppliers/customers.
2. **Configure warehouses**: add a warehouse → add locations for the warehouse.
3. **Inbound**: create an inbound order (draft) → confirm → execute (stock increases and an inbound transaction is generated).
4. **Outbound**: create an outbound order (draft) → confirm (locks stock) → execute (deducts stock); inventory and transactions stay in sync.
5. **Transfer**: choose source/destination warehouses and locations → execute (source deducted, destination increased).
6. **Stocktake**: pick a warehouse to generate a stocktake order → enter counted quantities → complete (stock adjusted by variance).
7. **Query & analysis**: stock ledger, inventory transactions, stock alerts, stock summary report, in/out report, dashboard trend charts.
8. **System management**: user/role authorization, menu maintenance, operation log auditing, plugin enable/disable and execution.

### Page Notes

- **Dashboard**: material count, total stock quantity/value, alert count, today's orders, 7-day trends, category distribution, recent transactions.
- **Stock Alerts**: aggregated by "material + warehouse"; shows the shortage when stock falls below safety stock.
- **Operation Logs**: all non-GET `/api` requests are automatically recorded with operator, module, path, duration, and result.

## API & Permissions

### Response Structure

All endpoints return a unified envelope:

```json
{
  "success": true,
  "code": 200,
  "message": "Sign-in successful",
  "data": {},
  "traceId": "…"
}
```

On failure, `success = false` and `code` is a business error code (4000 bad request, 4010 unauthenticated, 4030 forbidden, 4040 not found, 4090 conflict, etc.).

### Permission Model

- Permission codes look like `material:view`, `inbound:execute`, `system:user:edit`, centralized in `Wms.Core/Common/PermissionCodes.cs`.
- Controllers use the `[HasPermission("xxx")]` attribute; `PermissionPolicyProvider` builds policies dynamically and `PermissionAuthorizationHandler` enforces them.
- The administrator has the wildcard permission `*`; role permissions and menu grants are maintained on the "Roles" page.
- The frontend enforces page- and button-level control via route `meta.permission` and the `v-permission` directive.

### Main API Groups

| Group | Path Prefix |
| --- | --- |
| Authentication | `/api/auth` (login / profile / menus / change-password) |
| System | `/api/users`, `/api/roles`, `/api/menus`, `/api/logs` |
| Master Data | `/api/material-categories`, `/api/materials`, `/api/suppliers`, `/api/customers` |
| Warehouse | `/api/warehouses`, `/api/locations` |
| Inbound/Outbound | `/api/inbound-orders`, `/api/outbound-orders` |
| Stocktake/Transfer | `/api/stocktake-orders`, `/api/transfer-orders` |
| Inventory | `/api/inventory`, `/api/inventory/transactions`, `/api/inventory/summary`, `/api/inventory/alerts` |
| Reports | `/api/reports/dashboard`, `/api/reports/stock-summary`, `/api/reports/in-out-summary`, `/api/reports/trend`, `/api/reports/top-materials` |
| Plugins | `/api/plugins` |

Document status flow: `0 Draft → 1 Confirmed → 2 Completed`, or `→ 3 Cancelled`.

## Plugin Development

The plugin contract lives in `Wms.Plugin/IWmsPlugin.cs`:

```csharp
public interface IWmsPlugin
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Category { get; }
    string Version { get; }
    string Author { get; }
    Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default);
}
```

Steps:

1. Create a class library project that references `Wms.Plugin`.
2. Implement `IWmsPlugin`: read configuration via `context.Settings`, read host data via `context.Payload`, and log via `context.Logger`.
3. Build and drop the DLL into `backend/src/Wms.WebApi/Plugins/`; `PluginManager.LoadExternalPlugins()` loads it via reflection at startup.
4. Enable, configure, and run the plugin on the "Plugins" page.

Built-in plugins:

- `inventory-alert`: stock alert summary
- `webhook-notify`: pushes events to a custom Webhook URL
- `erp-sync`: syncs documents to an ERP endpoint based on field mappings

## Configuration

Backend configuration file: `backend/src/Wms.WebApi/appsettings.json`

```json
{
  "ConnectionStrings": { "Default": "Data Source=wms.db" },
  "Jwt": {
    "Secret": "change-me-in-production",
    "Issuer": "Wms",
    "Audience": "Wms.Client",
    "ExpireHours": 12
  },
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://localhost:5080" } } }
}
```

Switching databases: update the Provider configuration in `AddApplicationModule` in `Wms.App` and swap in the corresponding EF Core database package; no entity or index configuration changes are needed.

Frontend dev proxy: `devServer.proxy['/api'].target` in `frontend/vue.config.js`.

## Contributing

Issues and Pull Requests are welcome.

1. Fork this repository and create a feature branch: `feat/xxx` or `fix/xxx`.
2. Follow the existing code style: keep the five-module dependency direction on the backend — shared capabilities in `Core`, business logic in `App`, inventory rules in `Im`, extensions in `Plugin`; place frontend pages under `src/views` grouped by module.
3. New business endpoints should include XML comments, permission attributes, and DTO validation.
4. Before submitting, make sure:
   - `dotnet build backend/Wms.sln` succeeds;
   - `npm run build` (frontend) succeeds;
   - permission-related changes update the seed data and `PermissionCodes` in sync.
5. Commit messages follow Conventional Commits: `feat:`, `fix:`, `docs:`, `refactor:`, `chore:`, etc.

See [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## License

This project is open-sourced under the [MIT License](LICENSE) and may be used freely in commercial and non-commercial projects. Please retain the copyright notice.
