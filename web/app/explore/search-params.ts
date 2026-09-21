export type ExploreSearchParams = {
  q: string;
  type: string;
  region: string;
};

type RawSearchParams = Record<string, string | string[] | undefined>;

function first(value: string | string[] | undefined): string {
  return (Array.isArray(value) ? value[0] : value)?.trim() ?? "";
}

export function parseExploreSearchParams(params: RawSearchParams): ExploreSearchParams {
  return { q: first(params.q), type: first(params.type), region: first(params.region) };
}

export function buildExploreHref(values: Partial<ExploreSearchParams>): string {
  const params = new URLSearchParams();
  for (const key of ["q", "type", "region"] as const) {
    const value = values[key]?.trim();
    if (value) params.set(key, value);
  }
  const query = params.toString();
  return query ? `/explore?${query}` : "/explore";
}
