# Travel Planner BiH — AGENTS.md

* Ne koristi provjere u Chrome-u ili slično, nije intaliran plugin *

Ovaj fajl čita Codex (i drugi AI coding agenti) na početku svake sesije.
Sadrži trajni kontekst projekta. Ne mijenjati bez razloga — ovo je izvor istine
za arhitekturu i konvencije, ne mjesto za istoriju odluka (to ide u README).

## Šta je ovo

Travel Planner za Bosnu i Hercegovinu — MVP fokusiran na jedno tržište.
Otkrivanje destinacija, planiranje itinerera dan-po-dan, interaktivna mapa,
budžetiranje u KM/EUR, i AI asistent koji koristi tool-calling nad stvarnim,
kuriranim BiH podacima (ne izmišlja informacije).

Solo junior developer, cilj: portfolio-ready proizvod za 4-8 sedmica, $0-5
mjesečno hosting trošak.

## Stack

- **Web**: Next.js + TypeScript + React + Tailwind CSS
- **API**: ASP.NET Core / C#
- **DB**: PostgreSQL + PostGIS (geospatial upiti — udaljenosti, obližnja mjesta)
- **Cache/Realtime**: Redis / SignalR — NAMJERNO ODLOŽENO za V2, ne dodavati u MVP
- **Background poslovi**: Hangfire — isto, V2
- **Deployment**: Vercel (web) + Render ili Railway (API) + Supabase ili Neon (Postgres)

## Arhitektura

Modularni monolit, ne mikroservisi. Next.js frontend komunicira sa ASP.NET
Core API-jem preko HTTPS/JSON.

```
Next.js Web (React + TS)
        │ HTTPS / JSON
ASP.NET Core API (C#)
        │
PostgreSQL + PostGIS
├── Users / Trips / Itineraries
├── BiH Destinations / Places (kurirano + OSM)
└── AI conversation log
```

**Svi eksterni provajderi su iza interfejsa** (provider abstraction pattern —
npr. `IPlacesProvider`, `IHotelProvider`, `IWeatherProvider`). Ovo je namjerna
arhitektonska odluka da bi se besplatni provajder mogao zamijeniti plaćenim
kasnije bez diranja poslovne logike.

## Eksterni provajderi (svi besplatni za MVP — ne dodavati plaćene bez pitanja)

| Namjena | Provajder | Napomena |
|---|---|---|
| Mape/rute | Mapbox | Map Loads for Web: 5.000/mjesec | Static Tiles API: 20.000/mjesec | Static Images API: 5.000/mjesec | Mobile MAU: 100/mjesec |
| Mjesta/restorani | OpenStreetMap / Overpass API | Potpuno besplatno, bez ključa |
| Vrijeme | Open-Meteo | Potpuno besplatno, bez ključa, 10k poziva/dan |
| Smještaj | Ručno kuriran dataset (20-40 objekata) | Nema API-ja u MVP fazi |
| AI | Anthropic ili OpenAI API | Pay-as-you-go |

## Geografski obim MVP-a

Kuriran sadržaj samo za: Sarajevo, Mostar, Trebinje i Hercegovina, Neum,
Jahorina i Bjelašnica, Banja Luka, Travnik/Počitelj/Višegrad/Jajce.
Ostatak zemlje ide kroz generički Mapbox/OSM bez kuriranog sadržaja.

## Konvencije koda

- **C# backend**: standardne .NET konvencije — PascalCase za klase/metode,
  camelCase za lokalne varijable, `I` prefiks za interfejse.
- **Repository pattern** za pristup bazi — nikad direktan `DbContext` u
  kontrolerima.
- Svaki novi API endpoint dobija odgovarajući unit test (xUnit) prije nego
  se smatra gotovim.
- **Next.js/React**: funkcionalne komponente + hooks, TypeScript strict mode
  uključen, nikakav `any` bez eksplicitnog razloga u komentaru.
- Nazivi tabela i entiteta u bazi: `User`, `Trip`, `TripDay`, `ItineraryItem`,
  `Expense`, `Destination`, `Place`, `Accommodation`, `WeatherSnapshot`.

## Valuta i lokalizacija

Primarna valuta je KM (BAM). EUR je opcioni prikaz, fiksni kurs
1,95583 KM = 1 EUR (ne pozivati live exchange rate API za ovo — kurs je fiksan).

## AI asistent — alati (tools)

AI je kontrolisani agent koji poziva alate nad kuriranim podacima, ne
generiše informacije o BiH iz svog znanja:

- `search_bih_destinations` — preporuka gradova/regija prema preferencijama
- `search_places` — restorani/atrakcije po gradu (OSM + kurirana dopuna)
- `get_weather` — Open-Meteo za odabranu lokaciju
- `get_route` — Mapbox Directions između gradova
- `search_accommodation` — kurirani accommodation dataset
- `get_trip` / `update_itinerary` — čitanje/izmjena korisničkog itinerera
- `add_expense` — dodavanje troška u KM

## Pravila za agenta (Codex)

- **Ne dodavati plaćene API integracije** bez eksplicitnog pitanja korisniku
  — cijeli projekat cilja $0-5 mjesečno trošak.
- **Ne mijenjati postojeće EF Core migracije** — samo dodavati nove.
- **Ne uvoditi Redis/SignalR/Hangfire** dok se eksplicitno ne zatraži (V2 stvar).
- Kad nešto nije jasno definisano u specifikaciji, predloži rješenje i
  eksplicitno označi pretpostavku u komentaru/commit poruci — ne nagađaj
  tiho.
- Testovi (xUnit za unit/integration, Playwright za E2E) idu uz svaku
  funkcionalnu cjelinu, ne kao "poliranje na kraju".
- Tajni podaci (API ključevi, connection stringovi) isključivo u environment
  varijablama, nikad hardkodirani ili u Git-u.

## Definicija završenosti (MVP)

Vidi `ROADMAP.md` za detaljnu podjelu po sprintovima. Projekat se smatra
MVP-gotovim kad: registracija/login radi, sistem preporučuje BiH destinacije,
itinerer dan-po-dan se može kreirati i uređivati, mapa prikazuje rute, budžet
se prati u KM, AI asistent može predložiti/izmijeniti itinerer, i aplikacija
je deployovana i dostupna online.
