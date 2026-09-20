export type NavigationKey =
  | "how"
  | "publicExplore"
  | "explore"
  | "login"
  | "register"
  | "dashboard"
  | "recommendations"
  | "trips"
  | "budget"
  | "settings";

export type NavigationItem = {
  key: NavigationKey;
  href: string;
};

const publicNavigation: readonly NavigationItem[] = [
  { key: "how", href: "/#kako-funkcionise" },
  { key: "publicExplore", href: "/explore" },
  { key: "login", href: "/login" },
  { key: "register", href: "/register" },
];

const appNavigation: readonly NavigationItem[] = [
  { key: "dashboard", href: "/dashboard" },
  { key: "explore", href: "/explore" },
  { key: "recommendations", href: "/recommendations" },
  { key: "trips", href: "/trips" },
  { key: "budget", href: "/budget" },
  { key: "settings", href: "/settings" },
];

export function getNavigationItems(isAuthenticated: boolean): NavigationItem[] {
  return [...(isAuthenticated ? appNavigation : publicNavigation)];
}

export function isProtectedPath(pathname: string): boolean {
  return ["/dashboard", "/recommendations", "/trips", "/budget", "/settings"]
    .some((path) => pathname === path || pathname.startsWith(`${path}/`));
}
