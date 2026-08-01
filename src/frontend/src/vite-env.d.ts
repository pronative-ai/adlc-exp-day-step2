/// <reference types="vitest" />

declare global {
  interface Window {
    __VITE_API_URL__?: string
  }
}

export {}
