import { render, screen } from '@testing-library/react';
import App from '../App';
import { expect, test } from 'vitest';

test('renders the conversion title', () => {
  render(<App />);
  expect(screen.getByText(/Real-Time Currency Conversion/)).toBeInTheDocument();
});
