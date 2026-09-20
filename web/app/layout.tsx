import type { Metadata } from "next";
import { cookies } from "next/headers";
import { Suspense } from "react";
import { AppShell } from "./components/app-shell";
import { AuthProvider } from "./components/auth-provider";
import { LanguageProvider } from "./components/language-provider";
import { getLanguage, LANGUAGE_COOKIE } from "./lib/i18n";
import "./globals.css";

export const metadata: Metadata = {
  title: "Travel Planner BiH",
  description: "Isplaniraj putovanje kroz Bosnu i Hercegovinu.",
};

export default async function RootLayout({ children }: LayoutProps<"/">) {
  const language = getLanguage((await cookies()).get(LANGUAGE_COOKIE)?.value);
  return (
    <html
      lang={language}
      className="h-full antialiased"
    >
      <body className="min-h-full">
        <LanguageProvider language={language}>
          <AuthProvider>
            <Suspense fallback={<div className="min-h-screen bg-[#f5f2eb]" />}><AppShell>{children}</AppShell></Suspense>
          </AuthProvider>
        </LanguageProvider>
      </body>
    </html>
  );
}
