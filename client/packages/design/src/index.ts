/**
 * @datarizen/design
 * Shared design tokens and base styles.
 * Import tokens in app: import '@datarizen/design/tokens.css' or use the CSS file path.
 */

export const designTokens = {
  color: {
    primary: 'var(--datarizen-primary, #2563eb)',
    background: 'var(--datarizen-bg, #ffffff)',
    text: 'var(--datarizen-text, #1f2937)',
    border: 'var(--datarizen-border, #e5e7eb)',
  },
  spacing: {
    xs: '0.25rem',
    sm: '0.5rem',
    md: '1rem',
    lg: '1.5rem',
    xl: '2rem',
  },
  radius: {
    sm: '0.25rem',
    md: '0.375rem',
    lg: '0.5rem',
  },
} as const;
