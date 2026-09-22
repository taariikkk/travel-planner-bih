import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import { cookies } from "next/headers";
import { apiFetch } from "../../lib/api";
import { formatTag, getLanguage, LANGUAGE_COOKIE, text } from "../../lib/i18n";
import DestinationMap from "./destination-map";
import DestinationFavoriteButton from "./destination-favorite-button";
import { formatDestinationStatistics } from "./destination-statistics";
import type { Destination } from "./types";
import styles from "./destination.module.css";

export default async function DestinationPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const language = getLanguage((await cookies()).get(LANGUAGE_COOKIE)?.value);
  const labels = text[language].destination;
  const extra = text[language].destinationExtras;
  const response = await apiFetch(`/api/destinations/${encodeURIComponent(slug)}?language=${language}`, { cache: "no-store" });
  if (response.status === 404) notFound();
  if (!response.ok) throw new Error("Destination request failed");
  const destination: Destination = await response.json();
  // Mapbox GL runs in the browser and requires a public pk.* token. Never serialize a secret sk.* token.
  const token = process.env.MAPBOX_ACCESS_TOKEN?.trim();
  const publicToken = token?.startsWith("pk.") ? token : null;
  const statistics = formatDestinationStatistics(destination, language, extra);
  const number = new Intl.NumberFormat(language, { maximumFractionDigits: 1 });
  const quickFacts = [
    { label: labels.stay, value: `${destination.suggestedStayMinDays}–${destination.suggestedStayMaxDays} ${labels.days}` },
    { label: labels.bestTime, value: destination.bestTimeToVisit },
    ...(destination.averageTemperatureC != null ? [{ label: extra.temperature, value: `${number.format(destination.averageTemperatureC)} °C` }] : []),
  ];
  const hasDescription = destination.description.trim().length > 0;

  return <main className={styles.page}>
    <nav className={styles.breadcrumb} aria-label={labels.breadcrumb}><Link href="/">{labels.home}</Link><span aria-hidden="true">/</span><Link href="/explore">{labels.explore}</Link><span aria-hidden="true">/</span><span>{destination.name}</span></nav>
    <div className={styles.primaryGrid}>
      <section className={styles.hero} aria-labelledby="destination-title">
        <figure className={styles.photo}>
          {destination.imageUrl ? <Image src={destination.imageUrl} alt={extra.destinationPhotoAlt(destination.name)} fill unoptimized sizes="(max-width: 900px) 88vw, 38vw" className={styles.image} /> : <div className={styles.imageFallback} role="img" aria-label={extra.imageFallback}><svg width="72" height="72" viewBox="0 0 80 80" fill="none" aria-hidden="true"><path d="m10 58 22-35 13 21 9-14 16 28M23 58c10-12 21 12 35 0" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" strokeLinejoin="round" /></svg><span>{extra.imageFallback}</span></div>}
          {destination.imageAttribution ? <figcaption><span>{extra.attribution}: {destination.imageAttribution.author} · {destination.imageAttribution.license}</span><a href={destination.imageAttribution.url} target="_blank" rel="noreferrer">↗</a></figcaption> : null}
        </figure>
        <div className={styles.heroCopy}>
          <p className={styles.region}>{destination.region ? `${destination.region} · ` : ""}{labels.country}</p>
          <div className={styles.titleRow}><h1 id="destination-title">{destination.name}</h1><DestinationFavoriteButton destinationId={destination.id} slug={destination.slug} /></div>
          <ul className={styles.tags} aria-label={labels.interests}>{destination.tags.map((tag) => <li key={tag}>{formatTag(tag, language)}</li>)}</ul>
          <dl className={`${styles.statistics} ${statistics.length < 2 ? styles.statisticsCompact : ""}`}>{statistics.map((statistic) => <div key={statistic.label}><dt>{statistic.label}</dt><dd>{statistic.value}</dd></div>)}</dl>
        </div>
      </section>

      <div className={styles.detailColumn}>
        <section className={styles.detailSection} aria-labelledby="overview-title"><span>01</span><div><h2 id="overview-title">{extra.overview}</h2>{hasDescription ? <><p lang={destination.descriptionLanguage ?? language}>{destination.description}</p>{destination.descriptionAttribution ? <p className={styles.attribution}>{extra.descriptionAttribution}: <a href={destination.descriptionAttribution.url} target="_blank" rel="noreferrer">{destination.descriptionAttribution.license} ↗</a></p> : null}</> : <p>{extra.descriptionEmpty}</p>}</div></section>
        <section className={styles.detailSection} aria-labelledby="weather-title"><span>02</span><div><h2 id="weather-title">{extra.weather}</h2><p>{extra.weatherEmpty}</p></div></section>
        <section className={styles.detailSection} aria-labelledby="facts-title"><span>03</span><div><h2 id="facts-title">{extra.facts}</h2>{quickFacts.length ? <dl className={styles.quickFacts}>{quickFacts.map((fact) => <div key={fact.label}><dt>{fact.label}</dt><dd>{fact.value}</dd></div>)}</dl> : <p>{extra.factsEmpty}</p>}</div></section>
        <section className={styles.detailSection} aria-labelledby="places-title"><span>04</span><div><h2 id="places-title">{extra.topPlaces}</h2>{destination.places.length ? <ul className={styles.topPlaces}>{destination.places.slice(0, 6).map((place) => <li key={place.id}>{place.imageUrl && place.imageAttribution ? <figure><Image src={place.imageUrl} alt="" width={76} height={76} unoptimized className={styles.placeImage} /><figcaption className={styles.attribution}><a href={place.imageAttribution.url} target="_blank" rel="noreferrer">{extra.attribution}: {place.imageAttribution.author} · {place.imageAttribution.license}</a></figcaption></figure> : null}<div><h3>{place.name}</h3><p>{(extra.categories as Record<string, string>)[place.category] ?? place.category}</p></div></li>)}</ul> : <p>{extra.topPlacesEmpty}</p>}{destination.places.some((place) => place.metadata?.externalId) ? <p className={styles.attribution}><a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">{extra.osmAttribution}</a></p> : null}</div></section>
      </div>
    </div>

    <section id="mapa" className={styles.mapSection} aria-labelledby="map-title"><div className={styles.mapHeading}><div><span>05</span><h2 id="map-title">{labels.mapTitle}</h2></div><p>{labels.mapIntro}</p></div><DestinationMap destination={destination} token={publicToken} language={language} /></section>
    <section className={styles.utilitySection} aria-labelledby="accommodation-title"><div><span>06</span><h2 id="accommodation-title">{extra.accommodation}</h2><p>{extra.accommodationEmpty}</p></div><div className={styles.plan}><button className={styles.action} disabled aria-describedby="planning-hint">{extra.addToPlan}</button><p id="planning-hint">{extra.addToPlanHint}</p></div></section>
  </main>;
}
