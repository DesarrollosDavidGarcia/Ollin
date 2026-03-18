# Plan: Ollin — Landing Page + Blog + Dashboard

## Context

Sistema Tlatoani es la calculadora estadistica (motor de value bets). Ollin es el **sitio web publico** que:
- Vende un paquete de $299 MXN (landing page)
- Publica 2-3 articulos/dia generados por IA (blog SEO)
- Muestra dashboards de inteligencia deportiva a suscriptores
- Consume datos de Tlatoani via PostgreSQL compartido

Ambos proyectos comparten BD PostgreSQL. El blog muestra **solo analisis general**, nunca datos calculados (Edge, Kelly, IC 95%).

---

## Arquitectura de la solucion

```
SistemaTlatoani.sln
  ├── SistemaTlatoani/       (calculadora existente, port 8080, uso interno)
  ├── Ollin/                 (sitio publico, port 8081)
  ├── TlatoaniShared/        (EF Core entities + DbContext compartido)
  docker-compose.yml         (actualizado: +postgres, +redis, +ollin)
```

**Comunicacion:** Shared PostgreSQL (no API entre proyectos). Tlatoani escribe a schema `core`, Ollin lee de `core` y escribe en schema `web`.

---

## Fase 1: Foundation — TlatoaniShared + PostgreSQL

### 1.1 Proyecto TlatoaniShared (class library)

```
TlatoaniShared/
  TlatoaniShared.csproj       (EF Core + Npgsql)
  Data/
    TlatoaniDbContext.cs       (schemas core + web)
  Entities/
    Core/
      Analysis.cs              (reemplaza JSON storage)
      Prediction.cs            (reemplaza backtesting.json)
      JornadaSummary.cs
      MatchIntelligence.cs     (del pipeline v4)
      DailyScoutLog.cs
      WeeklySummary.cs
      TeamSeasonStats.cs
      PlayerSeasonStats.cs
      AnomalyLog.cs
    Web/
      BlogPost.cs
      BlogCategory.cs
      BlogTag.cs
      BlogPostTag.cs
      Subscription.cs
      ContentQueueItem.cs
      SeoRedirect.cs
      ApplicationUser.cs       (extends IdentityUser)
```

### 1.2 Schema PostgreSQL

**Schema `core`** (datos de la calculadora):

| Tabla | Reemplaza | Descripcion |
|---|---|---|
| `core.analyses` | `data/YYYY-MM-DD/*.json` | Resultados completos por partido |
| `core.predictions` | `data/backtesting.json` | Predicciones + resultados reales |
| `core.jornada_summaries` | (nuevo) | Agregacion de jornada con plays |
| `core.match_intelligence` | (pipeline v4) | Inteligencia pre/live/post match |
| `core.daily_scout_log` | (pipeline v4) | Log incremental de scouting |
| `core.weekly_summaries` | (pipeline v4) | Resumenes semanales |
| `core.team_season_stats` | (pipeline v4) | Stats acumuladas por equipo |
| `core.player_season_stats` | (pipeline v4) | Stats acumuladas por jugador |
| `core.anomaly_log` | (pipeline v4) | Anomalias detectadas |

**Schema `web`** (datos del sitio publico):

| Tabla | Descripcion |
|---|---|
| `web.asp_net_users` + Identity tables | Usuarios (ASP.NET Identity) |
| `web.subscriptions` | Suscripciones $299 MXN |
| `web.blog_posts` | Articulos con SEO (slug, meta, OG, JSON-LD) |
| `web.blog_categories` | Categorias: Previews, Resultados, Tendencias |
| `web.blog_tags` | Tags por equipo, jornada, torneo |
| `web.blog_post_tags` | Relacion many-to-many |
| `web.content_queue` | Cola de generacion de contenido LLM |
| `web.seo_redirects` | Redirects 301 para SEO |

### 1.3 Docker Compose

```yaml
postgres:       # PostgreSQL 16, shared_buffers=256MB, max_connections=50
redis:          # Redis 7, maxmemory 128mb
ollin:          # Sitio publico, port 8081
```

**Budget de memoria (8GB VPS):**
- PostgreSQL: ~400MB | Redis: 128MB | Tlatoani: ~200MB | Ollin: ~250MB
- OpenClaw + Chrome: ~1.3GB | OS + Traefik + DokPloy: ~1GB
- **Total: ~3.3GB usado, ~4.7GB libre**

---

## Fase 2: Migrar Tlatoani a PostgreSQL

### Archivos a modificar en SistemaTlatoani:

| Archivo | Cambio |
|---|---|
| `SistemaTlatoani.csproj` | Agregar ref a TlatoaniShared |
| `Program.cs` | Registrar TlatoaniDbContext con connection string |
| `Services/AnalysisBackgroundService.cs` | Dual-write: JSON + PostgreSQL |
| `Services/BacktestingService.cs` | Reemplazar File I/O por EF Core |
| `Services/JsonStorageService.cs` | Mantener como fallback, agregar write a PostgreSQL |
| `appsettings.json` | Agregar ConnectionStrings.Default |

### Script de migracion one-time:
- Leer todos los `data/YYYY-MM-DD/*.json` existentes
- Leer `data/backtesting.json`
- Insertar en PostgreSQL

---

## Fase 3: Ollin — Estructura del proyecto

```
Ollin/
  Controllers/
    HomeController.cs            — Landing page + pricing
    BlogController.cs            — Blog listing, post, categoria, tag, RSS
    DashboardController.cs       — [Authorize(Premium)] Jornada, match, backtesting
    AccountController.cs         — Login, register, profile
    SubscriptionController.cs    — Checkout Stripe, webhook, manage
    SitemapController.cs         — Genera sitemap.xml dinamico
  Services/
    BlogGenerationService.cs     — BackgroundService: genera articulos con LLM
    ContentSchedulerService.cs   — Programa publicacion automatica
    StripeService.cs             — Integracion de pagos con Stripe
    SeoService.cs                — Helpers para structured data
    DashboardDataService.cs      — Lee schema core, transforma para vistas
  Views/
    Home/Index.cshtml            — Hero, features, pricing $299, testimonials
    Blog/Index.cshtml            — Listado con paginacion + sidebar categorias
    Blog/Post.cshtml             — Articulo con JSON-LD Article + SportsEvent
    Dashboard/Index.cshtml       — Overview: plays activos, hit rate, ROI
    Dashboard/Jornada.cshtml     — Detalle de jornada con todos los partidos
    Dashboard/Match.cshtml       — Analisis profundo de un partido
    Dashboard/Backtesting.cshtml — Historial de rendimiento (graficas)
```

### NuGet packages:
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Markdig` (markdown → HTML para articulos)
- `Stripe.net` (pagos con Stripe)

---

## Fase 4: Blog — Generacion automatica con IA

### Schedule de generacion:

| Dia | Hora | Tipo articulo | Fuente de datos |
|---|---|---|---|
| Domingo | 22:00 | Resumen de Jornada | `core.analyses` del fin de semana |
| Martes | 10:00 | Preview Jornada (1-2) | `core.match_intelligence` + `daily_scout_log` |
| Jueves | 18:00 | Tendencias y Estadisticas | `core.team_season_stats` + `anomaly_log` |
| Viernes | 09:00 | Preview Partido Estelar | `core.match_intelligence` enriquecido |
| Lunes | 08:00 | Analisis Post-Jornada | `core.weekly_summaries` |

### Flujo de generacion:

```
1. ContentSchedulerService verifica schedule
2. Consulta datos de core.analyses / core.match_intelligence
3. Inserta en web.content_queue (status=pending)
4. BlogGenerationService toma items pendientes
5. Construye prompt con:
   - Datos fuente (SIN Edge/Kelly/IC — solo stats generales)
   - Template por tipo (preview/recap/tendencias)
   - Instrucciones SEO (meta description ≤160 chars, keywords, slug)
   - Tono: analitico, accesible, terminologia futbol mexicano
6. Llama a NovitaAi (deepseek-v3.2) para generar
7. Parsea: title, slug, excerpt, body_markdown, meta_description, keywords
8. Convierte markdown → HTML con Markdig
9. Actualiza content_queue status=approved
10. Crea web.blog_posts con status=published
```

### Reglas de contenido publico vs privado:

**Publico (blog):** H2H, tabla posiciones, forma reciente, xG general, PPDA, clima, lesiones, resultados, narrativa editorial.

**NUNCA publico:** Edge %, Kelly unidades, IC 95%, Value Bet signals, kill switch, plays recomendados (1/X/2), percentiles (P1-P8), probabilidades del modelo.

### SEO:
- JSON-LD: `Article` + `SportsEvent` por articulo
- Open Graph + Twitter Cards en `<head>`
- `sitemap.xml` dinamico (todos los posts publicados + paginas estaticas)
- `robots.txt`: permite blog, bloquea `/dashboard/` y `/account/`
- URLs canonicas en cada pagina
- RSS feed en `/blog/rss`
- Breadcrumbs con `BreadcrumbList` structured data
- Modelo LLM: `deepseek/deepseek-v3.2` ($0.269/$0.40 per M tokens)

---

## Fase 5: Pagos — Stripe

### Flujo:
1. Usuario click "Suscribirme $299/mes"
2. `SubscriptionController.Checkout` → crea Stripe Checkout Session ($299 MXN, recurring)
3. Redirect a Stripe Checkout
4. Success → webhook `checkout.session.completed` → crear `web.subscriptions` (status=active, stripe_subscription_id)
5. Webhook `invoice.payment_succeeded` → renovar suscripcion
6. Webhook `customer.subscription.deleted` → desactivar suscripcion
7. Portal de Stripe para que el usuario gestione su suscripcion

### Roles ASP.NET Identity:
- `Free` — landing + blog publico
- `Premium` — dashboard + blog premium
- `Admin` — moderacion de contenido

---

## Fase 6: Dashboard (Premium)

Vistas protegidas con `[Authorize(Policy = "PremiumOnly")]`.

### Contenido del dashboard:

| Vista | Muestra | NO muestra |
|---|---|---|
| Overview | Plays activos (count), hit rate, ROI, resumen | Edge exacto, Kelly raw |
| Jornada | Partidos analizados, indicadores visuales (semaforo) | Numeros de percentiles |
| Match | Radar de fortalezas, comparativa probabilidades (visual) | IC 95% numerico |
| Backtesting | Grafica PnL acumulado, hit rate historico | Formulas, parametros |
| Trends | xG por equipo over time, PPDA tendencias | Correlaciones internas |

Charts con **Chart.js** (ligero, sin dependencias).

---

## Fase 7: Complementar ligamx_pipeline_prompt_v4.md

Agregar al documento:

1. **Seccion "Integracion con Sistema Tlatoani"** — Como la calculadora alimenta el pipeline
2. **Seccion "Landing Page + Blog"** — Estructura del sitio web publico Ollin
3. **Seccion "Modelo de monetizacion $299"** — Desglose del paquete premium
4. **Actualizar SKILL_4 (content_generator)** — Agregar tipo `blog_article` con SEO requirements
5. **Actualizar Docker Compose** — Reflejar la arquitectura de 4 servicios
6. **Agregar SKILL_6: blog_generator** — Nuevo skill para generacion automatica de articulos

---

## Secuencia de implementacion

| # | Tarea | Archivos clave |
|---|---|---|
| 1 | Crear TlatoaniShared (entities + DbContext) | `TlatoaniShared/` |
| 2 | Agregar PostgreSQL + Redis a docker-compose | `docker-compose.yml` |
| 3 | SQL init script con schemas core + web | `sql/init.sql` |
| 4 | Migrar SistemaTlatoani a dual-write PostgreSQL | `AnalysisBackgroundService.cs`, `BacktestingService.cs`, `Program.cs` |
| 5 | Script migracion JSON → PostgreSQL | `sql/migrate-json.cs` (one-time) |
| 6 | Crear Ollin skeleton + Identity | `Ollin/` |
| 7 | Landing page (Home/Index) | `Views/Home/Index.cshtml` |
| 8 | Blog system (controller + views + SEO) | `BlogController.cs`, views |
| 9 | BlogGenerationService (LLM → articulos) | `Services/BlogGenerationService.cs` |
| 10 | Stripe integration | `Services/StripeService.cs` |
| 11 | Dashboard (Premium) | `DashboardController.cs`, views |
| 12 | Complementar ligamx_pipeline_prompt_v4.md | `ligamx_pipeline_prompt_v4.md` |

---

## Verificacion

1. `docker compose up` levanta postgres + redis + tlatoani + ollin
2. Correr un analisis en Tlatoani → verificar que datos llegan a PostgreSQL (core.analyses)
3. Verificar que Ollin lee los datos y muestra en dashboard
4. Generar un articulo de blog via BlogGenerationService → verificar SEO (Google Rich Results Test)
5. Flujo de pago: Stripe Checkout → webhook → suscripcion activa → acceso a dashboard
6. `sitemap.xml` incluye todos los posts publicados
7. `robots.txt` bloquea /dashboard/ y /account/
