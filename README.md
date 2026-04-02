# Employee Pair Analyzer

Upload a CSV file of employee project assignments and find the pair of employees who worked together on common projects for the longest cumulative period. Results are displayed in a data grid broken down per project.

## Tech stack

- **Backend** — ASP.NET Core Web API (.NET 8)
- **Frontend** — React + TypeScript + Vite

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org)

## One-time setup — trust the .NET dev certificate

The backend runs on HTTPS using the .NET development certificate. Trust it once so the API is reachable without errors:

```bash
dotnet dev-certs https --trust
```

> On Windows and macOS this opens a system prompt asking you to confirm. On Linux, follow the [additional steps](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl#trust-the-aspnet-core-https-development-certificate-on-linux) for your distro.

The frontend uses [`vite-plugin-mkcert`](https://github.com/liuweiGL/vite-plugin-mkcert), which automatically creates a local certificate authority, installs it in the OS and browser trust stores, and issues a `localhost` certificate — **no browser warnings, no manual steps**.

## Running the app

The quickest way to start both services at once:

```bash
# Windows
start.bat

# macOS / Linux / Git Bash
bash start.sh
```

Both scripts install dependencies on first run, then start:
- Backend on `https://localhost:7001` (HTTP on `:5000` redirects to HTTPS)
- Frontend on `https://localhost:5173`

The Vite dev server proxies all `/api` requests from the browser to the backend over HTTPS.

### Running services individually

**Backend**
```bash
cd backend/EmployeesApi
dotnet run
```
Swagger UI is available at `https://localhost:7001/swagger`.

**Frontend**
```bash
cd frontend
npm install   # first time only
npm run dev
```
Opens on `https://localhost:5173`. On first run, `vite-plugin-mkcert` will install the local CA and issue the certificate automatically.

## Running the tests

```bash
cd backend
dotnet test EmployeesApi.sln
```

## CSV format

```
EmpID, ProjectID, DateFrom, DateTo
143, 12, 2013-11-01, 2014-01-05
218, 10, 2012-05-16, NULL
```

- `DateTo` accepts `NULL` (treated as today).
- Dates are accepted in most common formats: ISO (`yyyy-MM-dd`), European (`dd/MM/yyyy`), US (`MM/dd/yyyy`), compact (`yyyyMMdd`), long month names, and more.
- A header row is optional and is skipped automatically.
