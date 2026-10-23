import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Markdown } from './Markdown'

describe('Markdown (REQ-SEC-003)', () => {
  it('renders basic formatting', () => {
    render(<Markdown source={'# Title\n\n**bold** and a list:\n\n- one\n- two'} />)
    expect(screen.getByRole('heading', { name: 'Title' })).toBeInTheDocument()
    expect(screen.getByText('bold').tagName).toBe('STRONG')
    expect(screen.getAllByRole('listitem')).toHaveLength(2)
  })

  it('does not render raw HTML or scripts', () => {
    const { container } = render(
      <Markdown source={'<script>alert(1)</script><img src=x onerror="alert(1)"><b>raw</b>'} />,
    )
    expect(container.querySelector('script')).toBeNull()
    expect(container.querySelector('img')).toBeNull()
    expect(container.querySelector('b')).toBeNull()
  })

  it('drops javascript: and data: link targets', () => {
    const { container } = render(<Markdown source={'[bad](javascript:alert(1)) [worse](data:text/html;base64,AAAA)'} />)
    expect(screen.queryAllByRole('link')).toHaveLength(0)
    expect(container).toHaveTextContent('bad worse')
  })

  it('opens https links safely', () => {
    render(<Markdown source={'[docs](https://docs.aws.amazon.com/x)'} />)
    const link = screen.getByRole('link', { name: 'docs' })
    expect(link).toHaveAttribute('href', 'https://docs.aws.amazon.com/x')
    expect(link).toHaveAttribute('rel', 'noopener noreferrer')
    expect(link).toHaveAttribute('target', '_blank')
  })

  it('does not render images (no external embeds)', () => {
    const { container } = render(<Markdown source={'![x](https://evil.example/p.png)'} />)
    expect(container.querySelector('img')).toBeNull()
  })
})
