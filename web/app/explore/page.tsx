import Image from "next/image";
import Link from "next/link";
import { cookies } from "next/headers";
import { apiFetch } from "../lib/api";
import { formatDestinationType, getLanguage, LANGUAGE_COOKIE, text } from "../lib/i18n";
import { buildExploreHref, parseExploreSearchParams } from "./search-params";
import styles from "./explore.module.css";

type SearchItem = {
  id: string; slug: string; name: string; type: string; region: string;
  description: string; source: string; imageUrl: string | null;
  imageAttribution: { author: string; license: string; url: string } | null;
  descriptionAttribution: { license: string; url: string } | null;
};

type SearchResponse = {
  items: SearchItem[];
  filters: { types: string[]; regions: string[] };
  usedFallback: boolean;
};

export default async function ExplorePage({ searchParams }: PageProps<"/explore">) {
  const language = getLanguage((await cookies()).get(LANGUAGE_COOKIE)?.value);
  const labels = text[language].explore;
  const params = parseExploreSearchParams(await searchParams);
  const apiParams = new URLSearchParams({ language });
  if (params.q) apiParams.set("q", params.q);
  if (params.type) apiParams.set("type", params.type);
  if (params.region) apiParams.set("region", params.region);
  const response = await apiFetch(`/api/search?${apiParams}`, { cache: "no-store" });
  if (!response.ok) throw new Error("Destination search request failed");
  const result: SearchResponse = await response.json();

  return <main className={styles.page}>
    <header className={styles.hero}>
      <p className={styles.number}>{labels.number}</p>
      <h1>{labels.title[0]}<br /><em>{labels.title[1]}</em></h1>
      <p className={styles.intro}>{labels.description}</p>
    </header>

    <section className={styles.searchPanel} aria-label={labels.searchLabel}>
      <form action="/explore" className={styles.searchForm}>
        <label htmlFor="destination-search">{labels.searchLabel}</label>
        <div className={styles.searchRow}>
          <input id="destination-search" name="q" type="search" defaultValue={params.q} maxLength={100} placeholder={labels.searchPlaceholder} />
          {params.type ? <input type="hidden" name="type" value={params.type} /> : null}
          {params.region ? <input type="hidden" name="region" value={params.region} /> : null}
          <button type="submit">{labels.search}<span aria-hidden="true">→</span></button>
        </div>
      </form>

      <div className={styles.typeFilter}>
        <span>{labels.type}</span>
        <div className={styles.chips}>
          <Link className={!params.type ? styles.activeChip : ""} href={buildExploreHref({ q: params.q, region: params.region })}>{labels.allTypes}</Link>
          {result.filters.types.map((type) => <Link key={type} className={params.type === type ? styles.activeChip : ""} href={buildExploreHref({ ...params, type })}>{formatDestinationType(type, language)}</Link>)}
        </div>
      </div>

      <form action="/explore" className={styles.regionFilter}>
        {params.q ? <input type="hidden" name="q" value={params.q} /> : null}
        {params.type ? <input type="hidden" name="type" value={params.type} /> : null}
        <label htmlFor="region">{labels.region}</label>
        <select id="region" name="region" defaultValue={params.region}>
          <option value="">{labels.allRegions}</option>
          {result.filters.regions.map((region) => <option key={region} value={region}>{region}</option>)}
        </select>
        <button type="submit">{labels.applyFilters}</button>
        {params.q || params.type || params.region ? <Link href="/explore">{labels.reset}</Link> : null}
      </form>
    </section>

    <section className={styles.results} aria-labelledby="results-title">
      <div className={styles.resultsHeading}>
        <h2 id="results-title">{labels.results(result.items.length)}</h2>
        {result.usedFallback ? <p>{labels.fallback}</p> : null}
      </div>
      {result.items.length ? <ol className={styles.list}>
        {result.items.map((destination, index) => <li key={destination.id} className={styles.result}>
          <span className={styles.rank}>{String(index + 1).padStart(2, "0")}</span>
          <figure className={styles.figure}>
            {destination.imageUrl ? <Image src={destination.imageUrl} alt={labels.imageAlt(destination.name)} fill unoptimized sizes="(max-width: 700px) 88vw, 240px" className={styles.image} /> : <div className={styles.imageFallback} aria-hidden="true">↗</div>}
            {destination.imageAttribution ? <figcaption>{labels.photo}: <a href={destination.imageAttribution.url} target="_blank" rel="noreferrer">{destination.imageAttribution.author} · {destination.imageAttribution.license}</a></figcaption> : null}
          </figure>
          <div className={styles.copy}>
            <p className={styles.meta}>{formatDestinationType(destination.type, language)}{destination.region ? ` · ${destination.region}` : ""}</p>
            <h3>{destination.name}</h3>
            <p className={styles.source}>{destination.source === "manual" ? labels.curated : labels.imported}</p>
            <p className={styles.description}>{destination.description || text[language].destinationExtras.descriptionEmpty}</p>
            {destination.descriptionAttribution ? <p className={styles.textAttribution}>{text[language].destinationExtras.descriptionAttribution}: <a href={destination.descriptionAttribution.url} target="_blank" rel="noreferrer">{destination.descriptionAttribution.license} ↗</a></p> : null}
            <Link href={`/destinations/${destination.slug}`}>{labels.details}<span aria-hidden="true">↗</span></Link>
          </div>
        </li>)}
      </ol> : <div className={styles.empty}><span>00</span><div><h3>{labels.emptyTitle}</h3><p>{labels.emptyText}</p></div></div>}
    </section>
  </main>;
}
