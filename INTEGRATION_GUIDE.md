# OmniRetail AI Enterprise — Guide d'Intégration Complet
## Architecture Senior .NET 8 | Clean Architecture | SaaS Enterprise

---

## 📁 STRUCTURE DES FICHIERS À INTÉGRER

```
enterprise_v2/
│
├── Core/
│   ├── Entities/
│   │   ├── RefreshToken.cs       → OmniRetail.Core/Entities/
│   │   ├── UserSession.cs        → OmniRetail.Core/Entities/
│   │   ├── AuditLog.cs           → OmniRetail.Core/Entities/
│   │   └── Permission.cs         → OmniRetail.Core/Entities/
│   ├── DTOs/
│   │   └── EnterpriseDtos.cs     → OmniRetail.Core/DTOs/
│   └── Enums/
│       └── Permissions.cs        → OmniRetail.Core/Enums/
│
├── Application/
│   └── Interfaces/
│       ├── IAuthService.cs       → OmniRetail.Application/Interfaces/ (REMPLACER)
│       └── EnterpriseInterfaces.cs → OmniRetail.Application/Interfaces/
│
├── Infrastructure/
│   ├── Services/
│   │   ├── AuthService.cs        → OmniRetail.Infrastructure/Services/ (REMPLACER)
│   │   ├── AuditService.cs       → OmniRetail.Infrastructure/Services/
│   │   ├── CacheService.cs       → OmniRetail.Infrastructure/Services/
│   │   ├── AiAssistantService.cs → OmniRetail.Infrastructure/Services/
│   │   └── ReportService.cs      → OmniRetail.Infrastructure/Services/
│   ├── Data/
│   │   ├── OmniRetailDbContext.cs → OmniRetail.Infrastructure/Data/ (REMPLACER)
│   │   └── DbSeeder.cs            → OmniRetail.Infrastructure/Data/ (REMPLACER)
│   └── OmniRetail.Infrastructure.csproj → (REMPLACER)
│
├── API/
│   ├── Controllers/
│   │   ├── AuthController.cs     → OmniRetail.API/Controllers/ (REMPLACER)
│   │   ├── AiController.cs       → OmniRetail.API/Controllers/
│   │   └── AuditAndReportsController.cs → OmniRetail.API/Controllers/
│   ├── Middleware/
│   │   ├── ExceptionMiddleware.cs     → OmniRetail.API/Middleware/ (REMPLACER)
│   │   └── SecurityHeadersMiddleware.cs → OmniRetail.API/Middleware/
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs → OmniRetail.API/Extensions/
│   ├── Program.cs                → OmniRetail.API/ (REMPLACER)
│   ├── appsettings.json          → OmniRetail.API/ (REMPLACER)
│   └── OmniRetail.API.csproj     → OmniRetail.API/ (REMPLACER)
│
└── docker/
    ├── docker-compose.yml        → racine du projet (REMPLACER)
    └── Dockerfile                → docker/Dockerfile (CRÉER)

.github/
└── workflows/
    └── ci-cd.yml                 → .github/workflows/ci-cd.yml (CRÉER)
```

---

## 🗑️ SUPPRIMER

```bash
del OmniRetail.API/WeatherForecast.cs
```

---

## ⚠️ VARIABLE D'ENVIRONNEMENT ANTHROPIC (optionnel)

Pour activer l'IA réelle (Claude), ajouter dans `.env` :
```bash
ANTHROPIC_API_KEY=sk-ant-api03-...
```
Sans cette clé, l'assistant IA utilise des réponses simulées intelligentes.

---

## 📦 PACKAGES MANQUANTS — INSTALLATION

```bash
# Infrastructure
cd OmniRetail.Infrastructure
dotnet add package AspNetCore.HealthChecks.Redis --version 8.0.1
dotnet add package Microsoft.Extensions.Http --version 8.0.0

# API
cd ../OmniRetail.API
dotnet add package AspNetCore.HealthChecks.Redis --version 8.0.1
dotnet add package Serilog.Enrichers.Environment --version 2.3.0
dotnet add package Serilog.Sinks.File --version 6.0.0
```

---

## 🗄️ MIGRATION EF CORE

```bash
# Depuis la racine du projet
cd OmniRetail.API

dotnet ef migrations add AddEnterpriseFeatures \
  --project ../OmniRetail.Infrastructure \
  --startup-project . \
  --output-dir ../OmniRetail.Infrastructure/Migrations

dotnet ef database update \
  --project ../OmniRetail.Infrastructure \
  --startup-project .
```

---

## 🐳 DÉMARRAGE DOCKER

```bash
# Développement (postgres + redis seulement)
docker-compose up postgres redis -d

# Production complète (API incluse)
docker-compose --profile "" up -d

# Avec pgAdmin
docker-compose --profile tools up -d
```

---

## ✅ ORDRE D'INTÉGRATION RECOMMANDÉ

1. Copier les entités Core (RefreshToken, UserSession, AuditLog, Permission)
2. Copier les DTOs (EnterpriseDtos.cs)
3. Copier les Enums (Permissions.cs)
4. Remplacer IAuthService.cs + copier EnterpriseInterfaces.cs
5. Remplacer OmniRetailDbContext.cs
6. Remplacer AuthService.cs + copier les nouveaux services
7. Remplacer DbSeeder.cs
8. Remplacer les fichiers API (Program.cs, appsettings.json, controllers)
9. Copier Middleware et Extensions
10. Installer les packages manquants
11. Générer la migration EF Core
12. Build et test

---

## 🧪 TESTS DE VALIDATION

### 1. Test Login + Refresh Token
```bash
# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}'

# Réponse attendue : accessToken + refreshToken + sessionId

# Refresh
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<token>"}'
```

### 2. Test AI Assistant
```bash
curl -X POST http://localhost:5000/api/ai/query \
  -H "Authorization: Bearer <jwt>" \
  -H "Content-Type: application/json" \
  -d '{"question":"Quels produits sont en stock critique ?"}'
```

### 3. Test Audit Logs (Admin)
```bash
curl http://localhost:5000/api/audit?page=1&pageSize=10 \
  -H "Authorization: Bearer <jwt>"
```

### 4. Test Rapport Ventes
```bash
curl "http://localhost:5000/api/reports/sales?from=2024-01-01&to=2024-12-31" \
  -H "Authorization: Bearer <jwt>"
```

### 5. Health Check
```bash
curl http://localhost:5000/health
```

---

## 📊 ARCHITECTURE FINALE

```
Request → SecurityHeadersMiddleware
        → ExceptionMiddleware (CorrelationId)
        → CORS + RateLimiting
        → Authentication (JWT Bearer)
        → Authorization (Roles/Permissions)
        → Controller
        → Service (IAuthService, IAiAssistantService, etc.)
        → Repository (DbContext + Redis Cache)
        → PostgreSQL / Redis
        + AuditService (async, fire-safe)
```

---

## 🔐 COMPTES DE DÉMONSTRATION

| Utilisateur | Mot de passe   | Rôle     |
|-------------|----------------|----------|
| admin       | Admin123!      | Admin    |
| employee    | Employee123!   | Employee |

**pgAdmin (si activé)** : http://localhost:5050
- Email : admin@omniretail.ai
- Mot de passe : pgAdmin2024!
