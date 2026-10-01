"use client";

import { useEffect, useRef, useState } from "react";
import type { GeoJSONSource, Map as MapboxMap, MapMouseEvent, Popup, SymbolLayerSpecification } from "mapbox-gl";
import "mapbox-gl/dist/mapbox-gl.css";
import type { Destination, Place } from "./types";
import { text, type Language } from "../../lib/i18n";
import { createClusteredPlaceSource, createPlaceFeatureCollection, getMapBoundsCoordinates } from "./destination-map-data";
import { getPlaceMarkerKind } from "./place-explorer-data";
import { createMapFocusTracker, MAP_ICON_PATHS, mapAnimationDuration, mapFitPadding, markerSvg, observeMapSize, selectedMapPlace, shouldCloseMapPopup, validMapPlace, type MapFocusRequest, type MapIconKind } from "./map-presentation";
import styles from "./destination-map.module.css";

const SOURCE = "destination-places";
const CLUSTERS = "place-clusters";
const MARKERS = "place-markers";
const SELECTED = "selected-place";
const HOVER = "hovered-place";
const MARKER_LAYERS = [SELECTED, "selected-label", HOVER, MARKERS, "place-labels"];
const GREEN = "#1f4d3a";
const PAPER = "#f5f0e6";

type Props = {
  destination: Destination; token: string | null; language: Language; places: Place[];
  selectedPlaceId?: string | null; onPlaceSelect?: (placeId: string) => void;
  focusRequest?: MapFocusRequest | null; explorer?: boolean;
};

function iconData(places: Place[]) {
  const data = createPlaceFeatureCollection(places);
  return { ...data, features: data.features.map(feature => ({ ...feature, properties: { ...feature.properties, icon: getPlaceMarkerKind(feature.properties.category) } })) };
}

function MapIcon({ kind }: { kind: MapIconKind }) {
  return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={MAP_ICON_PATHS[kind]} /></svg>;
}

export default function DestinationMap({ destination, token, language, places, selectedPlaceId = null, onPlaceSelect, focusRequest = null, explorer = false }: Props) {
  const labels = text[language].destination;
  const extra = text[language].destinationExtras;
  const copy = text[language].map;
  const container = useRef<HTMLDivElement>(null);
  const mapRef = useRef<MapboxMap | null>(null);
  const sync = useRef<(() => void) | null>(null);
  const latest = useRef({ places, selectedPlaceId, onPlaceSelect, focusRequest });
  const [status, setStatus] = useState<"loading" | "ready" | "error">("loading");
  const [attempt, setAttempt] = useState(0);
  const latitude = destination.latitude;
  const longitude = destination.longitude;
  const hasCoordinates = latitude != null && longitude != null && Number.isFinite(latitude) && Number.isFinite(longitude) && Math.abs(latitude) <= 90 && Math.abs(longitude) <= 180;
  const legendKinds = [...new Set(places.filter(validMapPlace).map(place => getPlaceMarkerKind(place.category)))];

  useEffect(() => {
    latest.current = { places, selectedPlaceId, onPlaceSelect, focusRequest };
    sync.current?.();
  }, [places, selectedPlaceId, onPlaceSelect, focusRequest]);

  useEffect(() => {
    if (!token || !hasCoordinates || latitude == null || longitude == null || !container.current) return;
    let disposed = false;
    let map: MapboxMap | undefined;
    let popup: Popup | undefined;
    let tooltip: Popup | undefined;
    let popupPlaceId: string | null = null;
    let localSelection: string | null = null;
    let hoveredId: string | null = null;
    let stopObserving: (() => void) | undefined;
    const takeFocus = createMapFocusTracker();
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    const duration = () => mapAnimationDuration(reducedMotion.matches);
    const timeout = window.setTimeout(() => { if (!disposed) setStatus("error"); }, 40000);

    import("mapbox-gl").then(({ default: mapboxgl }) => {
      if (disposed || !container.current) return;
      if (!mapboxgl.supported()) { window.clearTimeout(timeout); setStatus("error"); return; }
      map = new mapboxgl.Map({
        container: container.current, accessToken: token,
        style: "mapbox://styles/mapbox/standard-satellite",
        config: { basemap: { showRoadsAndTransit: true, showPedestrianRoads: true, showPlaceLabels: true, showRoadLabels: true, showPointOfInterestLabels: false, showTransitLabels: false, font: "Inter", lightPreset: "day" } },
        projection: "mercator", center: [longitude, latitude], zoom: 14,
        pitch: 0, bearing: 0, dragRotate: false, pitchWithRotate: false, touchPitch: false,
        cooperativeGestures: false, scrollZoom: true,
        locale: { "AttributionControl.ToggleAttribution": copy.attribution },
      });
      mapRef.current = map;
      map.touchZoomRotate.disableRotation();
      map.keyboard.disableRotation();
      map.addControl(new mapboxgl.ScaleControl({ maxWidth: 100, unit: "metric" }), "bottom-left");
      stopObserving = observeMapSize(container.current, () => { if (!disposed) map?.resize(); });

      function closePopup() {
        popup?.remove();
        popup = undefined;
        popupPlaceId = null;
      }

      function popupContent(name: string, category: string, kind: MapIconKind, place?: Place) {
        const surface = document.createElement("div");
        surface.className = styles.popupContent;
        const close = document.createElement("button");
        close.type = "button";
        close.className = styles.popupClose;
        close.setAttribute("aria-label", copy.close);
        close.title = copy.close;
        close.textContent = "×";
        close.addEventListener("click", closePopup);
        const micro = document.createElement("div");
        micro.className = styles.popupCategory;
        const icon = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        icon.setAttribute("viewBox", "0 0 24 24");
        icon.setAttribute("aria-hidden", "true");
        const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        path.setAttribute("d", MAP_ICON_PATHS[kind]);
        icon.append(path);
        const categoryText = document.createElement("span");
        categoryText.textContent = category;
        micro.append(icon, categoryText);
        const title = document.createElement("h3");
        title.textContent = name;
        surface.append(close, micro, title);
        if (place?.metadata?.address) {
          const address = document.createElement("p");
          address.textContent = place.metadata.address;
          surface.append(address);
        }
        if (place?.distanceKm != null && Number.isFinite(place.distanceKm) && place.distanceKm >= 0) {
          const distance = document.createElement("p");
          distance.className = styles.popupDistance;
          distance.textContent = extra.placesExplorer.distance(new Intl.NumberFormat(language, { maximumFractionDigits: 1 }).format(place.distanceKm));
          surface.append(distance);
        }
        return surface;
      }

      function showPlacePopup(place: Place) {
        if (!map) return;
        closePopup();
        tooltip?.remove();
        popupPlaceId = place.id;
        const kind = getPlaceMarkerKind(place.category);
        const category = (extra.categories as Record<string, string>)[place.category.toLocaleLowerCase("bs")] ?? copy.other;
        popup = new mapboxgl.Popup({ className: styles.popup, closeButton: false, maxWidth: "280px", offset: [0, -38], focusAfterOpen: false })
          .setLngLat([place.longitude, place.latitude]).setDOMContent(popupContent(place.name, category, kind, place)).addTo(map);
        popup.on("close", () => { popupPlaceId = null; });
      }

      function focusPlace(place: Place) {
        if (!map) return;
        const height = map.getContainer().clientHeight;
        map.easeTo({ center: [place.longitude, place.latitude], zoom: Math.max(map.getZoom(), 15), offset: [0, Math.min(85, height / 5)], duration: duration() });
      }

      const destinationButton = document.createElement("button");
      destinationButton.type = "button";
      destinationButton.className = styles.destinationPin;
      destinationButton.setAttribute("aria-label", destination.name);
      destinationButton.title = destination.name;
      const destinationIcon = document.createElement("img");
      destinationIcon.src = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(markerSvg("destination", true))}`;
      destinationIcon.alt = "";
      const destinationName = document.createElement("span");
      destinationName.textContent = destination.name;
      destinationButton.append(destinationIcon, destinationName);
      destinationButton.addEventListener("click", event => {
        event.stopPropagation();
        if (!map) return;
        closePopup();
        tooltip?.remove();
        popup = new mapboxgl.Popup({ className: styles.popup, closeButton: false, maxWidth: "280px", offset: 38, focusAfterOpen: false })
          .setLngLat([longitude, latitude]).setDOMContent(popupContent(destination.name, labels.mapDestination, "destination")).addTo(map);
      });
      new mapboxgl.Marker({ element: destinationButton, anchor: "bottom" }).setLngLat([longitude, latitude]).addTo(map);

      map.on("load", async () => {
        if (disposed || !map) return;
        try {
          // A small fixed sprite set scales to thousands of GeoJSON points.
          await Promise.all((Object.keys(MAP_ICON_PATHS) as MapIconKind[]).flatMap(kind => [false, true].map(async selected => {
            const image = new Image(80, 88);
            image.src = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(markerSvg(kind, selected))}`;
            await image.decode();
            if (!disposed && map) map.addImage(`${kind}${selected ? "-selected" : ""}`, image, { pixelRatio: 2 });
          })));
          if (disposed || !map) return;
          map.addSource(SOURCE, { ...createClusteredPlaceSource([]), attribution: `<a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">${extra.osmAttribution}</a>` });
          for (const id of [SELECTED, HOVER]) map.addSource(id, { type: "geojson", data: iconData([]) });
          map.addLayer({ id: CLUSTERS, type: "circle", source: SOURCE, filter: ["has", "point_count"], paint: {
            "circle-color": GREEN, "circle-stroke-color": PAPER, "circle-stroke-width": 3,
            "circle-radius": ["step", ["get", "point_count"], 21, 10, 26, 30, 31],
          } });
          map.addLayer({ id: "cluster-count", type: "symbol", source: SOURCE, filter: ["has", "point_count"], layout: { "text-field": ["get", "point_count_abbreviated"], "text-font": ["Inter Medium"], "text-size": 13, "text-allow-overlap": true }, paint: { "text-color": PAPER } });
          const markerLayout: SymbolLayerSpecification["layout"] = { "icon-image": ["get", "icon"], "icon-anchor": "bottom", "icon-size": .8, "icon-allow-overlap": true, "icon-ignore-placement": true };
          const labelLayout: SymbolLayerSpecification["layout"] = { "text-field": ["get", "name"], "text-font": ["Inter Medium"], "text-size": 12, "text-max-width": 12, "text-variable-anchor": ["left", "right", "top"], "text-radial-offset": 1.6, "text-padding": 6 };
          const labelPaint = { "text-color": GREEN, "text-halo-color": PAPER, "text-halo-width": 2 };
          map.addLayer({ id: MARKERS, type: "symbol", source: SOURCE, filter: ["!", ["has", "point_count"]], layout: markerLayout });
          map.addLayer({ id: "place-labels", type: "symbol", source: SOURCE, minzoom: 14, filter: ["!", ["has", "point_count"]], layout: labelLayout, paint: labelPaint });
          map.addLayer({ id: HOVER, type: "symbol", source: HOVER, layout: { ...markerLayout, "icon-size": .95 } });
          map.addLayer({ id: SELECTED, type: "symbol", source: SELECTED, layout: { ...markerLayout, "icon-image": ["concat", ["get", "icon"], "-selected"], "icon-size": 1 } });
          map.addLayer({ id: "selected-label", type: "symbol", source: SELECTED, layout: { ...labelLayout, "text-size": 14, "text-allow-overlap": true, "text-radial-offset": 2.2 }, paint: labelPaint });

          function setSingleSource(id: string, place?: Place) {
            (map?.getSource(id) as GeoJSONSource | undefined)?.setData(iconData(place ? [place] : []));
          }
          function syncSelection() {
            const active = selectedMapPlace(latest.current.places, latest.current.onPlaceSelect ? latest.current.selectedPlaceId : localSelection);
            setSingleSource(SELECTED, active);
            // The selected point has its own source and remains visible within clusters.
            const filter: NonNullable<SymbolLayerSpecification["filter"]> = ["all", ["!", ["has", "point_count"]], ["!=", ["get", "id"], active?.id ?? ""]];
            map?.setFilter(MARKERS, filter);
            map?.setFilter("place-labels", filter);
          }
          function clearHover() {
            if (hoveredId !== null) setSingleSource(HOVER);
            hoveredId = null;
            tooltip?.remove();
            if (map) map.getCanvas().style.cursor = "";
          }
          function hitPlace(event: MapMouseEvent) {
            const feature = map?.queryRenderedFeatures(event.point, { layers: MARKER_LAYERS })[0]?.toJSON();
            return selectedMapPlace(latest.current.places, String(feature?.properties?.id ?? ""));
          }
          map.on("mousemove", event => {
            if (!map) return;
            const place = hitPlace(event);
            if (!place) {
              clearHover();
              if (map.queryRenderedFeatures(event.point, { layers: [CLUSTERS] }).length) map.getCanvas().style.cursor = "pointer";
              return;
            }
            map.getCanvas().style.cursor = "pointer";
            if (hoveredId === place.id) return;
            hoveredId = place.id;
            setSingleSource(HOVER, place);
            tooltip?.remove();
            if (popupPlaceId !== place.id) tooltip = new mapboxgl.Popup({ className: styles.tooltip, closeButton: false, closeOnClick: false, maxWidth: "240px", offset: [0, -36], focusAfterOpen: false })
              .setLngLat([place.longitude, place.latitude]).setText(place.name).addTo(map);
          });
          map.getCanvas().addEventListener("mouseleave", clearHover);
          map.on("movestart", clearHover);
          map.on("click", event => {
            if (!map) return;
            const place = hitPlace(event);
            if (place) {
              localSelection = place.id;
              latest.current.onPlaceSelect?.(place.id);
              if (!latest.current.onPlaceSelect) syncSelection();
              showPlacePopup(place);
              focusPlace(place);
              return;
            }
            const cluster = map.queryRenderedFeatures(event.point, { layers: [CLUSTERS] })[0]?.toJSON();
            const clusterId = Number(cluster?.properties?.cluster_id);
            if (!cluster || !Number.isFinite(clusterId) || cluster.geometry.type !== "Point") return;
            closePopup();
            const center = cluster.geometry.coordinates as [number, number];
            (map.getSource(SOURCE) as GeoJSONSource).getClusterExpansionZoom(clusterId, (error, zoom) => {
              if (!disposed && map && !error && zoom != null) map.easeTo({ center, zoom, duration: duration() });
            });
          });
          let previousPlaces: Place[] | undefined;
          sync.current = () => {
            if (disposed || !map) return;
            const current = latest.current;
            if (previousPlaces !== current.places) {
              previousPlaces = current.places;
              (map.getSource(SOURCE) as GeoJSONSource).setData(iconData(current.places));
              clearHover();
              if (localSelection && !selectedMapPlace(current.places, localSelection)) localSelection = null;
            }
            const activeId = current.onPlaceSelect ? current.selectedPlaceId : localSelection;
            if (shouldCloseMapPopup(popupPlaceId, activeId, current.places)) closePopup();
            syncSelection();
            const focus = takeFocus(current.focusRequest, current.places);
            if (focus) { showPlacePopup(focus); focusPlace(focus); }
          };
          sync.current();
          window.clearTimeout(timeout);
          setStatus("ready");
        } catch {
          if (!disposed) { window.clearTimeout(timeout); setStatus("error"); }
        }
      });
      map.on("error", () => {
        // A failed individual tile must not cover an otherwise usable map.
        if (!disposed && !sync.current) { window.clearTimeout(timeout); setStatus("error"); }
      });
    }).catch(() => { if (!disposed) { window.clearTimeout(timeout); setStatus("error"); } });
    return () => {
      disposed = true;
      sync.current = null;
      mapRef.current = null;
      stopObserving?.();
      window.clearTimeout(timeout);
      tooltip?.remove();
      popup?.remove();
      map?.remove();
    };
  }, [attempt, token, hasCoordinates, latitude, longitude, destination.name, language, labels.mapDestination, extra, copy]);

  function move(action: "in" | "out" | "all" | "reset") {
    const map = mapRef.current;
    if (!map || status !== "ready" || latitude == null || longitude == null) return;
    const duration = mapAnimationDuration(window.matchMedia("(prefers-reduced-motion: reduce)").matches);
    if (action === "in") map.zoomIn({ duration });
    else if (action === "out") map.zoomOut({ duration });
    else if (action === "reset") map.easeTo({ center: [longitude, latitude], zoom: 14, bearing: 0, pitch: 0, duration });
    else {
      const points = getMapBoundsCoordinates({ latitude, longitude }, places);
      const bounds: [[number, number], [number, number]] = [[longitude, latitude], [longitude, latitude]];
      for (const [lng, lat] of points) { bounds[0][0] = Math.min(bounds[0][0], lng); bounds[0][1] = Math.min(bounds[0][1], lat); bounds[1][0] = Math.max(bounds[1][0], lng); bounds[1][1] = Math.max(bounds[1][1], lat); }
      map.fitBounds(bounds, { maxZoom: 15, padding: mapFitPadding(map.getContainer().clientWidth, map.getContainer().clientHeight), retainPadding: false, duration });
    }
  }

  return <div className={`${styles.root} ${explorer ? styles.explorer : ""}`}>
    <div className={styles.frame}>
      <div ref={container} className={styles.canvas} role="region" aria-label={copy.region(destination.name)} />
      {status === "ready" && token && hasCoordinates ? <>
        <span className={styles.badge}>{copy.satellite}</span>
        <div className={styles.controls} role="group" aria-label={copy.controls}>
          <button type="button" title={copy.zoomIn} aria-label={copy.zoomIn} onClick={() => move("in")}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12h14M12 5v14" /></svg></button>
          <button type="button" title={copy.zoomOut} aria-label={copy.zoomOut} onClick={() => move("out")}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12h14" /></svg></button>
          <button type="button" title={copy.showAll} aria-label={copy.showAll} onClick={() => move("all")}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M9 4H4v5m11-5h5v5M4 15v5h5m11-5v5h-5M9 9h6v6H9Z" /></svg></button>
          <button type="button" title={copy.reset} aria-label={copy.reset} onClick={() => move("reset")}><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="6" /><path d="M12 2v4m0 12v4M2 12h4m12 0h4" /><circle cx="12" cy="12" r="1" /></svg></button>
        </div>
      </> : null}
      {(!token || !hasCoordinates || status !== "ready") && <div className={styles.status} role="status">
        {!hasCoordinates ? <p>{extra.mapCoordinatesEmpty}</p> : !token || status === "error" ? <><p>{labels.mapUnavailable}</p><p>{labels.mapFallback}</p>{token && <button type="button" onClick={() => { setStatus("loading"); setAttempt(value => value + 1); }}>{labels.retry}</button>}</> : <p>{labels.mapLoading}</p>}
      </div>}
    </div>
    <ul className={styles.legend} aria-label={copy.legend}>
      <li><MapIcon kind="destination" />{labels.mapDestination}</li>
      {legendKinds.map(kind => <li key={kind}><MapIcon kind={kind} />{kind === "other" ? copy.other : extra.placesExplorer.filters[kind]}</li>)}
    </ul>
  </div>;
}
