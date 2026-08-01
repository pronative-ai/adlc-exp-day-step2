#!/bin/sh
set -eu

INDEX_HTML="/usr/share/nginx/html/index.html"

if [ -f "$INDEX_HTML" ]; then
  # Escape characters that are special in sed replacement.
  ESCAPED_API_URL=$(printf '%s' "${VITE_API_URL:-}" | sed -e 's/[\/&]/\\&/g')
  sed -i "s|__VITE_API_URL__|$ESCAPED_API_URL|g" "$INDEX_HTML"
fi

exec nginx -g 'daemon off;'
