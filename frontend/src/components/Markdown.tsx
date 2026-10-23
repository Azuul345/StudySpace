import ReactMarkdown from 'react-markdown'

// react-markdown never renders raw HTML (no rehype-raw), so scripts and inline HTML stay text.
// Links are limited to https; images are not rendered at all (no external embeds).
function safeUrl(url: string): string {
  try {
    return new URL(url).protocol === 'https:' ? url : ''
  } catch {
    return ''
  }
}

export function Markdown({ source }: { source: string }) {
  return (
    <ReactMarkdown
      urlTransform={safeUrl}
      disallowedElements={['img']}
      components={{
        a: ({ href, children }) =>
          href ? (
            <a href={href} target="_blank" rel="noopener noreferrer">
              {children}
            </a>
          ) : (
            <>{children}</>
          ),
      }}
    >
      {source}
    </ReactMarkdown>
  )
}
