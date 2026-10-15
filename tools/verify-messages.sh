#!/usr/bin/env bash
# Renders the admin Messages page and checks that every #i-* icon it references
# exists inside Views/Shared/_Icons.cshtml, plus the structural bits of the design.
set -uo pipefail
BASE=http://localhost:5199
TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT
cd "$(dirname "$0")/../Nexos Network System"

# --- login (cookie jar inside TMP so nothing lingers) ---
jar="$TMP/cookies"
page=$(curl -s -c "$jar" "$BASE/Identity/Account/Login")
token=$(printf '%s' "$page" | grep -o 'name="__RequestVerificationToken"[^>]*value="[^"]*"' | sed 's/.*value="//;s/"$//')
curl -s -b "$jar" -c "$jar" -o /dev/null -X POST "$BASE/Identity/Account/Login" \
  --data-urlencode "Email=admin@nexos.local" --data-urlencode "Password=Nexos@123" \
  --data-urlencode "__RequestVerificationToken=$token"

out="$TMP/messages.html"
code=$(curl -s -b "$jar" -o "$out" -w "%{http_code}" "$BASE/Admin/Messages")
echo "page status: $code"
[ "$code" = "200" ] || { echo "FAIL: expected 200"; exit 1; }

fail=0
# --- icon audit: every #i-* href on the page must resolve to a symbol in _Icons.cshtml ---
for id in $(grep -o 'href="#i-[a-z-]*"' "$out" | sed 's/href="#//;s/"//' | sort -u); do
  grep -q "symbol id=\"$id\"" Views/Shared/_Icons.cshtml || { echo "MISSING symbol: $id"; fail=1; }
done

# --- structure audit ---
declare -A checks=(
  ["kpi-card foot trend wrapper"]="kpi-card__trend"
  ["sparklines present"]="kpi-spark__line"
  ["trend caption"]="kpi-trend__caption"
  ["tab row"]="directory__tabs"
  ["active tab"]="directory__tab is-active"
  ["sort select"]="sortSelect"
  ["advanced filters"]="advancedFilters"
  ["person avatar"]="person__avatar"
  ["status pill"]="status-pill"
  ["paging"]="pager__btn"
  ["row actions"]="icon-btn"
  ["empty state does not show"]="empty-state"
)
for label in "${!checks[@]}"; do :; done
for needle in "kpi-card__trend" "kpi-spark__line" "kpi-trend__caption" "directory__tabs" \
              "directory__tab is-active" "sortSelect" "advancedFilters" "person__avatar" \
              "status-pill" "pager__btn" "icon-btn"; do
  count=$(grep -c "$needle" "$out" || true)
  if [ "${count:-0}" -eq 0 ]; then echo "MISSING on page: $needle"; fail=1; fi
done

# --- hardcoded mock percentages must not be back ---
if grep -q '↑ +12%\|↓ -5%\|↑ +3%\|↑ +18%' "$out"; then
  echo "FAIL: hard-coded mock percentages present"; fail=1
fi

echo "--- count summary ---"
echo "kpi cards:        $(grep -c 'kpi-card__value' "$out")"
echo "sparklines:       $(grep -c 'kpi-spark__line' "$out")"
echo "trend captions:   $(grep -c 'kpi-trend__caption' "$out")"
echo "status pills:     $(grep -c 'status-pill' "$out")"
echo "avatars:          $(grep -c 'person__avatar' "$out")"
echo "checkbox rows:    $(grep -c 'form-check-input' "$out")"

if [ "$fail" = 0 ]; then echo "ALL CHECKS PASSED"; else echo "CHECKS FAILED"; exit 1; fi
