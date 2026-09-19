import Image from "next/image";
import Link from "next/link";
import { cookies } from "next/headers";
import { LanguageSwitcher } from "./components/language-switcher";
import { getLanguage, LANGUAGE_COOKIE, text } from "./lib/i18n";
import styles from "./home.module.css";

function Arrow() {
  return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M4 12h15m-6-6 6 6-6 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>;
}

export default async function Home() {
  const language = getLanguage((await cookies()).get(LANGUAGE_COOKIE)?.value);
  const t = text[language].home;
  return (
    <div className={styles.home}>
      <a href="#sadrzaj" className={styles.skip}>{t.skip}</a>
      <header className={styles.header}>
        <Link href="/" className={styles.brand} aria-label={`Travel Planner BiH — ${language === "en" ? "home" : "početna"}`}>
          <svg width="40" height="40" viewBox="0 0 40 40" fill="none" aria-hidden="true"><path d="M5 29 16 11l7 11 4-6 8 13M12 29c5-7 11 7 17 0" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" /></svg>
          <span>Travel Planner <strong>BiH</strong></span>
        </Link>
        <nav className={styles.nav} aria-label={t.nav}>
          <a className={styles.aboutLink} href="#kako-funkcionise">{t.how}</a>
          <Link href="/login">{t.login}</Link>
          <Link href="/register" className={styles.navCta}>{t.register} <Arrow /></Link>
          <LanguageSwitcher className={styles.languageSwitcher} />
        </nav>
      </header>
      <main id="sadrzaj">
        <section className={styles.hero} aria-labelledby="hero-title">
          <div className={styles.heroCopy}>
            <h1 id="hero-title">{t.hero[0]}<br />{t.hero[1]}<br /><em>{t.hero[2]}</em></h1>
            <p>{t.heroDescription}</p>
            <Link href="/register" className={styles.primary}>{t.findTrip} <Arrow /></Link>
            <span className={styles.heroNote}>{t.heroNote}</span>
          </div>
          <figure className={styles.photo}>
            <div className={styles.photoFrame}>
              <Image src="/mostar-yu-siang-teo.jpg" alt={t.photoAlt} fill sizes="(max-width: 760px) 100vw, 58vw" preload className={styles.heroImage} />
            </div>
            <figcaption><span>Mostar <span aria-hidden="true">/</span> Hercegovina</span><span>{t.photoCaption}</span></figcaption>
          </figure>
        </section>
        <section className={styles.intro} id="kako-funkcionise" aria-labelledby="intro-title">
          <div className={styles.introHeading}>
            <span className={styles.sectionNumber} aria-hidden="true">{t.sectionNumber}</span>
            <h2 id="intro-title">{t.introTitle[0]}<br />{t.introTitle[1]}</h2>
            <p>{t.introDescription[0]}<br />{t.introDescription[1]}</p>
          </div>
          <ol className={styles.steps}>
            {t.steps.map(([title, description], index) => <li key={title}><span>{String(index + 1).padStart(2, "0")}</span><div><h3>{title}</h3><p>{description}</p></div></li>)}
          </ol>
        </section>
        <aside className={styles.closing}>
          <p>{t.closing[0]}<br /><em>{t.closing[1]}</em></p>
          <Link href="/register" className={styles.primary}>{t.startExploring} <Arrow /></Link>
        </aside>
      </main>
      <footer className={styles.footer}><span>Travel Planner BiH</span><span>{t.footer}</span><Link href="/login">{t.existingAccount} <Arrow /></Link></footer>
    </div>
  );
}
