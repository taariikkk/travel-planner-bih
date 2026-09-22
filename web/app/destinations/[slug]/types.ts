export type ImageAttribution = { author: string; license: string; url: string };
export type TextAttribution = { license: string; url: string };
export type Place = { id: string; name: string; category: string; latitude: number; longitude: number; imageUrl?: string | null; imageAttribution?: ImageAttribution | null;
  metadata?: { descriptionBs: string | null; descriptionEn: string | null; description: string | null;
    descriptionLanguage: string | null; address: string | null; cuisine: string | null; priceLevel: string | null;
    website: string | null; phone: string | null; email: string | null; source: string;
    externalId: string | null; sourceUrl: string | null; importedAt: string | null; lastVerifiedAt: string | null } | null;
};
export type Destination = {
  id: string; slug: string; name: string; region: string; description: string;
  bestTimeToVisit: string; suggestedStayMinDays: number; suggestedStayMaxDays: number;
  tags: string[]; latitude?: number | null; longitude?: number | null; places: Place[];
  distanceFromSarajevoKm?: number | null; elevationMeters?: number | null; averageTemperatureC?: number | null;
  population?: number | null; imageUrl?: string | null; imageAttribution?: ImageAttribution | null;
  descriptionAttribution?: TextAttribution | null; descriptionLanguage?: "bs" | "en" | null;
};
