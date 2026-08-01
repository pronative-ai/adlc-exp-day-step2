#!/bin/sh
set -eu

API_URL="${VITE_API_URL:-}"
ESCAPED_API_URL=$(printf '%s' "$API_URL" | sed -e 's/[\/&]/\\&/g')

find /usr/share/nginx/html -type f -name 'index.html' -exec sed -i "s|__VITE_API_URL__|${ESCAPED_API_URL}|g" {} +
