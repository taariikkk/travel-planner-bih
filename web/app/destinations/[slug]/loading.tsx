"use client";
import { useLanguage } from "../../components/language-provider";
import { text } from "../../lib/i18n";
import styles from "./destination.module.css";
export default function Loading() {
  return <main className={`${styles.page} ${styles.message}`}><p role="status">{text[useLanguage()].destination.loading}</p></main>;
}
