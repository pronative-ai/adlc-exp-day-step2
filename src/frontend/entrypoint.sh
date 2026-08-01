#!/bin/sh
set -e

INDEX_HTML="${INDEX_HTML:-/usr/share/nginx/html/index.html}"
VITE_API_URL="${VITE_API_URL:-}"

if [ -f "$INDEX_HTML" ]; then
  sed -i "s|__VITE_API_URL__|${VITE_API_URL}|g" "$INDEX_HTML"
fi

exec nginx -g "daemon off;"
