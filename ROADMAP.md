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
- [x] `.env.example` i `appsettings.Development.json.example` šabloni (bez pravih vrijednosti) commitovani
- [x] GitHub Actions CI: build backenda, `dotnet test`, lint/typecheck frontenda

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
- [x] Unit testovi za scoring i recommend endpoint
- [x] Objašnjenja preporuka spojiti u jednu prirodnu rečenicu (umjesto ponavljanja
      "Odgovara … i Odgovara …"), na BS i EN

### Stranica destinacije
- [x] Frontend: dinamička ruta `/destinations/[slug]`
- [x] Prikaz: pregled, tipično trajanje boravka, tagovi
- [x] Osnovna integracija Mapbox mape (glavni marker destinacije, markeri
      obližnjih `Place` zapisa kad postoje)
- [x] Brze činjenice: udaljenost od Sarajeva (PostGIS), najbolja sezona;
      nadmorska visina i prosječna temperatura prikazuju se samo kad postoje podaci
- [x] Lista "top mjesta" u blizini (iz `Place`, najbližih 6)
- [x] Prazna mjesta (skeleton) za prognozu, smještaj i dugme "Isplaniraj mi 2 dana"
      koja se povezuju u kasnijim sedmicama
- [x] Ispravke sadržaja: neutralni naslov "O destinaciji: Bjelašnica", regija Bjelašnice
      (Sarajevski kanton, ne Centralna Bosna)
- [x] Redizajn rasporeda stranice destinacije po uzoru na referencu (u postojećem stilu,
      bez kartica sa sjenkama): hero blok sa slikom, nazivom i osnovnom statistikom
      (stanovnici, nadmorska visina, region, udaljenost od Sarajeva), srce za omiljeno,
      zatim blokovi "O destinaciji", "Vrijeme", "Brze činjenice" i "Top mjesta"
- [x] Prikaz broja stanovnika (kad postoji `Population`)
- [x] "Top mjesta" prikazuju sliku mjesta kad postoji

### Navigacija i layout
Dva layouta u postojećem stilu (krem, zelena, serifni naslovi): javni sa gornjim
navbarom i aplikacijski sa lijevim sidebarom nakon prijave.

- [x] Layout za javne stranice (Next.js) sa gornjim navbarom: Kako funkcioniše,
      Istraži, Prijava, Kreiraj račun, BS/EN
- [x] Aplikacijski layout za prijavljene korisnike sa lijevim sidebarom: Dashboard,
      Destinacije (Istraži), Preporuke, Moja putovanja, Budžet, Postavke; u dnu blok
      sa korisnikom (ime, email, odjava) i BS/EN prekidač
- [x] Aktivno stanje linka; na mobilnom se sidebar pretvara u hamburger/drawer
- [x] Smislena prazna stanja za stavke čiji moduli još ne postoje (Dashboard,
      "Moja putovanja", "Budžet")
- [x] Stranica Postavke: profil i preferencije (koristi postojeći `GET/PUT /api/users/me`)

### Model podataka — proširenje (nova migracija, ne mijenjati postojeće)
Ostale tabele dodaju se tek kad zatrebaju: `ShareLink` u Sedmici 5-6,
`AiConversation`/`AiMessage` u Sedmici 7-8. `Place` i `Accommodation` proširenja
idu uz Overpass, odnosno Smještaj.

- [ ] `Destination`: Type (grad/planina/selo/…), Source, ExternalId (Wikidata Q-id),
      ImportedAt, ElevationM, Population, ImageUrl + atribucija (autor, licenca, URL),
      izvor i jezik opisa
- [ ] Odvojena override polja/flag po polju tako da ručne izmjene nadjačavaju uvoz;
      postojeći seed zapisi dobijaju `Source = manual`
- [ ] PostgreSQL ekstenzije `unaccent` i `pg_trgm` u migraciji (+ trigram indeks na nazivu)
- [ ] `SavedPlace` entitet (UserId, DestinationId/PlaceId, CreatedAt) — uz spremanje omiljenih

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
- [ ] Uvezene destinacije i preporuke: tagovi se izvode iz tipa mjesta, a budžet,
      tipično trajanje i sezona dobijaju podrazumijevane vrijednosti (konfiguracija,
      ne hardkodirano) uz ručnu korekciju; destinacija bez dovoljno podataka ne
      učestvuje u preporukama wizarda
- [ ] Opisan `User-Agent` i poštovanje rate limita za Wikimedia pozive
- [ ] Kontrola kvaliteta: zapis bez opisa ili sa preslabim opisom ne ulazi u preporuke;
      UI prikazuje smisleno prazno stanje umjesto lošeg sadržaja
- [ ] Prikaz slike i atribucije (Wikipedia tekst, slike) i nadmorske visine iz Wikidata
      na stranici destinacije
- [ ] Opciono: komanda za pred-uvoz liste destinacija
- [ ] Unit testovi (mapiranje odgovora, prioritet ručnih polja, TTL) i integration test adaptera

### Pretraga i "Istraži"
- [ ] `GET /api/search?q=` — prvo lokalna baza (`unaccent` + `pg_trgm`), zatim Wikidata
      fallback ako je rezultata premalo; filter po tipu
- [ ] Test: "Bjelasnica" pronalazi "Bjelašnica"
- [ ] Frontend: stranica `/explore` — search bar, filteri po tipu i regiji, lista
      destinacija (kurirane prve, zatim uvezene) u postojećem editorijalnom stilu
      (bez kartica sa sjenkama)
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

- [ ] Proširenje `Place` (nova migracija): opis, adresa, vrsta kuhinje, cjenovni nivo,
      web/kontakt link, `ExternalId` (OSM id), datum zadnje provjere, opciona slika
      sa atribucijom (autor, licenca, URL)
- [ ] `IPlacesProvider` interfejs (provider abstraction pattern)
- [ ] `OverpassPlacesProvider` implementacija — upit za restorane/atrakcije po gradu
- [ ] Keširanje Overpass odgovora u bazi (izbjeći ponovljene pozive za istu
      destinaciju — Overpass javni server ima rate limit)
- [ ] Fallback na alternativni Overpass mirror (npr. `overpass.kumi.systems`)
      ako glavni endpoint ne odgovori — opciono, ne blokira MVP
- [ ] Povezivanje ručnog zapisa sa OSM zapisom preko `ExternalId`: ručni zapis
      nadjačava uvezeni, bez duplikata
- [ ] JSON seeder za ručne lokacije (`manual-places.json`): idempotentan, validira
      ulaz i učitava zapise sa `Source = manual`; podaci žive u JSON fajlu, ne u C# kodu

### Ručni sadržaj (radiš ti, ne Codex)
Ne radi se prije nego što Overpass uvoz radi i vidiš šta nedostaje.

- [ ] Must-see lokacije i restorani (30-50 po prioritetnom gradu) u `manual-places.json`
      — opisi svojim riječima, koordinate iz OSM-a ili sa mape, bez kopiranja tekstova
      sa Google Mapsa, Bookinga ili TripAdvisora
- [ ] Kratki opisi na BS i EN za kurirane destinacije tamo gdje je Wikipedia slaba
- [ ] Izbor glavne slike za kurirane destinacije
- [ ] Korekcija tagova, budžeta i tipičnog trajanja za kurirane destinacije
- [ ] Pregled uvezenih opisa (ispravke ručnim vrijednostima)

### Open-Meteo integracija
- [ ] `IWeatherProvider` interfejs
- [ ] `OpenMeteoWeatherProvider` implementacija (prognoza + mjesečni klimatski prosjeci)
- [ ] Endpoint `GET /api/weather?lat=&lng=&date=`
- [ ] Keširanje u `WeatherSnapshot` sa osvježavanjem po isteku TTL-a (na zahtjev, bez Hangfirea)
- [ ] Prikaz prognoze i prosječne temperature na stranici destinacije

**Definicija završenosti Sedmice 3-4**: korisnik odgovori na wizard, dobije
rangirane preporuke sa objašnjenjem, koristi navigaciju (navbar, nakon prijave sidebar), na stranici "Istraži"
pretražuje bilo koji grad/planinu/selo i pregleda destinacije, otvori stranicu
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
- [ ] Pretraga s datumima na dashboardu (odredište + od/do) kreira putovanje i otvara ga za uređivanje

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
- [ ] Proširenje `Accommodation` (nova migracija): koordinate (PostGIS point), opis,
      slika, oznaka da je cijena okvirna, datum zadnje provjere cijene
- [ ] `IHotelProvider` interfejs sa implementacijom nad kuriranim datasetom
- [ ] JSON seeder za smještaj (`accommodations.json`), idempotentan, sa validacijom
- [ ] Ručno kuriran `Accommodation` dataset (20-40 objekata: Sarajevo, Mostar,
      Neum, Trebinje, Jahorina/Bjelašnica) — radiš ti, opisi svojim riječima
- [ ] Endpoint `GET /api/accommodations?destinationId=`
- [ ] Prikaz prijedloga smještaja na stranici destinacije (i na mapi)

### Dashboard i dijeljenje
- [ ] `GET /api/dashboard` — sažetak za prijavljenog korisnika (nadolazeće putovanje,
      broj sačuvanih mjesta, dana do polaska, ukupni budžet u KM)
- [ ] Stranica `/dashboard` u aplikacijskom layoutu, po uzoru na referencu (u postojećem
      stilu): pozdrav, pretraga s datumima (od/do), kartice sa sažetkom, blokovi
      "Nadolazeće putovanje" (slika, naziv, datumi), "Moj itinerer" (timeline po danima)
      i "Sačuvana mjesta" (sa smislenim praznim stanjima)
- [ ] `ShareLink` entitet (TripId, token, dozvola read-only/edit) i share link ka
      putovanju (read-only) — bez real-time infrastrukture

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
- [ ] `AiConversation` i `AiMessage` entiteti i čuvanje AI conversation log-a u bazi

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
- [ ] Obavijesti (zvonce u zaglavlju aplikacije)
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
