# Travel Planner — Bosna i Hercegovina Edition

Full-stack platforma za planiranje putovanja fokusirana na Bosnu i Hercegovinu.
Otkrivanje destinacija (preporuke prema preferencijama, pretraga i pregled
destinacija), planiranje itinerera dan-po-dan, interaktivna mapa sa
rutama, budžetiranje u KM/EUR, i AI asistent koji koristi tool-calling nad
stvarnim podacima o BiH.

> Ovo je namjerno sužena, BiH-only verzija globalnog Travel Planner koncepta —
> ne umanjena verzija vizije, nego pametan prvi korak: izvodljiva za solo
> juniora u 4-8 sedmica, sa potpuno besplatnim skupom provajdera, koristeći
> istu arhitekturu na koju se kasnije regionalno proširuje.

## Sadržaj

- [Zašto BiH kao prvo tržište](#zašto-bih-kao-prvo-tržište)
- [Funkcionalnosti](#funkcionalnosti)
- [Strategija podataka](#strategija-podataka)
- [Tehnološki stack](#tehnološki-stack)
- [Arhitektura](#arhitektura)
- [Eksterne integracije](#eksterne-integracije)
- [Pokretanje projekta](#pokretanje-projekta)
- [Roadmap](#roadmap)
- [Definicija završenosti](#definicija-završenosti-mvp)
- [Monetizacija (buduća faza)](#monetizacija-buduća-faza)

## Zašto BiH kao prvo tržište

Umjesto globalnog obima, MVP se namjerno ograničava na jednu zemlju: manje
edge-case-ova, realniji rok za solo developera, i jasan, demonstrabilan
proizvod za portfolio i tehnički intervju — uz punu mogućnost proširenja na
regiju (Hrvatska, Srbija, Crna Gora) kasnije, koristeći istu arhitekturu.

**Ciljani korisnici**: domaći turisti, dijaspora, regionalni posjetioci
(Hrvatska, Srbija, Crna Gora), strani turisti, grupe koje dijele troškove.

**Geografski obim MVP-a**: cijela BiH je pretraživa kroz uvoz podataka iz
API-ja. Ručno kuriran sadržaj (ručne dopune mjesta, smještaj)
pokriva: Sarajevo, Mostar, Trebinje i Hercegovina, Neum, Jahorina i Bjelašnica,
Banja Luka, Travnik/Počitelj/Višegrad/Jajce.

## Funkcionalnosti

| Oblast | Opis | Faza |
|---|---|---|
| Identity | Registracija, prijava, profil, preferencije | MVP |
| Navigacija | Zajednički navbar na svim stranicama | MVP |
| Lokalizacija | Bosanski i engleski jezik (BS/EN) | MVP |
| Discover | Wizard preferencija i preporuke sa objašnjenjem | MVP |
| Istraži | Pretraga grada/planine/sela, pregled destinacija sa opisima, filteri po tipu i regiji | MVP |
| Destinacija | Opis, brze činjenice, mapa, mjesta u blizini, vrijeme, smještaj | MVP |
| Saved places | Spremanje omiljenih mjesta | MVP |
| Places | Restorani, atrakcije (OSM uvoz + ručna dopuna) | MVP |
| Maps | Mapbox prikaz, rute, udaljenosti unutar BiH i do graničnih gradova (Dubrovnik, Split, Beograd) | MVP |
| Trips | Kreiranje putovanja, datumi, putnici, itinerer | MVP |
| Itinerary | Dani, aktivnosti, ručno uređivanje, prikaz rute | MVP |
| Dashboard | Pregled nakon prijave: putovanje, itinerer, sačuvana mjesta, budžet | MVP |
| Weather | Open-Meteo prognoza za sve BiH lokacije | MVP |
| Budget | Procjena i praćenje troškova u KM/EUR | MVP |
| AI asistent | Tool-calling nad podacima iz baze + live mapa/vrijeme | MVP |
| Smještaj | Ručno kuriran dataset (20-40 objekata) | MVP (API u V2) |
| Saradnja | Share link ka putovanju (MVP); pozivanje saputnika, komentari, glasanje (V2) | MVP / V2 |
| Aktivnosti/ture | Kurirane preporuke (rafting, ture) | V2 |
| Letovi | Van obima — BiH ima samo 3 aerodroma | V3 |
| Mobile | Android/iOS | V4 |

## Strategija podataka

Cilj je najbolji mogući kvalitet uz što manje hardcodiranja. Sadržaj o
destinacijama se ne piše u kod: dolazi iz API-ja (širina i činjenice) i iz
malog ručnog sloja (kvalitet i ispravke), a sve se čuva u vlastitoj bazi.

| Podatak | Izvor |
|---|---|
| Pronalaženje mjesta, koordinate, tip, nadmorska visina, stanovnici | Wikidata |
| Opisi | Wikipedia (bosanski, uz fallback na engleski) |
| Slike | Wikimedia Commons / Unsplash, uz atribuciju |
| Restorani, atrakcije, vrhovi | OpenStreetMap / Overpass |
| Vrijeme i klimatski prosjeci | Open-Meteo |
| Smještaj | Ručno kuriran dataset |
| Ispravke opisa, tagovi i parametri kuriranih destinacija, dopuna mjesta | Ručno |

Principi:

- **Ručna vrijednost ima prednost** nad uvezenom i nikad se ne prepisuje ponovnim uvozom.
- **Uvoz na zahtjev sa keširanjem** (TTL) u bazi — bez background poslova u MVP-u.
- **Atribucija** izvora (Wikipedia, Commons, Unsplash) prikazana u UI-ju.
- **Kontrola kvaliteta**: slab ili prazan sadržaj se ne izlaže u preporukama.
- **Pretraga** ignoriše dijakritike (č/ć/š/đ/ž) i prvo koristi lokalnu bazu.
- **Ručni podaci** (mjesta, smještaj) čuvaju se u JSON fajlovima u repou i učitava ih
  seeder; ručni zapis nadjačava uvezeni.

## Tehnološki stack

| Sloj | Tehnologija |
|---|---|
| Web | Next.js + TypeScript |
| UI | React + Tailwind CSS |
| Backend | ASP.NET Core / C# |
| ORM/DB | EF Core + PostgreSQL + PostGIS |
| Cache/Realtime | Redis / SignalR *(odloženo za V2)* |
| Maps | Mapbox |
| Podaci o mjestima | Wikidata + Wikipedia |
| Places | OpenStreetMap (Overpass API) |
| Weather | Open-Meteo |
| AI | Anthropic ili OpenAI API |
| CI | GitHub Actions |
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
├── Users / Trips / Itineraries / SavedPlaces
├── BiH Destinations / Places (uvoz + ručni sloj)
└── AI conversation log

Eksterni provajderi (svi iza interfejsa):
Mapbox • Wikidata/Wikipedia • Overpass • Open-Meteo • AI provajder
```

Svi eksterni provajderi stoje iza interfejsa (provider abstraction pattern),
lako zamjenjivi za plaćenu alternativu kad projekat generiše prihod.

## Eksterne integracije

| Kategorija | Provajder | Zašto |
|---|---|---|
| Mape/rute | Mapbox | Besplatni tier, bez kartice (aktuelne limite provjeriti na mapbox.com/pricing) |
| Podaci o mjestima | Wikidata + Wikipedia | Besplatno, bez ključa, širok obuhvat sela i planina; traži atribuciju |
| Slike | Wikimedia Commons / Unsplash | Besplatno, uz atribuciju |
| Mjesta | OpenStreetMap / Overpass | Potpuno besplatno, bez ključa, solidna pokrivenost za veće gradove |
| Vrijeme | Open-Meteo | Potpuno besplatno, bez ključa, 10k poziva/dan |
| Smještaj | Ručno kuriran dataset | Amadeus/Kiwi self-service ugašeni 2026 — kuriranje je realna opcija |
| AI | Anthropic ili OpenAI API | Pay-as-you-go, minimalni trošak na MVP obimu |

**Ukupan mjesečni trošak infrastrukture: $0-5** dok projekat ne generiše
saobraćaj koji zahtijeva plaćene planove. AI pozivi su jedini varijabilni trošak.

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
EF migracije ostaju sadržajno nepromijenjene; nove izmjene modela idu kroz nove migracije.
Namespace-ovi postojećih entiteta i konteksta zadržani su radi kompatibilnosti EF snapshot-a.
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
| Sedmica 3-4 | Discovery i mape — wizard, preporuke, navbar, uvoz podataka, Istraži i pretraga, mjesta, vrijeme |
| Sedmica 5-6 | Trip planner — itinerer, budžet, smještaj, dashboard |
| Sedmica 7-8 | AI asistent, testovi, deployment, portfolio materijal |
| Poslije MVP-a | Puna saradnja (V2), hotel API, regionalno proširenje |

## Definicija završenosti (MVP)

- Korisnik se može registrovati i podesiti preferencije
- Sistem preporučuje BiH destinacije sa objašnjenjem
- Stranica "Istraži" omogućava pretragu grada, planine ili sela i pregled destinacija
- Korisnik može otvoriti stranicu destinacije sa opisom, mapom, mjestima i prognozom
- Korisnik može sačuvati omiljena mjesta
- Korisnik može kreirati putovanje i itinerer dan-po-dan
- Rute i udaljenosti između gradova su prikazane na mapi
- Vremenska prognoza je dostupna za odabrane datume/lokacije
- Korisnik može pratiti budžet u KM
- AI asistent može predložiti i izmijeniti itinerer koristeći stvarne BiH podatke
- Aplikacija je dvojezična (BS/EN) i ima zajednički navbar
- Aplikacija radi produkcijski (deployed), uz minimalne troškove
- README i demo materijal jasno pokazuju BiH fokus kao svjesnu odluku

## Napomene za stranicu destinacije

Pretpostavka: „O destinaciji: {ime}” / „About: {name}” koristi nominativ i ne
zahtijeva ručnu deklinaciju. Obližnja mjesta su zapisi vezani za destinaciju,
sortirani PostGIS udaljenošću od postojećeg centra mape, najviše šest.
Udaljenost od Sarajeva računa se preko geography/WGS84 u kilometrima, zračnom
linijom. Visina i prosječna temperatura su nullable polja odgovora i ostaju
skriveni dok ne postoji izvor podataka; prognoza i smještaj su prazna stanja.

Migracija `CorrectBjelasnicaRegion` ispravlja regiju postojećeg seed zapisa.
PostGIS testovi koriste `TEST_POSTGIS_CONNECTION` (baza sa migracijama i seed
destinacijama). Test liste koristi privremenu tabelu i rollback, bez izmjene
stvarnih mjesta. Bez te varijable integracijski testovi se preskaču; unit test
SQL izraza i jedinica izvršava se uvijek.

## Monetizacija (buduća faza)

- Freemium model prilagođen BiH kupovnoj moći (5-10 KM/mjesečno Premium)
- Affiliate saradnja sa domaćim hotelima/pansionima i turističkim agencijama
- Sponzorisane preporuke restorana/tura, jasno označene
- Saradnja sa turističkim zajednicama gradova (Sarajevo, Mostar, Trebinje)

---

*Radni dokument • 2026 • Izveden iz globalne specifikacije Travel Planner projekta.*