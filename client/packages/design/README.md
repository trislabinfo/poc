# @datarizen/design

Shared design system for Builder, Dashboard, and Runtime: CSS tokens and optional JS tokens.

## Usage

In your SvelteKit app:

```css
/* In app.css or +layout.svelte */
@import '@datarizen/design/tokens.css';
```

Or reference the file from `node_modules/@datarizen/design/src/tokens.css` (or the exported path per package.json).

## Build

```bash
pnpm install
pnpm run build
```
