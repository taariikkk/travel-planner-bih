"use client";

import { useEffect, useRef, useState } from "react";
import type { GeoJSONSource, Map as MapboxMap } from "mapbox-gl";
import "mapbox-gl/dist/mapbox-gl.css";
import type { Destination, Place } from "./types";
import { apiFetch } from "../../lib/api";
import { text, type Language } from "../../lib/i18n";
import { createClusteredPlaceSource, getMapBoundsCoordinates } from "./destination-map-data";
import styles from "./destination.module.css";

const PLACE_SOURCE = "destination-places";
const CLUSTER_LAYER = "place-clusters";
const CLUSTER_COUNT_LAYER = "place-cluster-count";
const PLACE_LAYER = "unclustered-places";

export default function DestinationMap({ destination, token, language }: { destination: Destination; token: string | null; language: Language }) {
  const labels = text[language].destination;
  const extra = text[language].destinationExtras;
  const container = useRef<HTMLDivElement>(null);
  const [status, setStatus] = useState<"loading" | "ready" | "error">("loading");
  const [attempt, setAttempt] = useState(0);
  const latitude = destination.latitude;
  const longitude = destination.longitude;
  const hasCoordinates = latitude != null && longitude != null;
  useEffect(() => {
    if (!token || latitude == null || longitude == null || !container.current) return;
    let disposed = false;
    let map: MapboxMap | undefined;
    const timeout = window.setTimeout(() => { if (!disposed) setStatus("error"); }, 40000);
    Promise.all([
      import("mapbox-gl"),
      apiFetch(`/api/destinations/${encodeURIComponent(destination.slug)}/map-places`, { cache: "no-store" }),
    ]).then(async ([{ default: mapboxgl }, response]) => {
      if (disposed || !container.current) return;
      if (!response.ok) throw new Error("Destination map places request failed");
      const places = (await response.json()) as Place[];
      if (!mapboxgl.supported()) { setStatus("error"); return; }
      map = new mapboxgl.Map({ container: container.current, accessToken: token,
        style: "mapbox://styles/mapbox/outdoors-v12", center: [longitude, latitude],
        zoom: 13, cooperativeGestures: true });
      map.addControl(new mapboxgl.NavigationControl({ showCompass: false }), "top-right");

      const destinationButton = document.createElement("button");
      destinationButton.type = "button";
      destinationButton.className = styles.destinationPin;
      destinationButton.setAttribute("aria-label", destination.name);
      destinationButton.title = destination.name;
      const destinationPopup = document.createElement("div");
      destinationPopup.className = styles.mapPopup;
      const destinationName = document.createElement("strong");
      destinationName.textContent = destination.name;
      const destinationCategory = document.createElement("span");
      destinationCategory.textContent = labels.mapDestination;
      destinationPopup.append(destinationName, destinationCategory);
      new mapboxgl.Marker({ element: destinationButton }).setLngLat([longitude, latitude])
        .setPopup(new mapboxgl.Popup({ offset: 20 }).setDOMContent(destinationPopup)).addTo(map);

      map.on("load", () => {
        if (disposed || !map) return;
        map.addSource(PLACE_SOURCE, {
          ...createClusteredPlaceSource(places),
          ...(places.some((place) => place.metadata?.externalId)
            ? { attribution: `<a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">${extra.osmAttribution}</a>` }
            : {}),
        });
        map.addLayer({
          id: CLUSTER_LAYER, type: "circle", source: PLACE_SOURCE, filter: ["has", "point_count"],
          paint: {
            "circle-color": "#086b59", "circle-stroke-color": "#f5f2eb", "circle-stroke-width": 3,
            "circle-radius": ["step", ["get", "point_count"], 18, 10, 23, 30, 29],
          },
        });
        map.addLayer({
          id: CLUSTER_COUNT_LAYER, type: "symbol", source: PLACE_SOURCE, filter: ["has", "point_count"],
          layout: { "text-field": ["get", "point_count_abbreviated"], "text-size": 12 },
          paint: { "text-color": "#ffffff" },
        });
        map.addLayer({
          id: PLACE_LAYER, type: "circle", source: PLACE_SOURCE, filter: ["!", ["has", "point_count"]],
          paint: { "circle-color": "#f5f2eb", "circle-radius": 8, "circle-stroke-color": "#086b59", "circle-stroke-width": 3 },
        });

        map.on("click", CLUSTER_LAYER, (event) => {
          if (!map) return;
          const feature = map.queryRenderedFeatures(event.point, { layers: [CLUSTER_LAYER] })[0]?.toJSON();
          const clusterId = Number(feature?.properties?.cluster_id);
          if (!Number.isFinite(clusterId)) return;
          (map.getSource(PLACE_SOURCE) as GeoJSONSource).getClusterExpansionZoom(clusterId, (error, zoom) => {
            if (error || zoom == null || !map || feature.geometry.type !== "Point") return;
            map.easeTo({ center: feature.geometry.coordinates as [number, number], zoom });
          });
        });
        map.on("click", PLACE_LAYER, (event) => {
          const feature = event.features?.[0]?.toJSON();
          if (!map || !feature || feature.geometry.type !== "Point") return;
          const popup = document.createElement("div");
          popup.className = styles.mapPopup;
          const name = document.createElement("strong");
          name.textContent = String(feature.properties?.name ?? "");
          const category = document.createElement("span");
          const categoryKey = String(feature.properties?.category ?? "");
          category.textContent = (extra.categories as Record<string, string>)[categoryKey] ?? categoryKey;
          popup.append(name, category);
          new mapboxgl.Popup({ offset: 14 }).setLngLat(feature.geometry.coordinates as [number, number]).setDOMContent(popup).addTo(map);
        });
        for (const layer of [CLUSTER_LAYER, PLACE_LAYER]) {
          map.on("mouseenter", layer, () => { if (map) map.getCanvas().style.cursor = "pointer"; });
          map.on("mouseleave", layer, () => { if (map) map.getCanvas().style.cursor = ""; });
        }

        const coordinates = getMapBoundsCoordinates({ latitude, longitude }, places);
        if (coordinates.length > 1) {
          const bounds = coordinates.reduce((value, coordinate) => value.extend(coordinate), new mapboxgl.LngLatBounds(coordinates[0], coordinates[0]));
          map.fitBounds(bounds, { padding: 65, maxZoom: 14, duration: 0 });
        }
        window.clearTimeout(timeout);
        setStatus("ready");
      });
      map.on("error", () => { if (!disposed) setStatus("error"); });
    }).catch(() => { if (!disposed) setStatus("error"); });
    return () => { disposed = true; window.clearTimeout(timeout); map?.remove(); };
  }, [attempt, destination.id, destination.name, destination.slug, extra.categories, extra.osmAttribution, labels.mapDestination, latitude, longitude, token]);

  return <div className={styles.mapGrid}>
    <div>
      <div className={styles.mapWrap}>
        <div ref={container} className={styles.map} role="region" aria-label={language === "en" ? `Map of ${destination.name}` : `Mapa destinacije ${destination.name}`} />
        {(!token || status !== "ready") && <div className={styles.mapStatus} role="status">
          {!hasCoordinates ? <p>{extra.mapCoordinatesEmpty}</p> : !token || status === "error" ? <><p>{labels.mapUnavailable}</p><p>{labels.mapFallback}</p>{token && <button onClick={() => { setStatus("loading"); setAttempt(value => value + 1); }}>{labels.retry}</button>}</> : <p>{labels.mapLoading}</p>}
        </div>}
      </div>
      <p className={styles.legend}><span className={styles.legendDestination} /> {labels.mapDestination} <span className={styles.legendPlace} /> {labels.nearby}</p>
    </div>
  </div>;
}
