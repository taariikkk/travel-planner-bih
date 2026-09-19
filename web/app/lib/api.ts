export type UserProfile = {
  id: string;
  email: string;
  displayName: string;
  preferences: string[];
};

export type AuthResponse = {
  accessToken: string;
  expiresAt: string;
  user: UserProfile;
};

export type Recommendation = {
  id: string;
  name: string;
  region: string;
  description: string;
  bestTimeToVisit: string;
  tags: string[];
  score: number;
  reasons: RecommendationReasons;
};

export type RecommendationReasons = {
  matchingTags: string[];
  matchesSeason: boolean;
  matchesBudget: boolean;
  matchesDuration: boolean;
};

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5215";

export async function apiFetch(path: string, init: RequestInit = {}, token?: string): Promise<Response> {
  return fetch(`${apiUrl}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}), ...init.headers },
  });
}

export function getErrorMessage(response: Response, fallback: string): Promise<string> {
  return response.json().then((body: { message?: string }) => body.message ?? fallback).catch(() => fallback);
}
