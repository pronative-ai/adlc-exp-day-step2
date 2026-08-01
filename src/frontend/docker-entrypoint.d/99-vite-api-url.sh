#!/usr/bin/env sh
set -eu

API_URL="${VITE_API_URL:-}"
INDEX_HTML="/usr/share/nginx/html/index.html"

if [ -f "$INDEX_HTML" ]; then
  # Escape for sed replacement delimiter usage.
  # shellcheck disable=SC2001
  ESCAPED_API_URL=$(printf '%s' "$API_URL" | sed -e 's/[\\&/]/\\\\&/g')
  sed -i "s|__VITE_API_URL__|$ESCAPED_API_URL|g" "$INDEX_HTML"
fi
