import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { cookies } from "next/headers";
import { apiFetch } from "../../lib/api";
import { formatTag, getLanguage, LANGUAGE_COOKIE, text } from "../../lib/i18n";
import DestinationMap from "./destination-map";
import type { Destination } from "./types";
import styles from "./destination.module.css";

export default async function DestinationPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const language = getLanguage((await cookies()).get(LANGUAGE_COOKIE)?.value);
  const labels = text[language].destination;
  const extra = text[language].destinationExtras;
  const number = new Intl.NumberFormat(language, { maximumFractionDigits: 1 });
  const response = await apiFetch(`/api/destinations/${encodeURIComponent(slug)}?language=${language}`, { cache: "no-store" });
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
    </header>
    <main className={styles.main}>
      <nav className={styles.breadcrumb} aria-label={labels.breadcrumb}><Link href="/">{labels.home}</Link><span aria-hidden="true">/</span><Link href="/recommendations">{labels.recommendations}</Link><span aria-hidden="true">/</span><span>{destination.name}</span></nav>
      <section className={`${styles.hero} ${!hasPhoto ? styles.textHero : ""}`} aria-labelledby="destination-title">
        <div className={styles.heroCopy}>
          <p className={styles.region}>{destination.region} · {labels.country}</p>
          <h1 id="destination-title">{destination.name}</h1>
          <ul className={styles.tags} aria-label={labels.interests}>{destination.tags.map(tag => <li key={tag}>{formatTag(tag, language)}</li>)}</ul>
          <dl className={styles.facts} aria-label={extra.facts}>
            <div><dt>{labels.stay}</dt><dd>{destination.suggestedStayMinDays}–{destination.suggestedStayMaxDays} {labels.days}</dd></div>
            <div><dt>{labels.bestTime}</dt><dd>{destination.bestTimeToVisit}</dd></div>
            {destination.slug !== "sarajevo" && destination.distanceFromSarajevoKm != null && <div><dt>{extra.distance}</dt><dd>{number.format(destination.distanceFromSarajevoKm)} km</dd></div>}
            {destination.elevationMeters != null && <div><dt>{extra.elevation}</dt><dd>{number.format(destination.elevationMeters)} m</dd></div>}
            {destination.averageTemperatureC != null && <div><dt>{extra.temperature}</dt><dd>{number.format(destination.averageTemperatureC)} °C</dd></div>}
          </dl>
          <a href="#mapa" className={styles.action}>{labels.mapAction} <span aria-hidden="true">↓</span></a>
        </div>
        {hasPhoto && <figure className={styles.photo}>
          <div className={styles.photoFrame}><Image src="/mostar-yu-siang-teo.jpg" alt={labels.photoAlt} fill preload sizes="(max-width: 760px) 88vw, 52vw" className={styles.image} /></div>
          <figcaption>{labels.photoCaption}</figcaption>
        </figure>}
      </section>
      {/* Assumption: a neutral label keeps every destination name in nominative, without a declension catalog. */}
      <section className={styles.overview} aria-labelledby="overview-title"><h2 id="overview-title">{extra.overview} {destination.name}</h2><p>{destination.description}</p></section>
      <section id="mapa" className={styles.mapSection} aria-labelledby="map-title">
        <div className={styles.mapHeading}><h2 id="map-title">{labels.mapTitle}</h2><p>{labels.mapIntro}</p></div>
        <DestinationMap destination={destination} token={publicToken} language={language} />
      </section>
      <section className={styles.overview} aria-labelledby="weather-title">
        <h2 id="weather-title">{extra.weather}</h2><p>{extra.weatherEmpty}</p>
      </section>
      <section className={styles.overview} aria-labelledby="accommodation-title">
        <h2 id="accommodation-title">{extra.accommodation}</h2><p>{extra.accommodationEmpty}</p>
      </section>
      <div className={styles.planning}>
        <button className={styles.action} disabled aria-describedby="planning-hint">{extra.plan}</button>
        <p id="planning-hint">{extra.planHint}</p>
      </div>
    </main>
    <footer className={styles.footer}><span>Travel Planner BiH</span><Link href="/recommendations">{labels.continue}</Link></footer>
  </div>;
}
