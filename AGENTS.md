# Travel Planner BiH — AGENTS.md

* Ne koristi provjere u Chrome-u ili slično, nije instaliran plugin *

Ovaj fajl čita Codex (i drugi AI coding agenti) na početku svake sesije.
Sadrži trajni kontekst projekta. Ne mijenjati bez razloga — ovo je izvor istine
za arhitekturu i konvencije, ne mjesto za istoriju odluka (to ide u README).

## Šta je ovo

Travel Planner za Bosnu i Hercegovinu — MVP fokusiran na jedno tržište.
Otkrivanje destinacija (preporuke kroz wizard + stranica "Istraži" sa pretragom
i popularnim mjestima tekućeg mjeseca), planiranje itinerera dan-po-dan,
interaktivna mapa, budžetiranje u KM/EUR, i AI asistent koji koristi
tool-calling nad stvarnim podacima o BiH iz naše baze (ne izmišlja informacije).

Solo junior developer, cilj: portfolio-ready proizvod za 4-8 sedmica, $0-5
mjesečno hosting trošak (AI pozivi su pay-as-you-go i jedini varijabilni trošak).

## Stack

- **Web**: Next.js + TypeScript + React + Tailwind CSS
- **API**: ASP.NET Core / C#
- **DB**: PostgreSQL + PostGIS (geospatial upiti — udaljenosti, obližnja mjesta)
  uz ekstenzije `unaccent` i `pg_trgm` za pretragu tolerantnu na dijakritike
- **Cache/Realtime**: Redis / SignalR — NAMJERNO ODLOŽENO za V2, ne dodavati u MVP
- **Background poslovi**: Hangfire — isto, V2 (uvoz podataka radi na zahtjev, vidi dolje)
- **Deployment**: Vercel (web) + Render ili Railway (API) + Supabase ili Neon (Postgres)
- **CI**: GitHub Actions (build + `dotnet test` + lint/typecheck frontenda)

## Arhitektura

Modularni monolit, ne mikroservisi. Next.js frontend komunicira sa ASP.NET
Core API-jem preko HTTPS/JSON.

```
Next.js Web (React + TS)
        │ HTTPS / JSON
ASP.NET Core API (C#)
        │
PostgreSQL + PostGIS
├── Users / Trips / Itineraries / SavedPlaces
├── BiH Destinations / Places (import iz API-ja + ručni sloj)
└── AI conversation log
```

**Backend slojevi** (solution `api/TravelPlanner.sln`):

- `TravelPlanner.Domain` — entiteti; bez EF Core-a, JWT-a i HTTP-a.
- `TravelPlanner.Application` — DTO-ovi, interfejsi, servisi, izuzeci; zavisi samo od Domain.
- `TravelPlanner.Infrastructure` — EF Core, migracije, repozitoriji, sigurnost,
  implementacije provajdera; implementira Application interfejse.
- `TravelPlanner.WebAPI` — `Program.cs`, `Endpoints` (minimal API), konfiguracija, Swagger, CORS.

Zavisnosti idu samo prema unutra (WebAPI → Infrastructure → Application → Domain).

**Svi eksterni provajderi su iza interfejsa** (provider abstraction pattern —
npr. `IPlacesProvider`, `IWeatherProvider`, `IDestinationDataProvider`,
`IImageProvider`, `IRoutingProvider`, `IHotelProvider`, `IAiProvider`). Ovo je
namjerna arhitektonska odluka da bi se besplatni provajder mogao zamijeniti
plaćenim kasnije bez diranja poslovne logike.

## Strategija podataka (API-first + ručni sloj)

Cilj je najbolji mogući kvalitet uz što manje hardcodiranja. Sadržaj o
destinacijama se **ne piše u kod**. Ide u bazu na jedan od dva načina:

1. **Uvoz iz API-ja** — činjenice i širina pokrivenosti.
2. **Ručni sloj** (`Source = manual`) — urednička dopuna i ispravke.

| Podatak | Izvor | Napomena |
|---|---|---|
| Pronalaženje mjesta (grad, planina, selo), koordinate, tip, nadmorska visina, stanovnici | Wikidata | Besplatno, CC0 |
| Opisi | Wikipedia REST API (prvo `bs`, fallback `en`) | Besplatno, CC BY-SA — čuvati izvor i URL za atribuciju |
| Slike | Wikimedia Commons ili Unsplash API | Čuvati autora, licencu i URL; slike se linkuju, ne kopiraju |
| Restorani, atrakcije, vrhovi, POI | OpenStreetMap / Overpass | Keširati u bazi |
| Vrijeme, mjesečni klimatski prosjeci | Open-Meteo | Keširati u `WeatherSnapshot` |
| Smještaj | Ručno kuriran dataset | Nema besplatnog API-ja, iza `IHotelProvider` |
| "Istaknuto", ispravke opisa | Ručno | Mali urednički sloj |

**Pravila:**

- Svaki uvezeni zapis čuva `Source`, `ExternalId` (npr. Wikidata Q-id), `ImportedAt`
  i podatke za atribuciju.
- **Ručna vrijednost uvijek ima prednost nad uvezenom.** Ponovni uvoz nikad ne
  briše ni ne prepisuje ručno unesena polja (odvojena override polja ili flag po polju).
- **Uvoz radi na zahtjev sa TTL kešom** (bez Hangfirea): prvi put kad se mjesto
  otvori ili pronađe, povuče se i spremi; poslije se osvježava kad TTL istekne.
  Opciono: komanda/skripta za pred-uvoz liste popularnih mjesta.
- **Kontrola kvaliteta**: zapis bez opisa ili sa preslabim opisom ne prikazuje se
  kao "popularno"/istaknuto; UI mora imati smislen prazan prikaz umjesto lošeg sadržaja.
- **Atribucija je obavezna** u UI-ju gdje god se prikazuje Wikipedia tekst ili slika.
- **Wikimedia API pozivi moraju slati opisan `User-Agent`** i poštovati rate limit.
- **Pretraga** ide prvo nad lokalnom bazom (`unaccent` + `pg_trgm`, mora naći
  "Bjelasnica" za "Bjelašnica"), a tek ako nema dovoljno rezultata pada na Wikidata pretragu.
  Ne koristiti Nominatim za autocomplete (zabranjeno politikom korištenja).
- **Mapbox rezultate geocodinga ne pohranjivati** u bazu (ograničenja uslova
  korištenja) — Mapbox služi za prikaz mape i Directions, ne kao izvor podataka.
- **Udaljenosti** između mjesta u bazi računati PostGIS-om; Mapbox Directions
  samo za stvarne rute (itinerer).
- **"Popularno ovog mjeseca"** izvodi se iz podataka, ne iz analitike: mjeseci
  sezone destinacije (izvedeni iz tagova i Open-Meteo klimatskih prosjeka, uz
  ručnu korekciju) plus ručna oznaka `IsFeatured`.

## Eksterni provajderi (besplatni za MVP osim AI-ja — ne dodavati plaćene bez pitanja)

| Namjena | Provajder | Napomena |
|---|---|---|
| Mape/rute | Mapbox | Besplatni tier; aktuelne limite provjeriti na mapbox.com/pricing prije oslanjanja |
| Podaci o mjestima | Wikidata + Wikipedia | Besplatno, bez ključa, uz atribuciju |
| Slike | Wikimedia Commons / Unsplash | Besplatno, uz atribuciju |
| Mjesta/restorani | OpenStreetMap / Overpass API | Potpuno besplatno, bez ključa |
| Vrijeme | Open-Meteo | Potpuno besplatno, bez ključa, 10k poziva/dan |
| Smještaj | Ručno kuriran dataset (20-40 objekata) | Nema API-ja u MVP fazi |
| AI | Anthropic ili OpenAI API | Pay-as-you-go — jedini plaćeni provajder |

## Geografski obim MVP-a

**Cijela BiH** je pretraživa kroz uvoz iz API-ja. **Ručno kuriran sadržaj**
(istaknuta mjesta, ručne dopune, smještaj) samo za: Sarajevo, Mostar, Trebinje i
Hercegovina, Neum, Jahorina i Bjelašnica, Banja Luka, Travnik/Počitelj/Višegrad/Jajce.

## Frontend i dizajn

**Rute (planirane):** početna, wizard/preporuke (postojeća ruta), `/explore`
(stranica "Istraži"), `/destinations/[slug]`, `/dashboard`, `/trips` i
`/trips/[id]` (sedmica 5-6).

**Navigacija:** jedan zajednički layout sa navbarom na svim stranicama. Javni
korisnik vidi: Kako funkcioniše, Istraži, Prijava, Kreiraj račun. Prijavljeni
korisnik vidi: Istraži, Preporuke, Moja putovanja, profil/odjava. BS/EN prekidač
je u navbaru. Na mobilnom hamburger meni.

**Dizajn sistem — obavezno očuvati postojeći izgled:**

- topla krem pozadina i **jedna** duboka zelena akcentna boja,
- serifni naslovi krupne veličine, sa jednom istaknutom zelenom kurzivnom riječi,
  sitni sans-serif za tijelo teksta,
- editorijalni raspored: tanki razdjelnici umjesto kartica sa sjenkama,
  numerisane sekcije (01, 02…), puno praznog prostora,
- outline čipovi, jedno primarno zeleno dugme sa strelicom,
- sitne mikro-labele ("Zašto ti odgovara", "Oko tebe").

Nove stranice koriste postojeće Tailwind tokene (boje, fontovi, razmaci). **Ne
uvoditi nove boje, fontove ni stilove kartica.** Eventualne referentne slike
tuđih aplikacija služe samo za strukturu i funkcionalnost, ne za izgled.

**Lokalizacija:** aplikacija je dvojezična (BS/EN). Svi korisnički tekstovi idu
kroz i18n rječnik, ne hardkodirani u komponentama. Uvezeni opisi se prikazuju na
jeziku korisnika kad postoje, uz fallback.

## Konvencije koda

- **C# backend**: standardne .NET konvencije — PascalCase za klase/metode,
  camelCase za lokalne varijable, `I` prefiks za interfejse.
- **Repository pattern** za pristup bazi — nikad direktan `DbContext` u
  endpointima ni servisima izvan Infrastructure sloja.
- Svaki novi API endpoint dobija odgovarajući unit test (xUnit) prije nego
  se smatra gotovim. Isto važi za scoring, uvoz podataka i budžet kalkulacije.
- **Next.js/React**: funkcionalne komponente + hooks, TypeScript strict mode
  uključen, nikakav `any` bez eksplicitnog razloga u komentaru.
- Nazivi tabela i entiteta u bazi: `User`, `Trip`, `TripDay`, `ItineraryItem`,
  `Expense`, `Destination`, `Place`, `Accommodation`, `WeatherSnapshot`,
  `SavedPlace`, `ShareLink`, `AiConversation`, `AiMessage`.
- Nove kolone i tabele isključivo kroz **nove** migracije.

## Valuta i lokalizacija

Primarna valuta je KM (BAM). EUR je opcioni prikaz, fiksni kurs
1,95583 KM = 1 EUR (ne pozivati live exchange rate API za ovo — kurs je fiksan).

## AI asistent — alati (tools)

AI je kontrolisani agent koji poziva alate nad podacima iz naše baze i
provjerenih provajdera, ne generiše informacije o BiH iz svog znanja:

- `search_bih_destinations` — preporuka gradova/regija prema preferencijama
  (uvezeni + kurirani podaci)
- `search_places` — restorani/atrakcije po gradu (OSM + kurirana dopuna)
- `get_weather` — Open-Meteo za odabranu lokaciju
- `get_route` — Mapbox Directions između gradova
- `search_accommodation` — kurirani accommodation dataset
- `get_trip` / `update_itinerary` — čitanje/izmjena korisničkog itinerera
- `add_expense` — dodavanje troška u KM

Izmjene itinerera AI samo **predlaže**; upis tek nakon odobrenja korisnika.

## Pravila za agenta (Codex)

- **Ne dodavati plaćene API integracije** bez eksplicitnog pitanja korisniku
  (AI provajder je jedini dozvoljeni plaćeni servis).
- **Ne hardkodirati sadržaj destinacija/mjesta u kod.** Sadržaj ide u bazu kroz
  uvoz ili kroz seed sa `Source = manual`.
- **Ne mijenjati postojeće EF Core migracije** — samo dodavati nove.
- **Ne uvoditi Redis/SignalR/Hangfire** dok se eksplicitno ne zatraži (V2 stvar).
- **Ne mijenjati vizuelni identitet** (vidi "Frontend i dizajn").
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
stranica "Istraži" prikazuje popularna mjesta mjeseca i pretragu, stranica
destinacije ima mapu, mjesta i vremensku prognozu, itinerer dan-po-dan se može
kreirati i uređivati, mapa prikazuje rute, budžet se prati u KM, AI asistent
može predložiti/izmijeniti itinerer, aplikacija je dvojezična (BS/EN),
deployovana i dostupna online, a README i demo materijal jasno pokazuju BiH fokus.