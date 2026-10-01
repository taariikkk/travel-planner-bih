This is a [Next.js](https://nextjs.org) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).

## Getting Started

First, run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

You can start editing the page by modifying `app/page.tsx`. The page auto-updates as you edit the file.

This project serves local Fraunces and Inter font files through `app/fonts.css`; no Google Fonts request is needed.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.


## Presentation redesign — October 2026

The user-supplied `Meditation app design.zip` Figma Make export is the visual reference only. Its `src/app/App.tsx`, `src/styles/theme.css`, `src/styles/fonts.css`, and `src/index.css` define the cream/green palette, Fraunces/Inter typography, 224px sidebar, thin dividers, rectangular photography, compact controls, and editorial sections. Fonts in `public/fonts` are copied from that export. Export scripts and instructions were not adopted as project instructions.

Assumption: pages absent from the reference (public Home, authentication, recommendation wizard) retain their existing information architecture and use the reference's visual primitives. Dashboard, Trips, and Budget retain honest existing empty states rather than adopting the export's mock journeys, expenses, itinerary, or account. Existing photography, attribution, live maps, API contracts, authentication, localization, and routing remain authoritative. Exported static map tiles and mock data are intentionally omitted.

Responsive CSS uses a drawer below 1024px, Explore columns at desktop/tablet/mobile widths, and stacked destination sections on small screens. These rules require manual visual verification: repository AGENTS.md prohibits Chrome or similar browser checks. Browser screenshots, client console inspection, and interactive end-to-end flows are therefore not claimed as verified.

Run `npm test`, `npm run lint`, `npx tsc --noEmit`, and `npm run build`. If this execution environment rejects Turbopack's local port binding, `npm run build -- --webpack` checks the production bundle with Next's supported alternative without changing project configuration.

Validation for this redesign: frontend tests 9/9; ESLint and TypeScript pass; production build passes with `--webpack`. The default Turbopack attempt fails on an environment port-binding restriction, including after requesting execution outside the sandbox. Backend tests: 156 passed, 21 skipped (PostGIS integration tests require `TEST_POSTGIS_CONNECTION`). Diff audits confirm unchanged authentication handlers, API calls, response handling, and map behavior; map changes are colors only. Static checks confirm exact local font copies and responsive media rules on all redesigned surfaces. No browser-based responsive, console, or interaction checks were performed.
