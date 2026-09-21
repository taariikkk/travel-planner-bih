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
  const latitude = destination.latitude;
  const longitude = destination.longitude;
  const hasCoordinates = latitude != null && longitude != null;
  useEffect(() => {
    if (!token || latitude == null || longitude == null || !container.current) return;
    let disposed = false;
    let map: MapboxMap | undefined;
    const markerSet = markers.current;
    const timeout = window.setTimeout(() => { if (!disposed) setStatus("error"); }, 20000);
    import("mapbox-gl").then(({ default: mapboxgl }) => {
      if (disposed || !container.current) return;
      if (!mapboxgl.supported()) { setStatus("error"); return; }
      map = new mapboxgl.Map({ container: container.current, accessToken: token,
        style: "mapbox://styles/mapbox/outdoors-v12", center: [longitude, latitude],
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
      addMarker(destination.id, destination.name, longitude, latitude, true);
      destination.places.forEach(place => addMarker(place.id, place.name, place.longitude, place.latitude, false));
      if (destination.places.length) map.fitBounds(bounds, { padding: 65, maxZoom: 14, duration: 0 });
      map.on("load", () => { if (!disposed) { window.clearTimeout(timeout); setStatus("ready"); } });
      map.on("error", () => { if (!disposed) setStatus("error"); });
    }).catch(() => { if (!disposed) setStatus("error"); });
    return () => { disposed = true; window.clearTimeout(timeout); markerSet.clear(); map?.remove(); mapRef.current = null; };
  }, [destination, latitude, longitude, token, attempt]);

  return <div className={styles.mapGrid}>
    <div>
      <div className={styles.mapWrap}>
        <div ref={container} className={styles.map} role="region" aria-label={language === "en" ? `Map of ${destination.name}` : `Mapa destinacije ${destination.name}`} />
        {(!token || status !== "ready") && <div className={styles.mapStatus} role="status">
          {!hasCoordinates ? <p>{text[language].destinationExtras.mapCoordinatesEmpty}</p> : !token || status === "error" ? <><p>{labels.mapUnavailable}</p><p>{labels.mapFallback}</p>{token && <button onClick={() => { setStatus("loading"); setAttempt(value => value + 1); }}>{labels.retry}</button>}</> : <p>{labels.mapLoading}</p>}
        </div>}
      </div>
      <p className={styles.legend}><span className={styles.legendDestination} /> {labels.mapDestination} <span className={styles.legendPlace} /> {labels.nearby}</p>
    </div>
  </div>;
}
