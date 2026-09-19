# Roadmap — Travel Planner BiH (solo developer, 4-8 sedmica)

* Ne koristi provjere u Chrome-u ili slično, nije instaliran plugin *

Ovo je radni checklist. Čekiraj `[x]` kako završavaš zadatke. Svaki zadatak je
namjerno dovoljno mali da bude **jedan Codex prompt** — ako ti se čini prevelik,
razbij ga dalje prije nego ga daš Codexu.

Redoslijed unutar sedmice je preporučen, ali ne strogo obavezan — neki zadaci
(npr. seed podataka) mogu ići paralelno sa drugima.

**Pravila koja važe za sve zadatke** (detalji u `AGENTS.md`):
unit test uz svaku cjelinu, sadržaj destinacija ide u bazu (uvoz ili
`Source = manual`), postojeći vizuelni stil se ne mijenja, svi tekstovi kroz
BS/EN i18n, nove izmjene modela samo kroz nove migracije.

---

## Sedmica 1-2 — Temelj

### Infrastruktura
- [x] Next.js + TypeScript + Tailwind scaffold (`web/`)
- [x] ASP.NET Core Web API scaffold (`api/`)
- [x] Docker Compose sa PostgreSQL + PostGIS
- [ ] `.env.example` i `appsettings.Development.json.example` šabloni (bez pravih vrijednosti) commitovani
- [ ] GitHub Actions CI: build backenda, `dotnet test`, lint/typecheck frontenda

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

Napomena: ovaj seed postaje **kurirano jezgro** (`Source = manual`). Širina
pokrivenosti (sela, planine, ostali gradovi) dolazi kroz uvoz iz API-ja, vidi
Sedmicu 3-4.

**Definicija završenosti Sedmice 1-2**: korisnik se može registrovati, prijaviti,
podesiti preferencije; baza ima 10-15 destinacija; migracije rade bez grešaka.

---

## Sedmica 3-4 — Discovery, podaci i mape

### Wizard preferencija i scoring
- [x] Frontend: wizard komponenta (interesovanja, budžet, broj dana, sezona)
- [x] Backend: `search_bih_destinations` logika — scoring algoritam koji upoređuje
      preferencije korisnika sa tagovima destinacija
- [x] Endpoint `POST /api/destinations/recommend` vraća rangiranu listu sa
      objašnjenjem ("Trebinje — odgovara tvom interesu za vino i historiju")
- [ ] Unit testovi za scoring i recommend endpoint (dug: trebali su ići uz funkcionalnost)
- [ ] Objašnjenja preporuka spojiti u jednu prirodnu rečenicu (umjesto ponavljanja
      "Odgovara … i Odgovara …"), na BS i EN

### Stranica destinacije
- [x] Frontend: dinamička ruta `/destinations/[slug]`
- [x] Prikaz: pregled, tipično trajanje boravka, tagovi
- [x] Osnovna integracija Mapbox mape (glavni marker destinacije, markeri
      obližnjih `Place` zapisa kad postoje)
- [ ] Brze činjenice: nadmorska visina, udaljenost od Sarajeva (PostGIS), najbolja
      sezona, prosječna temperatura
- [ ] Lista "top mjesta" u blizini (iz `Place`)
- [ ] Prazna mjesta (skeleton) za prognozu, smještaj i dugme "Isplaniraj mi 2 dana"
      koja se povezuju u kasnijim sedmicama
- [ ] Ispravke sadržaja: naslov "Upoznaj Bjelašnicu" (gramatika), regija Bjelašnice
      (Sarajevski kanton, ne Centralna Bosna)

### Navigacija i layout
- [ ] Zajednički layout (Next.js) sa navbarom na svim stranicama
- [ ] Javni navbar: Kako funkcioniše, Istraži, Prijava, Kreiraj račun, BS/EN
- [ ] Prijavljeni navbar: Istraži, Preporuke, Moja putovanja, profil/odjava, BS/EN
- [ ] Aktivno stanje linka, hamburger meni na mobilnom
- [ ] "Moja putovanja" ima smisleno prazno stanje dok Trip modul ne postoji

### Model podataka — proširenje (nova migracija, ne mijenjati postojeće)
- [ ] `Destination`: Type (grad/planina/selo/…), Source, ExternalId (Wikidata Q-id),
      ImportedAt, ElevationM, Population, ImageUrl + atribucija (autor, licenca, URL),
      SeasonMonths (mjeseci sezone), IsFeatured
- [ ] Odvojena override polja/flag po polju tako da ručne izmjene nadjačavaju uvoz
- [ ] `SavedPlace` entitet (UserId, DestinationId/PlaceId, CreatedAt)
- [ ] `ShareLink` entitet (TripId, token, dozvola read-only/edit) — za MVP saradnju
- [ ] `AiConversation` i `AiMessage` entiteti (za AI conversation log)
- [ ] PostgreSQL ekstenzije `unaccent` i `pg_trgm` u migraciji

### Uvoz podataka iz API-ja (API-first)
Cilj: širina i činjenice iz API-ja, kvalitet kroz ručni sloj. Uvoz radi na
zahtjev sa TTL kešom, bez Hangfirea.

- [ ] `IDestinationDataProvider` interfejs + `WikidataProvider` (pretraga, koordinate,
      tip, nadmorska visina, stanovnici)
- [ ] `WikipediaSummaryProvider` (opis na bosanskom, fallback engleski; čuva izvor i URL)
- [ ] `IImageProvider` + implementacija (Wikimedia Commons ili Unsplash) sa atribucijom
- [ ] Servis za uvoz: prvi pristup mjestu → povuci, spremi, zabilježi `ImportedAt`;
      osvježi kad TTL istekne
- [ ] Pravilo prioriteta: ručna vrijednost > uvezena; ponovni uvoz nikad ne prepisuje ručna polja
- [ ] Opisan `User-Agent` i poštovanje rate limita za Wikimedia pozive
- [ ] Kontrola kvaliteta: zapis bez opisa ili sa preslabim opisom ne ulazi u
      "popularno/istaknuto"
- [ ] Prikaz atribucije u UI-ju (Wikipedia tekst, slike)
- [ ] Opciono: komanda za pred-uvoz liste popularnih mjesta
- [ ] Unit testovi (mapiranje odgovora, prioritet ručnih polja, TTL) i integration test adaptera

### Pretraga i "Istraži"
- [ ] `GET /api/search?q=` — prvo lokalna baza (`unaccent` + `pg_trgm`), zatim Wikidata
      fallback ako je rezultata premalo; filter po tipu
- [ ] Test: "Bjelasnica" pronalazi "Bjelašnica"
- [ ] `GET /api/destinations/popular?month=` — sezona iz tagova i klimatskih podataka
      + `IsFeatured`; izračun sezone (Open-Meteo klimatski prosjeci) i ručna korekcija
- [ ] Frontend: stranica `/explore` — "Popularno ovog mjeseca", search bar, filteri po
      tipu, lista destinacija u postojećem editorijalnom stilu (bez kartica sa sjenkama)
- [ ] Prikaz punog opisa i slike destinacije sa atribucijom
- [ ] Dugme "Dodaj u plan" (skriveno/onemogućeno dok Trip modul ne postoji)
- [ ] Spremanje omiljenih: `SavedPlace` endpointi (dodaj/ukloni/lista) i UI (zahtijeva prijavu)
- [ ] Loading, prazna i error stanja; BS/EN

### Mapbox — puni potencijal (novo, prošireno nakon prvog prolaza)
Osnovna mapa radi (marker Mostara, "Oko tebe 0" ispravno prikazuje prazno
stanje dok `Place` tabela nije popunjena). Ovi zadaci nadograđuju taj isti
prikaz da iskoristi više Mapbox mogućnosti, bez izmjene backend logike:

- [ ] Marker clustering — kad destinacija ima puno `Place` zapisa (npr.
      Sarajevo nakon Overpass integracije), grupiše markere u brojeve
      umjesto preklapanja; testirati na destinaciji sa najviše mjesta
- [ ] Popup/tooltip na klik markera — naziv mjesta, kategorija (Atrakcija/
      Restoran), bez napuštanja stranice
- [ ] Fit bounds — mapa se automatski centrira/zumira da prikaže destinaciju
      i sva obližnja mjesta odjednom, umjesto fiksnog zoom nivoa
- [ ] Pripremiti Directions API poziv kao reusable funkciju/servis iza
      `IRoutingProvider` (koristi se kasnije za rutu u itinereru, Sedmica 5-6 —
      ne implementirati punu funkcionalnost ovdje, samo osnovni wrapper)

### OpenStreetMap / Overpass integracija
Napomena: Overpass API ne zahtijeva API ključ niti registraciju — javni
endpoint `https://overpass-api.de/api/interpreter` prima upite direktno.
Ovo je zaseban podatkovni sloj od Mapboxa: Overpass popunjava `Place`
tabelu stvarnim podacima, Mapbox samo prikazuje ono što je već u bazi.
Mapbox rezultati geocodinga se ne pohranjuju u bazu.

- [ ] `IPlacesProvider` interfejs (provider abstraction pattern)
- [ ] `OverpassPlacesProvider` implementacija — upit za restorane/atrakcije po gradu
- [ ] Keširanje Overpass odgovora u bazi (izbjeći ponovljene pozive za istu
      destinaciju — Overpass javni server ima rate limit)
- [ ] Fallback na alternativni Overpass mirror (npr. `overpass.kumi.systems`)
      ako glavni endpoint ne odgovori — opciono, ne blokira MVP
- [ ] Ručno dodane "must-see" lokacije za prioritetne destinacije (30-50 po gradu)
      kao dopuna Overpass podacima gdje su rijetki/nepotpuni (`Source = manual`)

### Open-Meteo integracija
- [ ] `IWeatherProvider` interfejs
- [ ] `OpenMeteoWeatherProvider` implementacija (prognoza + mjesečni klimatski prosjeci)
- [ ] Endpoint `GET /api/weather?lat=&lng=&date=`
- [ ] Keširanje u `WeatherSnapshot` sa osvježavanjem po isteku TTL-a (na zahtjev, bez Hangfirea)
- [ ] Prikaz prognoze na stranici destinacije

**Definicija završenosti Sedmice 3-4**: korisnik odgovori na wizard, dobije
rangirane preporuke sa objašnjenjem, koristi navbar, na stranici "Istraži" vidi
popularna mjesta mjeseca i pretražuje bilo koji grad/planinu/selo, otvori stranicu
destinacije sa opisom, mapom (u dizajn sistemu, sa markerima i clusteringom),
stvarnim obližnjim mjestima iz OSM-a i vremenskom prognozom, i može sačuvati
omiljeno mjesto.

---

## Sedmica 5-6 — Trip planner

### Trip CRUD
- [ ] `POST /api/trips` — kreiranje putovanja (naslov, datumi, broj putnika)
- [ ] `GET /api/trips/{id}` — čitanje sa danima i stavkama itinerera
- [ ] `PUT /api/trips/{id}` — izmjena
- [ ] `DELETE /api/trips/{id}`
- [ ] Autorizacija: korisnik vidi/mijenja samo svoja putovanja
- [ ] Povezati dugme "Dodaj u plan" sa Istraži i stranice destinacije na Trip

### Itinerar dan-po-dan
- [ ] `POST /api/trips/{id}/days` — dodavanje dana
- [ ] `POST /api/trips/{id}/days/{dayId}/items` — dodavanje stavke (mjesto, redoslijed, napomena)
- [ ] Frontend: prikaz itinerera po danima (drag-and-drop za redoslijed — opciono za MVP)
- [ ] Prikaz rute na mapi između stavki istog dana (Mapbox Directions —
      koristi wrapper pripremljen u Sedmici 3-4)
- [ ] Rute i udaljenosti do graničnih gradova (Dubrovnik, Split, Beograd) — opciono

### Budžet
- [ ] `Expense` CRUD endpoint-i, sve u KM
- [ ] Endpoint koji vraća zbir po kategorijama (smještaj, hrana, prevoz, aktivnosti, ostalo)
- [ ] Frontend: prikaz budžeta sa opcionim EUR prikazom (fiksni kurs 1,95583)

### Smještaj
- [ ] `IHotelProvider` interfejs sa implementacijom nad kuriranim datasetom
- [ ] Ručno kuriran `Accommodation` seed dataset (20-40 objekata: Sarajevo, Mostar,
      Neum, Trebinje, Jahorina/Bjelašnica)
- [ ] Endpoint `GET /api/accommodations?destinationId=`
- [ ] Prikaz prijedloga smještaja na stranici destinacije

### Dashboard i dijeljenje
- [ ] Stranica `/dashboard`: nadolazeće putovanje, itinerer, sačuvana mjesta, sažetak budžeta
      (sa smislenim praznim stanjima)
- [ ] Share link ka putovanju (read-only, `ShareLink`) — bez real-time infrastrukture

### UI polish
- [ ] Responzivan dizajn za mobilne uređaje (osnovni nivo — nije V4 native app)
- [ ] Loading i error stanja za sve API pozive

**Definicija završenosti Sedmice 5-6**: korisnik kreira putovanje, dodaje dane
i stavke, prati budžet u KM, vidi prijedloge smještaja i dashboard, može podijeliti
putovanje linkom — sve kroz responzivan UI.

---

## Sedmica 7-8 — AI asistent i poliranje

### AI provider integracija
- [ ] Odabir provajdera (Anthropic ili OpenAI API) i osnovni klijent u backend-u iza `IAiProvider`
- [ ] Definisanje tool schema-a za sve alate iz sekcije 8 specifikacije:
  - [ ] `search_bih_destinations`
  - [ ] `search_places`
  - [ ] `get_weather`
  - [ ] `get_route`
  - [ ] `search_accommodation`
  - [ ] `get_trip` / `update_itinerary`
  - [ ] `add_expense`
- [ ] Endpoint `POST /api/ai/chat` — tool-calling loop (poziv modela → izvršenje alata → povratak rezultata modelu)
- [ ] Čuvanje AI conversation log-a u bazi (`AiConversation`, `AiMessage`)

### Frontend AI asistent
- [ ] Chat komponenta na stranici putovanja ("Isplaniraj mi 2 dana u Mostaru")
- [ ] Dugme "Isplaniraj mi 2 dana" na stranici destinacije pokreće asistenta
- [ ] Prikaz predloženih izmjena itinerera prije nego korisnik odobri (ne direktan upis)

### Testovi
Unit testovi se pišu uz svaku cjelinu (vidi pravila iznad). Ovdje ostaju
integration i E2E, i provjera da nema duga:
- [ ] Integration testovi: autorizacija, perzistencija, OSM/Mapbox/Open-Meteo/Wikidata adapteri
- [ ] E2E testovi (Playwright): registracija → Istraži/pretraga → preporuka destinacije → kreiranje itinerera → AI izmjena → budžet
- [ ] Pregled: svaki endpoint ima unit test

### Deployment
- [ ] Frontend deployed na Vercel
- [ ] Backend deployed na Render ili Railway
- [ ] Baza na Supabase ili Neon (managed PostgreSQL + PostGIS, uključujući `unaccent` i `pg_trgm`)
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
- [ ] Popularnost iz stvarnih podataka (broj pregleda/spremanja) kao dodatni signal
- [ ] Background uvoz/osvježavanje podataka (Hangfire)
- [ ] Regionalno proširenje — Hrvatska, Srbija, Crna Gora (ista arhitektura, novi seed podaci)
- [ ] Flight search (Duffel sandbox ili Travelpayouts) — samo ako treba za AI demo
- [ ] Mobile aplikacija (Android/iOS)
- [ ] Mapbox Isochrone API ("šta je dostupno za X minuta odavde") — napredna
      funkcija, nije potrebna za MVP

---

## Napomena o korištenju ovog fajla sa Codexom

Ovaj fajl **nije** nešto što Codex automatski čita (to je posao AGENTS.md).
Ovo je tvoj lični tracking alat: prije svakog Codex prompta, dođi ovdje, uzmi
sljedeću nečekiranu stavku, i to postane sadržaj tvog zadatka. Kad Codex završi
i ti provjeriš/commituješ, vrati se i čekiraj `[x]`.