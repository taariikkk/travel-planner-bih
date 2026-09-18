# Travel Planner — Bosna i Hercegovina Edition

Full-stack platforma za planiranje putovanja fokusirana na Bosnu i Hercegovinu.
Otkrivanje destinacija, planiranje itinerera dan-po-dan, interaktivna mapa sa
rutama, budžetiranje u KM/EUR, i AI asistent koji koristi tool-calling nad
stvarnim, kuriranim BiH podacima.

> Ovo je namjerno sužena, BiH-only verzija globalnog Travel Planner koncepta —
> ne umanjena verzija vizije, nego pametan prvi korak: izvodljiva za solo
> juniora u 4-8 sedmica, sa potpuno besplatnim skupom provajdera, koristeći
> istu arhitekturu na koju se kasnije regionalno proširuje.

## Sadržaj

- [Zašto BiH kao prvo tržište](#zašto-bih-kao-prvo-tržište)
- [Funkcionalnosti](#funkcionalnosti)
- [Tehnološki stack](#tehnološki-stack)
- [Arhitektura](#arhitektura)
- [Eksterne integracije](#eksterne-integracije-sve-besplatno-za-mvp)
- [Pokretanje projekta](#pokretanje-projekta)
- [Roadmap](#roadmap)
- [Definicija završenosti](#definicija-završenosti-mvp)
- [Monetizacija (buduća faza)](#monetizacija-buduća-faza)

## Zašto BiH kao prvo tržište

Umjesto globalnog obima, MVP se namjerno ograničava na jednu zemlju: manje
podataka za kuriranje, manje edge-case-ova, realniji rok za solo developera,
i jasan, demonstrabilan proizvod za portfolio i tehnički intervju — uz punu
mogućnost proširenja na regiju (Hrvatska, Srbija, Crna Gora) kasnije, koristeći
istu arhitekturu.

**Ciljani korisnici**: domaći turisti, dijaspora, regionalni posjetioci
(Hrvatska, Srbija, Crna Gora), strani turisti, grupe koje dijele troškove.

**Geografski obim MVP-a**: Sarajevo, Mostar, Trebinje i Hercegovina, Neum,
Jahorina i Bjelašnica, Banja Luka, Travnik/Počitelj/Višegrad/Jajce. Ostatak
zemlje dostupan generički kroz Mapbox/OSM bez kuriranog sadržaja.

## Funkcionalnosti

| Oblast | Opis | Faza |
|---|---|---|
| Identity | Registracija, prijava, profil, preferencije | MVP |
| Discover | Preporuke destinacija ograničene na BiH | MVP |
| Places | Restorani, atrakcije (OSM + ručna dopuna) | MVP |
| Maps | Mapbox prikaz, rute, udaljenosti unutar BiH | MVP |
| Trips | Kreiranje putovanja, datumi, putnici, itinerer | MVP |
| Itinerary | Dani, aktivnosti, ručno uređivanje, prikaz rute | MVP |
| Weather | Open-Meteo prognoza za sve BiH lokacije | MVP |
| Budget | Procjena i praćenje troškova u KM/EUR | MVP |
| AI asistent | Tool-calling nad kuriranim BiH podacima | MVP |
| Smještaj | Ručno kuriran dataset (20-40 objekata) | MVP (API u V2) |
| Saradnja | Pozivanje saputnika, komentari, glasanje | V2 |
| Aktivnosti/ture | Kurirane preporuke (rafting, ture) | V2 |
| Letovi | Van obima — BiH ima samo 3 aerodroma | V3 |
| Mobile | Android/iOS | V4 |

## Tehnološki stack

| Sloj | Tehnologija |
|---|---|
| Web | Next.js + TypeScript |
| UI | React + Tailwind CSS |
| Backend | ASP.NET Core / C# |
| ORM/DB | EF Core + PostgreSQL + PostGIS |
| Cache/Realtime | Redis / SignalR *(odloženo za V2)* |
| Maps | Mapbox |
| Places | OpenStreetMap (Overpass API) |
| Weather | Open-Meteo |
| AI | Anthropic ili OpenAI API |
| Deployment | Vercel + Render/Railway + Supabase/Neon |

## Arhitektura

Modularni monolit — namjerno, ne mikroservisi. Manji obim podataka (samo BiH)
dodatno smanjuje potrebu za ranim raspadom na servise.

```
┌──────────────────────┐
│     Next.js Web       │
│  React + TypeScript   │
└───────────┬───────────┘
            │ HTTPS / JSON
┌───────────▼───────────┐
│   ASP.NET Core API     │
│          C#            │
└───────────┬───────────┘
            │
PostgreSQL (+ PostGIS)      Redis (opciono, V2)
            │
├── Users / Trips / Itineraries
├── BiH Destinations / Places (kurirano + OSM)
└── AI conversation log
```

Svi eksterni provajderi stoje iza interfejsa (provider abstraction pattern),
lako zamjenjivi za plaćenu alternativu kad projekat generiše prihod.

## Eksterne integracije (sve besplatno za MVP)

| Kategorija | Provajder | Zašto |
|---|---|---|
| Mape/rute | Mapbox | 50k map loads + 100k geocoding/directions/mjesec besplatno, bez kartice |
| Mjesta | OpenStreetMap / Overpass | Potpuno besplatno, bez ključa, solidna pokrivenost za veće gradove |
| Vrijeme | Open-Meteo | Potpuno besplatno, bez ključa, 10k poziva/dan |
| Smještaj | Ručno kuriran dataset | Amadeus/Kiwi self-service ugašeni 2026 — kuriranje je realna opcija |
| AI | Anthropic ili OpenAI API | Pay-as-you-go, minimalni trošak na MVP obimu |

**Ukupan mjesečni trošak infrastrukture: $0-5** dok projekat ne generiše
saobraćaj koji zahtijeva plaćene planove.

## Pokretanje projekta

```bash
# Kloniraj repo
git clone <repo-url>
cd travel-planner-bih

# Podigni lokalno okruženje (Postgres + PostGIS)
docker compose up -d

# Backend
cd api
dotnet restore
dotnet ef database update --project TravelPlanner.Infrastructure --startup-project TravelPlanner.WebAPI -- --environment Development
dotnet run --project TravelPlanner.WebAPI

# Frontend (u novom terminalu)
cd web
npm install
npm run dev
```

Environment varijable (API ključevi za Mapbox, AI provajder, connection
string) idu u `.env` fajlove koji **nisu** u Git-u — vidi `.env.example` u
svakom folderu za potrebne varijable.

Backend solution `api/TravelPlanner.sln` sadrži četiri projekta:

- `TravelPlanner.Domain`: entiteti; bez EF Core-a, JWT-a i HTTP-a.
- `TravelPlanner.Application`: `DTOs`, `Interfaces`, `Services` i `Exceptions`; zavisi od Domain sloja.
- `TravelPlanner.Infrastructure`: `Persistence/Migrations`, `Repositories`, `Security`; implementira Application interfejse.
- `TravelPlanner.WebAPI`: `Program.cs`, `Endpoints`, `Properties`, konfiguracija, Swagger i CORS.

Lokalna konfiguracija je u ignorisanom
`api/TravelPlanner.WebAPI/appsettings.Development.json`. U produkciji postaviti
`ConnectionStrings__DefaultConnection` i `Jwt__Key` kroz environment varijable.
.NET ne učitava `.env` automatski.

Testovi: `dotnet test api/TravelPlanner.sln`.
EF migracije ostaju sadržajno nepromijenjene. Namespace-ovi postojećih entiteta
i konteksta zadržani su radi kompatibilnosti EF snapshot-a.
`NetTopologySuite.Point` ostaje geometrijski tip u Domain sloju;
PostGIS/EF konfiguracija je u Infrastructure sloju.

## Roadmap

Solo developer, realno 4-8 sedmica intenzivnog rada. Detaljan, granularan
checklist po zadacima (dovoljno mali da svaki bude jedan Codex prompt) vodi
se u [`ROADMAP.md`](./ROADMAP.md) — taj fajl se ažurira skoro svakodnevno
dok README ostaje stabilan pregled projekta.

Grubi pregled po sedmicama:

| Period | Fokus |
|---|---|
| Sedmica 1-2 | Temelj — setup, baza, autentifikacija, seed podaci |
| Sedmica 3-4 | Discovery i mape — wizard, preporuke, mjesta, vrijeme |
| Sedmica 5-6 | Trip planner — itinerer, budžet, smještaj |
| Sedmica 7-8 | AI asistent, testovi, deployment, portfolio materijal |
| Poslije MVP-a | Saradnja (V2), hotel API, regionalno proširenje |

## Definicija završenosti (MVP)

- Korisnik se može registrovati i podesiti preferencije
- Sistem preporučuje BiH destinacije sa objašnjenjem
- Korisnik može otvoriti stranicu destinacije sa mapom i mjestima
- Korisnik može kreirati putovanje i itinerer dan-po-dan
- Rute i udaljenosti između gradova su prikazane na mapi
- Vremenska prognoza je dostupna za odabrane datume/lokacije
- Korisnik može pratiti budžet u KM
- AI asistent može predložiti i izmijeniti itinerer koristeći stvarne BiH podatke
- Aplikacija radi produkcijski (deployed), uz minimalne troškove
- README i demo materijal jasno pokazuju BiH fokus kao svjesnu odluku

## Monetizacija (buduća faza)

- Freemium model prilagođen BiH kupovnoj moći (5-10 KM/mjesečno Premium)
- Affiliate saradnja sa domaćim hotelima/pansionima i turističkim agencijama
- Sponzorisane preporuke restorana/tura, jasno označene
- Saradnja sa turističkim zajednicama gradova (Sarajevo, Mostar, Trebinje)

---

*Radni dokument • 2026 • Izveden iz globalne specifikacije Travel Planner projekta.*
