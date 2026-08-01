import { describe, expect, it, afterEach } from "vitest";
import { resolveApiBaseUrl } from "./api.js";

describe("resolveApiBaseUrl", () => {
  afterEach(() => {
    delete window.__VITE_API_URL__;
  });

  it("returns empty string when no runtime value is injected", () => {
    expect(resolveApiBaseUrl()).toBe("");
  });

  it("returns empty string when the placeholder token is still present", () => {
    window.__VITE_API_URL__ = "__VITE_API_URL__";
    expect(resolveApiBaseUrl()).toBe("");
  });

  it("returns the runtime-injected value with trailing slashes trimmed", () => {
    window.__VITE_API_URL__ = "https://api.example.com/";
    expect(resolveApiBaseUrl()).toBe("https://api.example.com");
  });
});
