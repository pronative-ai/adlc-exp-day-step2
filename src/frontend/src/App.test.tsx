import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import App from './App'

// Minimal smoke test: app renders the key UI elements.
describe('App', () => {
  it('renders conversion and audit lookup forms', () => {
    ;(window as any).__VITE_API_URL__ = ''
    render(<App />)

    expect(screen.getByText('New Conversion')).toBeTruthy()
    expect(screen.getByText('Audit Lookup')).toBeTruthy()
    expect(screen.getByRole('button', { name: /Convert/i })).toBeTruthy()
  })
})
