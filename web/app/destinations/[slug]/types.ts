export type Place = { id: string; name: string; category: string; latitude: number; longitude: number };
export type Destination = {
  id: string; slug: string; name: string; region: string; description: string;
  bestTimeToVisit: string; suggestedStayMinDays: number; suggestedStayMaxDays: number;
  tags: string[]; latitude: number; longitude: number; places: Place[];
};
