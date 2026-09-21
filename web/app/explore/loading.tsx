"use client";
import { useLanguage } from "../components/language-provider";
import { text } from "../lib/i18n";
import styles from "./explore.module.css";

export default function Loading() {
  return <main className={`${styles.page} ${styles.message}`}><p role="status">{text[useLanguage()].explore.loading}</p></main>;
}
