# FitPlay

FitPlay is a multi-tenant fitness management platform built with .NET 8. It supports gyms, trainers, and users in the same system, with booking workflows, role-based access, gamification, and Stripe billing.

Live deployment: `https://dependable-enthusiasm-production-a97f.up.railway.app`

## Main Features

- Multi-tenant gym architecture (independent gyms, trainers, and users)
- Role-based access (`User`, `Trainer`, `GymAdmin`)
- Class and room booking flows
- Stripe integration for subscriptions and payments
- Gamification (XP, levels, achievements)
- JWT + Identity authentication, including external login providers

## Tech Stack

- Backend: ASP.NET Core 8 (`FitPlay.Api`)
- Domain layer: `FitPlay.Domain`
- Frontend: Blazor Server (`FitPlay-Blazor`)
- Database: PostgreSQL + Entity Framework Core
- Payments: Stripe
- Deployment: Railway + Docker

## Project Structure

- `FitPlay.Api` - REST API, auth, billing, webhooks, business endpoints
- `FitPlay.Domain` - entities, services, and domain logic
- `FitPlay-Blazor` - web app UI
- `FitPlay.tests` - automated tests

## Quick Start (Local)

Prerequisites:

- .NET 8 SDK
- PostgreSQL

1. Clone this repository.
2. Copy `.env.example` to `.env`.
3. Fill `.env` with valid values (Stripe keys at minimum).
4. Restore dependencies:

```bash
dotnet restore FitPlay-Project.sln
```

5. Run API:

```bash
dotnet run --project FitPlay.Api
```

6. Run Blazor app:

```bash
dotnet run --project FitPlay-Blazor
```

## Environment Variables

Current template using dotnet user-secrets

Notes:

readme to set keys was sent by email to john 

## Railway Deployment

- Production URL: `https://dependable-enthusiasm-production-a97f.up.railway.app`
- CI/CD pipeline is configured through GitHub Actions (`.github/workflows/main.yml`).
- Deployments run on pushes to `master` after build/test checks.

## Screenshots

 Suggested captures:

- Login/Register page
- Gym onboarding (multi-tenant flow)
- Class/room booking
- Stripe payment flow
- Gamification dashboard (XP/achievements)



## Academic Notes

This project demonstrates:

- Multi-tenant architecture in a real fitness scenario
- Clean separation between API, domain logic, and UI
- Real payment provider integration (Stripe)
- Cloud deployment with CI/CD and environment-based configuration
