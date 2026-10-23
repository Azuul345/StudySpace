import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, getExam, getExams, getTopic } from './client'

function mockFetch(status: number, body: unknown) {
  const fn = vi.fn().mockResolvedValue(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'Content-Type': status >= 400 ? 'application/problem+json' : 'application/json' },
    }),
  )
  vi.stubGlobal('fetch', fn)
  return fn
}

afterEach(() => vi.unstubAllGlobals())

describe('api client', () => {
  it('lists exams from /api/v1/exams', async () => {
    const fetchMock = mockFetch(200, [{ code: 'SAA-C03', name: 'AWS SAA', version: '2026.10.1' }])
    const exams = await getExams()
    expect(exams[0].code).toBe('SAA-C03')
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/exams')
  })

  it('url-encodes the exam code', async () => {
    const fetchMock = mockFetch(200, { code: 'A/B', name: 'x', version: '1', domains: [] })
    await getExam('A/B')
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/exams/A%2FB')
  })

  it('fetches a topic by id', async () => {
    const fetchMock = mockFetch(200, { id: 3, name: 'S3' })
    await getTopic(3)
    expect(fetchMock.mock.calls[0][0]).toBe('/api/v1/topics/3')
  })

  it('throws ApiError with the problem title on 404', async () => {
    mockFetch(404, { title: 'Exam not found.', status: 404 })
    await expect(getExam('NOPE')).rejects.toMatchObject({
      name: 'ApiError',
      status: 404,
      message: 'Exam not found.',
    })
  })

  it('throws ApiError on network failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('failed')))
    await expect(getExams()).rejects.toBeInstanceOf(ApiError)
  })
})
