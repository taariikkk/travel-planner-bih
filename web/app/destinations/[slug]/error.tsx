"use client";
import styles from "./destination.module.css";
export default function ErrorPage({ reset }: { reset: () => void }) {
  return <main className={`${styles.page} ${styles.message}`}><h1>Destinaciju trenutno ne možemo učitati.</h1><p>Pokušaj ponovo za nekoliko trenutaka.</p><button className={styles.action} onClick={reset}>Pokušaj ponovo</button></main>;
}
