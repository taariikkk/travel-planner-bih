import styles from "./destination.module.css";
export default function Loading() {
  return <main className={`${styles.page} ${styles.message}`}><p role="status">Učitavamo destinaciju…</p></main>;
}
