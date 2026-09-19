"use client";
import { useLanguage } from "../../components/language-provider";
import { text } from "../../lib/i18n";
import styles from "./destination.module.css";
export default function ErrorPage({ reset }: { reset: () => void }) {
  const t = text[useLanguage()].destination;
  return <main className={`${styles.page} ${styles.message}`}><h1>{t.errorTitle}</h1><p>{t.errorText}</p><button className={styles.action} onClick={reset}>{t.retry}</button></main>;
}
