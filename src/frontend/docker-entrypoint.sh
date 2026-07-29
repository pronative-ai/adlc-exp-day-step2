#!/bin/sh
set -eu

TEMPLATE_PATH=/usr/share/nginx/html/runtime-config.template.js
OUT_PATH=/usr/share/nginx/html/runtime-config.js

if [ -f "${TEMPLATE_PATH}" ]; then
  # Placeholder replacement at container startup.
  # Known Constraints require reading VITE_API_URL dynamically at runtime.
  API_URL_VALUE="${VITE_API_URL:-}"
  # Replace placeholder token in the template.
  sed "s|__VITE_API_URL__|${API_URL_VALUE}|g" "${TEMPLATE_PATH}" > "${OUT_PATH}"
fi

exec nginx -g 'daemon off;'
