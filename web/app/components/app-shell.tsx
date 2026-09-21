"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useRef, useState, useSyncExternalStore } from "react";
import { getNavigationItems, isProtectedPath, type NavigationKey } from "../lib/navigation";
import { text } from "../lib/i18n";
import { useAuth } from "./auth-provider";
import { LanguageSwitcher } from "./language-switcher";
import { useLanguage } from "./language-provider";
import { shouldRenderNeutralShell } from "./app-shell-state";
import styles from "./app-shell.module.css";

function Logo() {
  return <>
    <svg width="36" height="36" viewBox="0 0 40 40" fill="none" aria-hidden="true"><path d="M5 29 16 11l7 11 4-6 8 13M12 29c5-7 11 7 17 0" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" /></svg>
    <span>Travel Planner <strong>BiH</strong></span>
  </>;
}

function NavigationIcon({ name }: { name: NavigationKey }) {
  const paths: Partial<Record<NavigationKey, React.ReactNode>> = {
    dashboard: <><rect x="3" y="3" width="7" height="7" rx="1" /><rect x="14" y="3" width="7" height="7" rx="1" /><rect x="3" y="14" width="7" height="7" rx="1" /><rect x="14" y="14" width="7" height="7" rx="1" /></>,
    explore: <><circle cx="12" cy="12" r="9" /><path d="m15 9-2 4-4 2 2-4 4-2Z" /></>,
    recommendations: <><path d="M12 3v3m0 12v3M3 12h3m12 0h3" /><circle cx="12" cy="12" r="4" /></>,
    trips: <><path d="M4 7h16v13H4zM8 7V4h8v3" /><path d="M4 12h16" /></>,
    budget: <><rect x="3" y="6" width="18" height="13" rx="2" /><path d="M16 12h5M7 10h4" /></>,
    settings: <><circle cx="12" cy="12" r="3" /><path d="M19 13.5v-3l-2-.7-.7-1.7.9-1.9-2.1-2.1-1.9.9-1.7-.7L10.5 2h-3l-.7 2-1.7.7-1.9-.9-2.1 2.1.9 1.9-.7 1.7L-.7 10v3l2 .7.7 1.7-.9 1.9 2.1 2.1 1.9-.9 1.7.7.7 2h3l.7-2 1.7-.7 1.9.9 2.1-2.1-.9-1.9.7-1.7 2-.7Z" transform="translate(2.25 .5) scale(.8)" /></>,
  };
  return <svg className={styles.navIcon} width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name]}</svg>;
}

function isActive(pathname: string, href: string) {
  if (href === "/explore") return pathname === href || pathname.startsWith("/destinations/");
  return pathname === href || (href !== "/" && pathname.startsWith(`${href}/`));
}

const subscribeToHydration = () => () => {};

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const language = useLanguage();
  const labels = text[language].navigation;
  const { status, user, signOut } = useAuth();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const hydrated = useSyncExternalStore(subscribeToHydration, () => true, () => false);
  const menuButtonRef = useRef<HTMLButtonElement>(null);
  const firstLinkRef = useRef<HTMLAnchorElement>(null);
  const protectedPage = isProtectedPath(pathname);

  useEffect(() => {
    if (status === "guest" && protectedPage) router.replace("/login");
  }, [protectedPage, router, status]);

  useEffect(() => {
    if (!drawerOpen) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      setDrawerOpen(false);
      menuButtonRef.current?.focus();
    };
    document.addEventListener("keydown", onKeyDown);
    firstLinkRef.current?.focus();
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [drawerOpen]);

  function closeDrawer(restoreFocus = false) {
    setDrawerOpen(false);
    if (restoreFocus) menuButtonRef.current?.focus();
  }

  if (shouldRenderNeutralShell(hydrated, status, protectedPage)) {
    return <div className={styles.neutral}><Link href="/" className={styles.neutralBrand}><Logo /></Link><p role="status">{labels.loading}</p></div>;
  }

  if (status === "guest") {
    const items = getNavigationItems(false);
    return <div className={styles.publicShell}>
      <a className={styles.skip} href="#main-content">{text[language].home.skip}</a>
      <header className={styles.publicHeader}>
        <Link href="/" className={styles.brand} aria-label={`Travel Planner BiH — ${labels.home}`}><Logo /></Link>
        <nav className={styles.publicNav} aria-label={labels.main}>
          {items.map((item) => <Link key={item.key} href={item.href} className={`${isActive(pathname, item.href) ? styles.publicActive : ""} ${item.key === "register" ? styles.publicCta : ""}`}>{labels[item.key]}</Link>)}
          <LanguageSwitcher className={styles.languageSwitcher} />
        </nav>
      </header>
      <div id="main-content" className={styles.publicContent}>{children}</div>
    </div>;
  }

  const items = getNavigationItems(true);
  return <div className={styles.appShell}>
    <header className={styles.mobileHeader}>
      <Link href="/dashboard" className={styles.brand}><Logo /></Link>
      <button ref={menuButtonRef} type="button" className={styles.menuButton} aria-expanded={drawerOpen} aria-controls="app-sidebar" aria-label={drawerOpen ? labels.closeMenu : labels.menu} onClick={() => setDrawerOpen((open) => !open)}>
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" aria-hidden="true"><path d={drawerOpen ? "m6 6 12 12M18 6 6 18" : "M4 7h16M4 12h16M4 17h16"} /></svg>
      </button>
    </header>
    <button type="button" className={`${styles.scrim} ${drawerOpen ? styles.scrimVisible : ""}`} tabIndex={drawerOpen ? 0 : -1} aria-label={labels.closeMenu} onClick={() => closeDrawer(true)} />
    <aside id="app-sidebar" className={`${styles.sidebar} ${drawerOpen ? styles.sidebarOpen : ""}`}>
      <Link href="/dashboard" className={`${styles.brand} ${styles.sidebarBrand}`}><Logo /></Link>
      <nav className={styles.appNav} aria-label={labels.main}>
        {items.map((item, index) => <Link ref={index === 0 ? firstLinkRef : undefined} key={item.key} href={item.href} aria-current={isActive(pathname, item.href) ? "page" : undefined} onClick={() => closeDrawer()}><NavigationIcon name={item.key} /><span>{labels[item.key]}</span></Link>)}
      </nav>
      <div className={styles.account}>
        <div className={styles.user}><span aria-hidden="true">{user?.displayName.slice(0, 1).toLocaleUpperCase(language)}</span><div><strong>{user?.displayName}</strong><small>{user?.email}</small></div></div>
        <button type="button" onClick={() => { signOut(); closeDrawer(); router.replace("/"); }}>{labels.logout}</button>
        <LanguageSwitcher className={styles.languageSwitcher} />
      </div>
    </aside>
    <div className={styles.appContent}>{children}</div>
  </div>;
}
