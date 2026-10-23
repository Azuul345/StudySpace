import { describe, expect, it } from 'vitest'
import { contentSecurityPolicy } from './csp'

describe('contentSecurityPolicy', () => {
  const directives = Object.fromEntries(
    contentSecurityPolicy.split(';').map((d) => {
      const [name, ...values] = d.trim().split(/\s+/)
      return [name, values]
    }),
  )

  it('restricts everything to self by default', () => {
    expect(directives['default-src']).toEqual(["'self'"])
  })

  it('allows no inline or remote scripts', () => {
    expect(directives['script-src']).toEqual(["'self'"])
  })

  it('blocks plugins and locks base URI and form targets', () => {
    expect(directives['object-src']).toEqual(["'none'"])
    expect(directives['base-uri']).toEqual(["'self'"])
    expect(directives['form-action']).toEqual(["'self'"])
  })

  it('only talks to its own origin', () => {
    expect(directives['connect-src']).toEqual(["'self'"])
  })
})
