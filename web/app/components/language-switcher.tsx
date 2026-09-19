"use client";

import { useRouter } from "next/navigation";
import { LANGUAGE_COOKIE, type Language } from "../lib/i18n";
import { useLanguage } from "./language-provider";

export function LanguageSwitcher({ className }: { className?: string }) {
  const router = useRouter();
  const language = useLanguage();

  function changeLanguage(nextLanguage: Language) {
    // eslint-disable-next-line react-hooks/immutability -- cookie persistence is the intentional side effect of this click handler.
    document.cookie = `${LANGUAGE_COOKIE}=${nextLanguage}; path=/; max-age=31536000; samesite=lax`;
    router.refresh();
  }

  return <div className={className} role="group" aria-label="Language / Jezik">
    {(["bs", "en"] as const).map((code) => <button key={code} type="button" aria-pressed={language === code} onClick={() => changeLanguage(code)}>{code.toUpperCase()}</button>)}
  </div>;
}
