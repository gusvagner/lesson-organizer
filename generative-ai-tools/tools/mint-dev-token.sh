#!/usr/bin/env bash
# Mints an HS256 JWT for local development only: this API has no login, it trusts tokens signed with Jwt:SigningKey.
# Usage: TASKS_JWT_SIGNING_KEY=<same value as Jwt:SigningKey> bash tools/mint-dev-token.sh <user-guid> [minutes]
set -euo pipefail

usage="usage: bash tools/mint-dev-token.sh <user-guid> [minutes]"
user_id="${1:?$usage}"
minutes="${2:-60}"
signing_key="${TASKS_JWT_SIGNING_KEY:?set TASKS_JWT_SIGNING_KEY to the same value as the Jwt:SigningKey user secret}"
issuer="${TASKS_JWT_ISSUER:-https://identity.task-management.local}"
audience="${TASKS_JWT_AUDIENCE:-task-management-api}"

if [[ ! "$user_id" =~ ^[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}$ ]]; then
  echo "user id must be a Guid like 5981E58B-AC08-434D-9070-72777ED400A0; got '$user_id'. $usage" >&2
  exit 2
fi

if [[ ! "$minutes" =~ ^[1-9][0-9]{0,4}$ ]]; then
  echo "minutes must be a whole number from 1 to 99999; got '$minutes'. $usage" >&2
  exit 2
fi

base64url() { openssl base64 -A | tr '+/' '-_' | tr -d '='; }

now=$(date +%s)
expires=$((now + minutes * 60))
header=$(printf '{"alg":"HS256","typ":"JWT"}' | base64url)
payload=$(printf '{"sub":"%s","iss":"%s","aud":"%s","iat":%d,"nbf":%d,"exp":%d}' \
  "$user_id" "$issuer" "$audience" "$now" "$now" "$expires" | base64url)
signature=$(printf '%s.%s' "$header" "$payload" | openssl dgst -sha256 -hmac "$signing_key" -binary | base64url)

printf '%s.%s.%s\n' "$header" "$payload" "$signature"
