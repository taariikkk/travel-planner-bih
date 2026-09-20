import type { Destination } from "./types";
import type { Language } from "../../lib/i18n";

export type DestinationStatistic = { label: string; value: string };

type StatisticLabels = {
  population: string;
  elevation: string;
  distance: string;
  region: string;
};

export function formatDestinationStatistics(
  destination: Pick<Destination, "population" | "elevationMeters" | "distanceFromSarajevoKm" | "region">,
  language: Language,
  labels: StatisticLabels,
): DestinationStatistic[] {
  const number = new Intl.NumberFormat(language, { maximumFractionDigits: 1 });
  const statistics: DestinationStatistic[] = [];

  if (destination.population != null) statistics.push({ label: labels.population, value: number.format(destination.population) });
  if (destination.elevationMeters != null) statistics.push({ label: labels.elevation, value: `${number.format(destination.elevationMeters)} m` });
  if (destination.distanceFromSarajevoKm != null) statistics.push({ label: labels.distance, value: `${number.format(destination.distanceFromSarajevoKm)} km` });
  if (destination.region) statistics.push({ label: labels.region, value: destination.region });

  return statistics;
}
