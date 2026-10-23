// Content Security Policy for the built app (REQ-SEC-003). No inline or remote scripts.
// style-src allows 'unsafe-inline' only because React sets style attributes; scripts stay locked down.
export const contentSecurityPolicy = [
  "default-src 'self'",
  "script-src 'self'",
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data:",
  "connect-src 'self'",
  "object-src 'none'",
  "base-uri 'self'",
  "form-action 'self'",
].join('; ')
