import Image from "next/image";
import Link from "next/link";
import styles from "./home.module.css";

function Arrow() {
  return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M4 12h15m-6-6 6 6-6 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>;
}

export default function Home() {
  return (
    <div className={styles.home}>
      <a href="#sadrzaj" className={styles.skip}>Preskoči na sadržaj</a>
      <header className={styles.header}>
        <Link href="/" className={styles.brand} aria-label="Travel Planner BiH — početna">
          <svg width="40" height="40" viewBox="0 0 40 40" fill="none" aria-hidden="true"><path d="M5 29 16 11l7 11 4-6 8 13M12 29c5-7 11 7 17 0" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" /></svg>
          <span>Travel Planner <strong>BiH</strong></span>
        </Link>
        <nav className={styles.nav} aria-label="Glavna navigacija">
          <a className={styles.aboutLink} href="#kako-funkcionise">Kako funkcioniše</a>
          <Link href="/login">Prijava</Link>
          <Link href="/register" className={styles.navCta}>Kreiraj račun <Arrow /></Link>
        </nav>
      </header>
      <main id="sadrzaj">
        <section className={styles.hero} aria-labelledby="hero-title">
          <div className={styles.heroCopy}>
            <h1 id="hero-title">Blizu je.<br />A toliko toga<br /><em>tek treba otkriti.</em></h1>
            <p>Upoznaj Bosnu i Hercegovinu svojim tempom. Pronađi mjesta za sebe, od čaršijskih ulica do planinskih staza.</p>
            <Link href="/register" className={styles.primary}>Pronađi svoje putovanje <Arrow /></Link>
            <span className={styles.heroNote}>Tvoja interesovanja. Tvoj budžet. Tvoj ritam.</span>
          </div>
          <figure className={styles.photo}>
            <div className={styles.photoFrame}>
              <Image src="/mostar-yu-siang-teo.jpg" alt="Stari most preko Neretve, kamene kuće Mostara i brdo u pozadini" fill sizes="(max-width: 760px) 100vw, 58vw" preload className={styles.heroImage} />
            </div>
            <figcaption><span>Mostar <span aria-hidden="true">/</span> Hercegovina</span><span>Fotografija: Yu Siang Teo · Unsplash</span></figcaption>
          </figure>
        </section>
        <section className={styles.intro} id="kako-funkcionise" aria-labelledby="intro-title">
          <div className={styles.introHeading}>
            <span className={styles.sectionNumber} aria-hidden="true">01 — PO TVOJOJ MJERI</span>
            <h2 id="intro-title">Dobar početak<br />svakog putovanja.</h2>
            <p>Ne moraš znati odakle krenuti.<br />Kreni od onoga što voliš.</p>
          </div>
          <ol className={styles.steps}>
            <li><span>01</span><div><h3>Reci nam šta te privlači</h3><p>Historija, priroda, hrana ili malo od svega. Odaberi interesovanja, budžet i vrijeme za putovanje.</p></div></li>
            <li><span>02</span><div><h3>Otkrij mjesta koja ti odgovaraju</h3><p>Dobij preporuke iz našeg izbora BiH destinacija, uz objašnjenje zašto bi ti se mogle svidjeti.</p></div></li>
            <li><span>03</span><div><h3>Pronađi svoj sljedeći pravac</h3><p>Uporedi preporuke i promijeni odabir kad poželiš drugačiji doživljaj.</p></div></li>
          </ol>
        </section>
        <aside className={styles.closing}>
          <p>Od Sarajeva do Trebinja.<br /><em>Počni od znatiželje.</em></p>
          <Link href="/register" className={styles.primary}>Počni istraživati <Arrow /></Link>
        </aside>
      </main>
      <footer className={styles.footer}><span>Travel Planner BiH</span><span>Putovanja počinju bliže nego što misliš.</span><Link href="/login">Već imaš račun? Prijavi se <Arrow /></Link></footer>
    </div>
  );
}
