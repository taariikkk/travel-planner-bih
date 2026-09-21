const DEFAULT_RETURN_TO = "/recommendations";

export function getSafeReturnTo(value: string | null): string {
  if (!value?.startsWith("/") || value.startsWith("//") || value.startsWith("/\\")) return DEFAULT_RETURN_TO;
  return value;
}

export function getAuthModeHref(pathname: "/login" | "/register", returnTo: string): string {
  return returnTo === DEFAULT_RETURN_TO ? pathname : `${pathname}?returnTo=${encodeURIComponent(returnTo)}`;
}
