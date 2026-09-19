import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { apiFetch } from "../../lib/api";
import DestinationMap from "./destination-map";
import type { Destination } from "./types";
import styles from "./destination.module.css";

export default async function DestinationPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const response = await apiFetch(`/api/destinations/${encodeURIComponent(slug)}`, { cache: "no-store" });
  if (response.status === 404) notFound();
  if (!response.ok) throw new Error("Destination request failed");
  const destination: Destination = await response.json();
  // Mapbox GL runs in the browser and requires a public pk.* token. Never serialize a secret sk.* token.
  const token = process.env.MAPBOX_ACCESS_TOKEN?.trim();
  const publicToken = token?.startsWith("pk.") ? token : null;
  const hasPhoto = destination.slug === "mostar";
  return <div className={styles.page}>
    <header className={styles.header}>
      <Link href="/" className={styles.brand}>Travel Planner <strong>BiH</strong></Link>
      <Link href="/recommendations">Moje preporuke <span aria-hidden="true">↗</span></Link>
    </header>
    <main className={styles.main}>
      <nav className={styles.breadcrumb} aria-label="Putanja"><Link href="/">Početna</Link><span aria-hidden="true">/</span><span>{destination.name}</span></nav>
      <section className={`${styles.hero} ${!hasPhoto ? styles.textHero : ""}`} aria-labelledby="destination-title">
        <div className={styles.heroCopy}>
          <p className={styles.region}>{destination.region} · Bosna i Hercegovina</p>
          <h1 id="destination-title">{destination.name}</h1>
          <ul className={styles.tags} aria-label="Interesovanja">{destination.tags.map(tag => <li key={tag}>{tag}</li>)}</ul>
          <dl className={styles.facts}>
            <div><dt>Vrijeme za istraživanje</dt><dd>{destination.suggestedStayMinDays}–{destination.suggestedStayMaxDays} dana</dd></div>
            <div><dt>Najbolje vrijeme za posjetu</dt><dd>{destination.bestTimeToVisit}</dd></div>
          </dl>
          <a href="#mapa" className={styles.action}>Istraži na mapi <span aria-hidden="true">↓</span></a>
        </div>
        {hasPhoto && <figure className={styles.photo}>
          <div className={styles.photoFrame}><Image src="/mostar-yu-siang-teo.jpg" alt="Stari most iznad Neretve i kamene kuće Mostara" fill preload sizes="(max-width: 760px) 88vw, 52vw" className={styles.image} /></div>
          <figcaption>Mostar na Neretvi · Fotografija: Yu Siang Teo / Unsplash</figcaption>
        </figure>}
      </section>
      <section className={styles.overview} aria-labelledby="overview-title"><h2 id="overview-title">Upoznaj {destination.name}</h2><p>{destination.description}</p></section>
      <section id="mapa" className={styles.mapSection} aria-labelledby="map-title">
        <div className={styles.mapHeading}><h2 id="map-title">Mjesta u blizini</h2><p>Pronađi svoj sljedeći korak.</p></div>
        <DestinationMap destination={destination} token={publicToken} />
      </section>
    </main>
    <footer className={styles.footer}><span>Travel Planner BiH</span><Link href="/recommendations">Nastavi istraživati ↗</Link></footer>
  </div>;
}
