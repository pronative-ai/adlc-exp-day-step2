#!/bin/sh
set -eu

API_URL="${VITE_API_URL:-}"

# Escape characters that commonly break sed replacement.
ESCAPED_API_URL=$(printf '%s' "$API_URL" | sed -e 's/[\/&]/\\&/g')

INDEX_HTML="/usr/share/nginx/html/index.html"
if [ -f "$INDEX_HTML" ]; then
  sed -i "s|__VITE_API_URL__|$ESCAPED_API_URL|g" "$INDEX_HTML"
fi

exec "$@"
