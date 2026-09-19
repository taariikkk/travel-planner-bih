# Roadmap — Travel Planner BiH (solo developer, 4-8 sedmica)

Ovo je radni checklist. Čekiraj `[x]` kako završavaš zadatke. Svaki zadatak je
namjerno dovoljno mali da bude **jedan Codex prompt** — ako ti se čini prevelik,
razbij ga dalje prije nego ga daš Codexu.

Redoslijed unutar sedmice je preporučen, ali ne strogo obavezan — neki zadaci
(npr. seed podataka) mogu ići paralelno sa drugima.

---

## Sedmica 1-2 — Temelj

### Infrastruktura
- [x] Next.js + TypeScript + Tailwind scaffold (`web/`)
- [x] ASP.NET Core Web API scaffold (`api/`)
- [x] Docker Compose sa PostgreSQL + PostGIS
- [ ] `.env.example` i `appsettings.Development.json.example` šabloni (bez pravih vrijednosti) commitovani

### Baza podataka i EF Core
- [x] `ApplicationDbContext` povezan na PostgreSQL
- [x] `User` entitet (Id, Email, PasswordHash, DisplayName, CreatedAt, Preferences)
- [x] `Destination` entitet (Id, Name, Region, Description, BestTimeToVisit, Tags[])
- [x] `Place` entitet (Id, DestinationId, Name, Category, Location [PostGIS point], Source [OSM/manual])
- [x] `Trip` entitet (Id, UserId, Title, StartDate, EndDate, Travelers)
- [x] `TripDay` entitet (Id, TripId, DayNumber, Date)
- [x] `ItineraryItem` entitet (Id, TripDayId, PlaceId, Order, Notes)
- [x] `Expense` entitet (Id, TripId, Category, AmountKM, Description)
- [x] `Accommodation` entitet (Id, DestinationId, Name, Type, PricePerNightKM, ContactLink)
- [x] `WeatherSnapshot` entitet (Id, DestinationId, FetchedAt, RawData)
- [x] Prva migracija (`dotnet ef migrations add InitialCreate`) i primjena na lokalnu bazu
- [x] PostGIS ekstenzija uključena u migraciji (`CREATE EXTENSION IF NOT EXISTS postgis`)

### Autentifikacija
- [x] Registracija endpoint (`POST /api/auth/register`) — hash lozinke (bcrypt/Argon2)
- [x] Login endpoint (`POST /api/auth/login`) — vraća JWT access token
- [x] JWT middleware/autorizacija na backend-u
- [x] Endpoint za profil (`GET/PUT /api/users/me`) uključujući preferencije
- [x] Unit testovi za auth logiku (hashing, token generisanje)

### Seed podaci
- [x] Seed skripta/migracija sa 10-15 BiH destinacija (tabela 1.2 iz specifikacije: Sarajevo, Mostar, Trebinje i Hercegovina, Neum, Jahorina i Bjelašnica, Banja Luka, Travnik, Počitelj, Višegrad, Jajce)
- [x] Svaka destinacija ima opis, best time to visit, tagove (historija/priroda/grad/hrana)

**Definicija završenosti Sedmice 1-2**: korisnik se može registrovati, prijaviti,
podesiti preferencije; baza ima 10-15 destinacija; migracije rade bez grešaka.

---

## Sedmica 3-4 — Discovery i mape

### Wizard preferencija i scoring
- [ ] Frontend: wizard komponenta (interesovanja, budžet, broj dana, sezona)
- [ ] Backend: `search_bih_destinations` logika — scoring algoritam koji upoređuje
      preferencije korisnika sa tagovima destinacija
- [ ] Endpoint `POST /api/destinations/recommend` vraća rangiranu listu sa
      objašnjenjem ("Trebinje — odgovara tvom interesu za vino i historiju")

### Stranica destinacije
- [ ] Frontend: dinamička ruta `/destinations/[slug]`
- [ ] Prikaz: pregled, tipično trajanje boravka, tagovi
- [ ] Integracija Mapbox mape (prikaz destinacije + obližnjih mjesta)

### OpenStreetMap / Overpass integracija
- [ ] `IPlacesProvider` interfejs (provider abstraction pattern)
- [ ] `OverpassPlacesProvider` implementacija — upit za restorane/atrakcije po gradu
- [ ] Keširanje Overpass odgovora (izbjeći ponovljene pozive za istu destinaciju)
- [ ] Ručno dodane "must-see" lokacije za prioritetne destinacije (30-50 po gradu)

### Open-Meteo integracija
- [ ] `IWeatherProvider` interfejs
- [ ] `OpenMeteoWeatherProvider` implementacija
- [ ] Endpoint `GET /api/weather?lat=&lng=&date=`
- [ ] Prikaz prognoze na stranici destinacije

**Definicija završenosti Sedmice 3-4**: korisnik odgovori na wizard, dobije
rangirane preporuke sa objašnjenjem, otvori stranicu destinacije sa mapom,
mjestima i vremenskom prognozom.

---

## Sedmica 5-6 — Trip planner

### Trip CRUD
- [ ] `POST /api/trips` — kreiranje putovanja (naslov, datumi, broj putnika)
- [ ] `GET /api/trips/{id}` — čitanje sa danima i stavkama itinerera
- [ ] `PUT /api/trips/{id}` — izmjena
- [ ] `DELETE /api/trips/{id}`
- [ ] Autorizacija: korisnik vidi/mijenja samo svoja putovanja

### Itinerar dan-po-dan
- [ ] `POST /api/trips/{id}/days` — dodavanje dana
- [ ] `POST /api/trips/{id}/days/{dayId}/items` — dodavanje stavke (mjesto, redoslijed, napomena)
- [ ] Frontend: prikaz itinerera po danima (drag-and-drop za redoslijed — opciono za MVP)
- [ ] Prikaz rute na mapi između stavki istog dana (Mapbox Directions)

### Budžet
- [ ] `Expense` CRUD endpoint-i, sve u KM
- [ ] Endpoint koji vraća zbir po kategorijama (smještaj, hrana, prevoz, aktivnosti, ostalo)
- [ ] Frontend: prikaz budžeta sa opcionim EUR prikazom (fiksni kurs 1,95583)

### Smještaj
- [ ] Ručno kuriran `Accommodation` seed dataset (20-40 objekata: Sarajevo, Mostar,
      Neum, Trebinje, Jahorina/Bjelašnica)
- [ ] Endpoint `GET /api/accommodations?destinationId=`
- [ ] Prikaz prijedloga smještaja na stranici destinacije

### UI polish
- [ ] Responzivan dizajn za mobilne uređaje (osnovni nivo — nije V4 native app)
- [ ] Loading i error stanja za sve API pozive

**Definicija završenosti Sedmice 5-6**: korisnik kreira putovanje, dodaje dane
i stavke, prati budžet u KM, vidi prijedloge smještaja — sve kroz responzivan UI.

---

## Sedmica 7-8 — AI asistent i poliranje

### AI provider integracija
- [ ] Odabir provajdera (Anthropic ili OpenAI API) i osnovni klijent u backend-u
- [ ] Definisanje tool schema-a za sve alate iz sekcije 8 specifikacije:
  - [ ] `search_bih_destinations`
  - [ ] `search_places`
  - [ ] `get_weather`
  - [ ] `get_route`
  - [ ] `search_accommodation`
  - [ ] `get_trip` / `update_itinerary`
  - [ ] `add_expense`
- [ ] Endpoint `POST /api/ai/chat` — tool-calling loop (poziv modela → izvršenje alata → povratak rezultata modelu)
- [ ] Čuvanje AI conversation log-a u bazi

### Frontend AI asistent
- [ ] Chat komponenta na stranici putovanja ("Isplaniraj mi 2 dana u Mostaru")
- [ ] Prikaz predloženih izmjena itinerera prije nego korisnik odobri (ne direktan upis)

### Testovi
- [ ] Unit testovi (xUnit): bodovanje preporuka, budžet kalkulacije, validacija itinerera
- [ ] Integration testovi: autorizacija, perzistencija, OSM/Mapbox/Open-Meteo adapteri
- [ ] E2E testovi (Playwright): registracija → preporuka destinacije → kreiranje itinerera → AI izmjena → budžet

### Deployment
- [ ] Frontend deployed na Vercel
- [ ] Backend deployed na Render ili Railway
- [ ] Baza na Supabase ili Neon (managed PostgreSQL + PostGIS)
- [ ] Environment varijable i tajne postavljene u produkciji (ne u kodu)
- [ ] Provjera da cijela aplikacija radi end-to-end u produkciji

### Portfolio materijal
- [ ] README ažuriran sa screenshot-ovima
- [ ] Kratak demo video (2-3 minuta) koji pokazuje ključne tokove
- [ ] CV tekst iz sekcije 17 specifikacije uključen/prilagođen

**Definicija završenosti Sedmice 7-8 (= definicija završenosti cijelog MVP-a)**:
vidi punu listu u README.md, sekcija "Definicija završenosti".

---

## Poslije MVP-a (V2 i dalje — nije dio ovog roadmapa)

Ovi zadaci **namjerno nisu** dio 4-8 sedmičnog plana — dodaju se tek kad MVP radi
i deployovan je:

- [ ] Real-time saradnja (SignalR) — pozivanje saputnika, komentari, glasanje
- [ ] Hotel API integracija (zamjena ručnog dataset-a)
- [ ] Kurirane aktivnosti/ture (rafting Neretva, ture Mostar/Sarajevo)
- [ ] Regionalno proširenje — Hrvatska, Srbija, Crna Gora (ista arhitektura, novi seed podaci)
- [ ] Flight search (Duffel sandbox ili Travelpayouts) — samo ako treba za AI demo
- [ ] Mobile aplikacija (Android/iOS)

---

## Napomena o korištenju ovog fajla sa Codexom

Ovaj fajl **nije** nešto što Codex automatski čita (to je posao AGENTS.md).
Ovo je tvoj lični tracking alat: prije svakog Codex prompta, dođi ovdje, uzmi
sljedeću nečekiranu stavku, i to postane sadržaj tvog zadatka. Kad Codex završi
i ti provjeriš/commituješ, vrati se i čekiraj `[x]`.
