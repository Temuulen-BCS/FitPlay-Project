# FitPlay

FitPlay is a multi-tenant fitness management platform built with .NET 8. It supports independent gyms, trainers, and users within the same system, with booking workflows, role-based access, gamification, and Stripe billing including automated revenue splits.

Live deployment: `https://dependable-enthusiasm-production-a97f.up.railway.app`

---

## Features

### Authentication & Roles
- Email/password registration and login via ASP.NET Core Identity
- JWT tokens (HS256, 2-hour expiry) with embedded role and membership claims
- Roles: `User`, `Trainer`, `GymAdmin`
- `ActiveMembership` policy enforced via a custom JWT claim injected at login

### Multi-Tenant Gym Architecture
- `Gym` entities have a unique CNPJ constraint and a configurable Stripe Connect account
- `GymLocation` models physical locations under a gym
- `TrainerGymLink` tracks trainer–gym associations with an `Approved` / `Pending` / `Rejected` status — trainers cannot book rooms at a gym until their link is approved
- Commission rates and cancellation fee rates are stored per gym and used in revenue-split calculations

### Room & Booking Management
- Rooms belong to a `GymLocation` with per-room capacity, hourly price, and configurable `RoomOperatingHours` (open/close time per day of week; defaults to 08:00–22:00 if not specified)
- Overlap-safe booking creation: a DB query checks for any `Pending` or `Confirmed` booking that overlaps the requested interval before inserting
- `GetRoomAvailabilityAsync` builds a timeline of free and occupied slots for a given day
- Booking statuses: `Pending` → `Confirmed` → `Completed` / `Cancelled`

### Class Scheduling & Enrollment
- `ClassSchedule` links a room booking to a trainer and tracks enrolled users
- Free-class booking and paid-class booking are separate flows; the API returns `400` if a client tries to book a priced class without going through the Stripe payment endpoint
- `ClassSession` (session-level) and `ClassEnrollment` (per-user) track attendance and status independently
- A `BackgroundService` (`ClassStatusAutoCompleteService`) polls every minute to mark expired schedules and sessions as `Completed`; when running with a mock clock in development the same service can also *revert* completions if the clock is moved backward

### Queue System
- When a trainer creates a class but has not yet paid for the room booking, users can join a queue instead of booking directly
- Members join for free (up to 5 skips per calendar month); after the skip limit is exhausted they pay a 5 % deposit via Stripe
- Non-members always pay the 5 % deposit
- When the trainer pays for the room, `NotifyQueuedUsersAsync` marks all queue entries as notified so users can act
- Users can mark a notified entry as "skipped"; skips are counted per calendar month against the free-skip allowance

### Stripe Integration
- **Subscriptions**: `default_incomplete` payment behaviour; client secret retrieved from `Invoice.ConfirmationSecret` (Stripe.net v50 API — `PaymentIntent` was removed from `Invoice` in this version)
- **PaymentIntents for classes**: per-class payment intents with idempotent reuse of existing `requires_payment_method` / `requires_confirmation` / `requires_action` intents
- **Revenue splits**: after a session completes, `ProcessSessionSplitAsync` calculates three shares — platform (10 %), gym (configurable `CommissionRate`), trainer (remainder) — and creates two Stripe `Transfer` objects to the gym's and trainer's Connect accounts
- **Refunds**: cancelling a paid class booking triggers an 85 % refund via the Stripe `Refund` API; the 15 % retention is recorded in metadata
- **Billing webhook**: `BillingWebhookController` handles Stripe webhook events for subscription lifecycle updates

### Gamification
- `UserLevel` tracks total XP and current level; level thresholds are defined in `LevelDefinition.DefaultLevels`
- `AddXpAsync` writes an `XpTransaction` record (before/after XP, reason, optionally the awarding trainer) and recalculates the user's level
- Trainers can reset a user's XP to an arbitrary value; this also creates a `Reset`-type transaction
- `PointsCalculator` applies a difficulty multiplier (0.8 + 0.2 × difficulty) and a duration factor (clamped to [0.5, 1.5]) to a base point value
- **Training completion workflow**: completions where the training has `RequiresValidation = true` stay in `Pending` status and do not award XP until a trainer approves them; trainers can also adjust the XP awarded at validation time
- **Streak calculation**: counts consecutive distinct calendar days with approved completions, starting from today or yesterday
- **Achievements** (checked after every approved completion or validation):
  - First training, 10 / 50 / 100 trainings
  - 7-day and 30-day streaks
  - Level-up, Level 5, Level 10

### Exercise & Training Logs
- `Exercise` catalogue with `ExerciseLog` entries per user session
- `Training` entities link to exercises via `TrainingExercise` join records
- `TrainingCompletion` records link a user, a training, and the XP outcome

### Leaderboards & Rankings
- `LeaderboardsController` and `RankingsController` expose sorted user rankings

### Trainer Notifications
- `TrainerNotification` entity and `TrainerNotificationService` / `TrainerNotificationController` for in-system notifications to trainers

---

## Project Structure

```
FitPlay-Project/
├── FitPlay.Api/              # ASP.NET Core 8 REST API
│   ├── Controllers/          # 25+ controllers (auth, billing, rooms, classes, gamification, …)
│   ├── Services/             # PaymentService, MembershipService, ClassStatusAutoCompleteService
│   ├── auth/                 # ApplicationUser, ApplicationDbContext (Identity)
│   └── Program.cs            # DI registration, middleware, startup migrations
│
├── FitPlay.Domain/           # Domain layer (no ASP.NET dependency)
│   ├── Models/               # EF Core entities
│   ├── Data/                 # FitPlayContext (DbContext)
│   ├── Services/             # Domain services (XP, achievements, rooms, queue, schedules, …)
│   └── DTOs/                 # Request / response record types
│
├── FitPlay-Blazor/           # Blazor Server frontend
├── FitPlay.tests/            # xUnit test project
├── Dockerfile                # Multi-stage Docker build for FitPlay.Api
└── .github/workflows/        # CI/CD pipeline (build, test, deploy)
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8 |
| Domain | .NET 8 class library |
| Frontend | Blazor Server |
| Database | PostgreSQL via EF Core 8 |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Payments | Stripe .NET SDK v50 |
| Deployment | Railway (Docker) |
| CI/CD | GitHub Actions |

---

## Quick Start (Local)

**Prerequisites:** .NET 8 SDK, PostgreSQL

```bash
# 1. Restore
dotnet restore FitPlay-Project.sln

# 2. Configure secrets (replace with real values)
cd FitPlay.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=fitplay;Username=postgres;Password=yourpassword"
dotnet user-secrets set "Jwt:Key"       "your-256-bit-secret"
dotnet user-secrets set "Jwt:Issuer"    "fitplay"
dotnet user-secrets set "Jwt:Audience" "fitplay"
dotnet user-secrets set "Stripe:SecretKey"      "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret"  "whsec_..."
dotnet user-secrets set "Stripe:PriceId"        "price_..."

# 3. Run API (schema is created automatically via EnsureCreatedAsync on startup)
dotnet run --project FitPlay.Api

# 4. Run Blazor (separate terminal)
dotnet run --project FitPlay-Blazor
```

Swagger UI is available at `https://localhost:<port>/swagger` in Development mode.

---

## Environment Variables (Railway / Production)

| Variable | Description |
|---|---|
| `DATABASE_URL` | PostgreSQL connection URI (Railway format: `postgres://user:pass@host:port/db`) |
| `Jwt__Key` | JWT signing secret (≥ 32 characters) |
| `Jwt__Issuer` | JWT issuer string |
| `Jwt__Audience` | JWT audience string |
| `Stripe__SecretKey` | Stripe server-side secret key |
| `Stripe__PublishableKey` | Stripe publishable key |
| `Stripe__WebhookSecret` | Stripe webhook signing secret |
| `Stripe__PriceId` | Stripe Price ID for membership subscriptions |

The API parses `DATABASE_URL` automatically on startup and converts it to an EF Core connection string.

---

## CI/CD Pipeline (`.github/workflows/main.yml`)

| Job | Trigger | What it does |
|---|---|---|
| `branch-check` | Every push | Rejects branches not matching `master`, `feature/*`, `fix/*`, `hotfix/*`, `release/*` |
| `build-and-test` | Push / non-draft PR | Restores, builds all projects, runs xUnit tests, uploads coverage report |
| `docker-build` | PR only | Builds both Dockerfiles to verify they compile cleanly |
| `code-review` | PR only | Applies path-based labels, adds PR size label, posts build status comment |
| `deploy` | Push to `master` | Deploys API and Blazor services to Railway via `railway up` |

---

## Key Design Decisions

- **Two DbContexts**: `FitPlayContext` holds all domain data; `ApplicationDbContext` holds ASP.NET Identity tables. Both use the same PostgreSQL database and connection string.
- **Startup backfill**: On every start the API checks for `Teacher` records missing an `IdentityUserId` and links them by matching email — this handles records created before the identity link field was added.
- **Mock clock**: `IClockService` / `ClockService` wraps `DateTime.UtcNow`. The background service checks `clock.IsMocked` to decide whether to allow time-travel reversals in development.
- **Stripe v50 compatibility**: `Invoice.PaymentIntent` was removed in Stripe.net v50; the subscription creation flow now expands `latest_invoice.confirmation_secret` and falls back to fetching the invoice separately if the expanded value is null.
- **Revenue split model**: Platform retains 10 %; gym receives its configured `CommissionRate`; trainer receives the remainder. Splits are persisted as `PaymentSplit` records alongside the Stripe Transfer IDs.
