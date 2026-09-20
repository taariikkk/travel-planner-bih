"use client";

import Link from "next/link";
import { text } from "../lib/i18n";
import { useLanguage } from "./language-provider";
import styles from "./section-page.module.css";

type PageKey = keyof typeof text.bs.pages;

const actionHref: Record<PageKey, string> = {
  dashboard: "/recommendations",
  trips: "/explore",
  budget: "/trips",
  explore: "/recommendations",
};

export function EmptyStatePage({ pageKey }: { pageKey: PageKey }) {
  const copy = text[useLanguage()].pages[pageKey];
  return <main className={styles.page}>
    <section className={styles.intro}>
      <p className={styles.number}>{copy.number}</p>
      <h1>{copy.title[0]}<br /><em>{copy.title[1]}</em></h1>
      <p className={styles.description}>{copy.description}</p>
      <Link href={actionHref[pageKey]} className={styles.action}>{copy.action}<svg aria-hidden="true" width="20" height="20" viewBox="0 0 24 24" fill="none"><path d="M4 12h15m-6-6 6 6-6 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg></Link>
    </section>
    <div className={styles.rule} aria-hidden="true"><span>01</span><span>BiH</span></div>
  </main>;
}
