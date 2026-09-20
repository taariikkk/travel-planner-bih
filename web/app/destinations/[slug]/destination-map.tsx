"use client";

import { useEffect, useRef, useState } from "react";
import type { Map as MapboxMap, Marker } from "mapbox-gl";
import "mapbox-gl/dist/mapbox-gl.css";
import type { Destination } from "./types";
import { text, type Language } from "../../lib/i18n";
import styles from "./destination.module.css";

export default function DestinationMap({ destination, token, language }: { destination: Destination; token: string | null; language: Language }) {
  const labels = text[language].destination;
  const container = useRef<HTMLDivElement>(null);
  const mapRef = useRef<MapboxMap | null>(null);
  const markers = useRef(new Map<string, Marker>());
  const [status, setStatus] = useState<"loading" | "ready" | "error">("loading");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    if (!token || !container.current) return;
    let disposed = false;
    let map: MapboxMap | undefined;
    const markerSet = markers.current;
    const timeout = window.setTimeout(() => { if (!disposed) setStatus("error"); }, 20000);
    import("mapbox-gl").then(({ default: mapboxgl }) => {
      if (disposed || !container.current) return;
      if (!mapboxgl.supported()) { setStatus("error"); return; }
      map = new mapboxgl.Map({ container: container.current, accessToken: token,
        style: "mapbox://styles/mapbox/outdoors-v12", center: [destination.longitude, destination.latitude],
        zoom: 13, cooperativeGestures: true });
      mapRef.current = map;
      map.addControl(new mapboxgl.NavigationControl({ showCompass: false }), "top-right");
      const bounds = new mapboxgl.LngLatBounds();
      const addMarker = (id: string, name: string, longitude: number, latitude: number, primary: boolean) => {
        if (!map) return;
        const button = document.createElement("button");
        button.type = "button";
        button.className = primary ? styles.destinationPin : styles.placePin;
        button.setAttribute("aria-label", name);
        button.title = name;
        const marker = new mapboxgl.Marker({ element: button }).setLngLat([longitude, latitude])
          .setPopup(new mapboxgl.Popup({ offset: 20 }).setText(name)).addTo(map);
        markerSet.set(id, marker);
        bounds.extend([longitude, latitude]);
      };
      addMarker(destination.id, destination.name, destination.longitude, destination.latitude, true);
      destination.places.forEach(place => addMarker(place.id, place.name, place.longitude, place.latitude, false));
      if (destination.places.length) map.fitBounds(bounds, { padding: 65, maxZoom: 14, duration: 0 });
      map.on("load", () => { if (!disposed) { window.clearTimeout(timeout); setStatus("ready"); } });
      map.on("error", () => { if (!disposed) setStatus("error"); });
    }).catch(() => { if (!disposed) setStatus("error"); });
    return () => { disposed = true; window.clearTimeout(timeout); markerSet.clear(); map?.remove(); mapRef.current = null; };
  }, [destination, token, attempt]);

  function focusPlace(id: string, longitude: number, latitude: number) {
    const map = mapRef.current;
    if (!map) return;
    map.easeTo({ center: [longitude, latitude], zoom: 15, duration: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? 0 : 500 });
    markers.current.forEach(marker => { if (marker.getPopup()?.isOpen()) marker.togglePopup(); });
    markers.current.get(id)?.togglePopup();
  }

  return <div className={styles.mapGrid}>
    <div>
      <div className={styles.mapWrap}>
        <div ref={container} className={styles.map} role="region" aria-label={language === "en" ? `Map of ${destination.name}` : `Mapa destinacije ${destination.name}`} />
        {(!token || status !== "ready") && <div className={styles.mapStatus} role="status">
          {!token || status === "error" ? <><p>{labels.mapUnavailable}</p><p>{labels.mapFallback}</p>{token && <button onClick={() => { setStatus("loading"); setAttempt(value => value + 1); }}>{labels.retry}</button>}</> : <p>{labels.mapLoading}</p>}
        </div>}
      </div>
      <p className={styles.legend}><span className={styles.legendDestination} /> {labels.mapDestination} <span className={styles.legendPlace} /> {labels.nearby}</p>
    </div>
    <aside className={styles.places} aria-label={labels.nearby}>
      <h3>{labels.around} <span>{destination.places.length}</span></h3>
      {destination.places.length ? <ul>{destination.places.map(place => <li key={place.id}><button disabled={status !== "ready" || !token} onClick={() => focusPlace(place.id, place.longitude, place.latitude)}><span>{place.name}<small>{(text[language].destinationExtras.categories as Record<string, string>)[place.category] ?? place.category}</small></span><span aria-hidden="true">↗</span></button></li>)}</ul> : <p>{labels.noPlaces}</p>}
    </aside>
  </div>;
}
