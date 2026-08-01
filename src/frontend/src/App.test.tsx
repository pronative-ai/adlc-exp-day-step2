import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import App from './App';

describe('App', () => {
  it('renders the main conversion and audit workflow headings', () => {
    render(<App />);

    expect(screen.getByRole('heading', { name: /real-time currency conversion/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /convert currency/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /audit lookup/i })).toBeInTheDocument();
  });
});
