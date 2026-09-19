"use client";
import Link from "next/link";
import { useLanguage } from "../../components/language-provider";
import { text } from "../../lib/i18n";
import styles from "./destination.module.css";
export default function NotFound() {
  const t = text[useLanguage()].destination;
  return <main className={`${styles.page} ${styles.message}`}><h1>{t.missingTitle}</h1><p>{t.missingText}</p><Link className={styles.action} href="/recommendations">{t.explore}</Link></main>;
}
