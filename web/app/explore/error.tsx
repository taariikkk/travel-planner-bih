"use client";
import { useLanguage } from "../components/language-provider";
import { text } from "../lib/i18n";
import styles from "./explore.module.css";

export default function ErrorPage({ reset }: { reset: () => void }) {
  const labels = text[useLanguage()].explore;
  return <main className={`${styles.page} ${styles.message}`}><div><h1>{labels.errorTitle}</h1><p>{labels.errorText}</p><button type="button" onClick={reset}>{labels.retry}</button></div></main>;
}
