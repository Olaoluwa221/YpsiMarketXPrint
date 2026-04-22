# YpsiMarketXPrint

Ecommerce platform for Ypsi Marketing & Print Company — a local print and marketing business in Ypsilanti, Michigan.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 19 + Vite + Tailwind CSS |
| Backend | ASP.NET Core 10 Web API |
| Database | MySQL 8.0 |
| ORM | Entity Framework Core 9 |
| File Storage | Azure Blob Storage (Azurite locally) |
| Email | Resend |
| Payments | Stripe |
| Containers | Docker + Docker Compose |

---

## Prerequisites

Make sure the following are installed on your machine:

- [Git](https://git-scm.com/download/win)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) — enable WSL2 backend on Windows
- [Node.js LTS](https://nodejs.org)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio 2026](https://visualstudio.microsoft.com) with the **ASP.NET and web development** workload

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Olaoluwa221/YpsiMarketXPrint.git
cd YpsiMarketXPrint
```

### 2. Set up backend config

Duplicate the example files, rename them and fill in your credentials:

docker-compose.example.yml -> docker-compose.yml
YpsiMarketXPrint.API/appsettings.example.json -> YpsiMarketXPrint.API/appsettings.json

See the **Environment Variables** section below for details on each value.

### 3. Set up frontend config

Duplicate the example file, rename it and fill in your credentials:

ypsimarketxprint-client/.env.example -> ypsimarketxprint-client/.env

### 4. Start the backend (Docker)

From the repo root and in a powershell terminal:

docker compose up --build

This will:
- Pull and start MySQL 8.0 on port `3307`
- Pull and start Azurite (Azure Blob Storage emulator) on port `10000`
- Build and start the ASP.NET Core API on port `8080`
- Automatically run all EF Core migrations
- Seed an admin user using the credentials in your `docker-compose.yml`

### 5. Start the frontend

In a separate cmd terminal:

cd ypsimarketxprint-client
npm install
npm run dev

The frontend will be available at `http://localhost:5173`.

## Environment Variables

### `docker-compose.yml` — API environment

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | MySQL connection string |
| `Jwt__Key` | JWT signing secret — minimum 32 characters |
| `Jwt__Issuer` | JWT issuer — set to `YpsiMarketXPrint` |
| `Jwt__Audience` | JWT audience — set to `YpsiMarketXPrint` |
| `Seed__AdminEmail` | Email address for the seeded admin account |
| `Seed__AdminPassword` | Password for the seeded admin account |
| `Azure__StorageConnectionString` | Azurite connection string (pre-filled in example) |
| `Azure__ContainerName` | Blob container name — set to `product-images` |
| `Resend__ApiKey` | Resend API key — starts with `re_` |
| `Resend__FromEmail` | Sender email address — use `onboarding@resend.dev` for dev |
| `Resend__ReplyTo` | Reply-to email address |
| `Stripe__PublishableKey` | Stripe publishable key — starts with `pk_test_` |
| `Stripe__SecretKey` | Stripe secret key — starts with `sk_test_` |

### `appsettings.json` — local dev (Visual Studio)

Same values as above but formatted as JSON. Used when running the API directly in Visual Studio outside of Docker. The `Azure.StorageConnectionString` should point to `localhost:10000` instead of `azurite:10000`.

### `ypsimarketxprint-client/.env` — frontend

| Variable | Description |
|----------|-------------|
| `VITE_STRIPE_PUBLISHABLE_KEY` | Stripe publishable key — starts with `pk_test_` |

---

## Project Structure

```
YpsiMarketXPrint/
├── YpsiMarketXPrint.API/          # ASP.NET Core Web API
│   ├── Controllers/               # API endpoints
│   ├── Data/                      # AppDbContext
│   ├── DTOs/                      # Data transfer objects
│   ├── Models/                    # EF Core models
│   ├── Migrations/                # EF Core migrations
│   ├── Services/                  # EmailService
│   ├── Program.cs                 # App startup, DI, middleware
│   ├── appsettings.json           # Local config (gitignored)
│   ├── appsettings.example.json   # Config template
│   └── Dockerfile
├── ypsimarketxprint-client/       # React frontend
│   ├── src/
│   │   ├── api/                   # Axios instance
│   │   ├── components/            # Shared components
│   │   ├── context/               # Auth and Toast context
│   │   └── pages/                 # All page components
│   │       └── admin/             # Admin-only pages
│   ├── .env                       # Frontend env (gitignored)
│   └── .env.example               # Frontend env template
├── docker-compose.yml             # Local Docker config (gitignored)
├── docker-compose.example.yml     # Docker config template
└── README.md
```

---

## User Roles

| Role | Access |
|------|--------|
| `customer` | Browse products, manage cart, checkout, view own orders |
| `admin` | All customer access + manage products, orders, images, send emails |
| `staff` | View and update order status |

The first admin account is seeded automatically from `Seed__AdminEmail` and `Seed__AdminPassword` in your config.

---

## Key Features

- **Product catalog** with dynamic categories, variants (size + price), and image gallery
- **Cart** — persistent for logged-in users, localStorage for guests
- **Guest checkout** — no account required, order confirmed via email
- **Stripe payments** — card payments via Stripe Elements
- **Order management** — customers see order history, admin updates status with email notifications
- **Image uploads** — product photos stored in Azure Blob Storage
- **Marketing emails** — customers opt in at registration, admin sends campaigns from the portal
- **Mobile responsive** — sticky navbar with hamburger menu

---

## Useful Commands

### Docker

```bash
# Start all services
docker compose up --build

# Stop all services (keep data)
docker compose down

# Stop all services and wipe database
docker compose down -v

# View API logs
docker logs ypsiprint-api -f
```

### EF Core Migrations

Run these in Visual Studio **Package Manager Console** with the API project selected:

```powershell
# Create a new migration
Add-Migration MigrationName

# Apply migrations manually (Docker does this automatically on startup)
Update-Database
```

### Frontend

```bash
# Install dependencies
npm install

# Start dev server
npm run dev

# Build for production
npm run build
```

---

## Stripe Test Cards

Use these in test mode:

| Scenario | Card Number |
|----------|------------|
| Payment succeeds | `4242 4242 4242 4242` |
| Payment declined | `4000 0000 0000 0002` |
| Requires authentication | `4000 0025 0000 3155` |

Use any future expiry date, any 3-digit CVC, and any 5-digit ZIP.

---

## External Services

| Service | Purpose | Dashboard |
|---------|---------|-----------|
| Stripe | Payment processing | https://dashboard.stripe.com |
| Resend | Email delivery | https://resend.com |
| Azure Blob Storage | Image storage | https://portal.azure.com |

---

## Notes

- `appsettings.json`, `docker-compose.yml`, and `.env` are gitignored — never commit secrets
- Azurite uses hardcoded dev credentials — safe for local development only
- Resend currently uses `onboarding@resend.dev` as the sender — the client will need to verify their domain for production
- All EF Core migrations are applied automatically on API startup — no manual database setup required
