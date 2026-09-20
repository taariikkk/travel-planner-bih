export type ImageAttribution = { author: string; license: string; url: string };
export type Place = { id: string; name: string; category: string; latitude: number; longitude: number; imageUrl?: string | null };
export type Destination = {
  id: string; slug: string; name: string; region: string; description: string;
  bestTimeToVisit: string; suggestedStayMinDays: number; suggestedStayMaxDays: number;
  tags: string[]; latitude: number; longitude: number; places: Place[];
  distanceFromSarajevoKm?: number | null; elevationMeters?: number | null; averageTemperatureC?: number | null;
  population?: number | null; imageUrl?: string | null; imageAttribution?: ImageAttribution | null;
};
