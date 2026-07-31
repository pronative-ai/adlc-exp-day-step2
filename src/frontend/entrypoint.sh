#!/bin/sh
set -eu

FILE="/usr/share/nginx/html/index.html"

api_url="${VITE_API_URL:-}"

if [ -f "$FILE" ]; then
  # Escape sed replacement characters.
  escaped=$(printf '%s' "$api_url" | sed -e 's/[\/&]/\\&/g')
  sed -i "s|__VITE_API_URL__|$escaped|g" "$FILE"
fi

exec "$@"
