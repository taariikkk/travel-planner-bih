import Link from "next/link";
import styles from "./destination.module.css";
export default function NotFound() {
  return <main className={`${styles.page} ${styles.message}`}><h1>Destinacija nije pronađena.</h1><p>Provjeri adresu ili pronađi novo mjesto među preporukama.</p><Link className={styles.action} href="/recommendations">Istraži preporuke</Link></main>;
}
