#!/usr/bin/env bash
# Verifies every Retail / Technical / Accounts page renders for its own role.
# Reports HTTP status plus the first Razor error line when a page fails.
BASE=http://localhost:5199
TMP=$(mktemp -d)
FAIL=0

login() {
  local email="$1" jar="$TMP/$(echo "$1" | tr -d '@.')".cookies
  local page token
  page=$(curl -s -c "$jar" "$BASE/Identity/Account/Login")
  token=$(printf '%s' "$page" | grep -o 'name="__RequestVerificationToken"[^>]*value="[^"]*"' | sed 's/.*value="//;s/"$//')
  curl -s -b "$jar" -c "$jar" -o /dev/null -X POST "$BASE/Identity/Account/Login" \
    --data-urlencode "Email=$email" --data-urlencode "Password=Nexos@123" \
    --data-urlencode "RememberMe=false" --data-urlencode "__RequestVerificationToken=$token"
  echo "$jar"
}

check() {
  local jar="$1" path="$2" want="${3:-200}" code
  code=$(curl -s -b "$jar" -o "$TMP/body.html" -w "%{http_code}" "$BASE$path")
  if [ "$code" = "$want" ]; then
    printf '  ok   %-3s %s\n' "$code" "$path"
  else
    FAIL=$((FAIL + 1))
    printf '  FAIL %-3s (want %s) %s\n' "$code" "$want" "$path"
    grep -o 'InvalidOperationException[^<]*\|NullReferenceException[^<]*\|An unhandled exception[^<]*' "$TMP/body.html" | head -2 | sed 's/^/         /'
  fi
}

# Discovers the ids used by the details pages straight from the corresponding list page.
first_link() {
  local jar="$1" path="$2" needle="$3"
  curl -s -b "$jar" "$BASE$path" | grep -o "href=\"[^\"]*$needle[^\"]*\"" | head -1 | sed 's/href="//;s/"$//'
}

echo "== Retail =="
J=$(login retail@nexos.local)
for p in /Retail/Dashboard /Retail/Customers /Retail/Customers/Create /Retail/Orders \
         /Retail/Orders/Create /Retail/Orders/Track /Retail/Bills /Retail/Connections \
         /Retail/Payments /Retail/Plans; do check "$J" "$p"; done
for p in $(first_link "$J" /Retail/Orders '/Retail/Orders/Details/') \
         $(first_link "$J" /Retail/Bills '/Retail/Bills/Details/') \
         $(first_link "$J" /Retail/Connections '/Retail/Connections/Details/') \
         $(first_link "$J" /Retail/Customers '/Retail/Customers/Details/'); do check "$J" "$p"; done

echo "== Technical =="
J=$(login technical@nexos.local)
for p in /Technical/Dashboard /Technical/Feasibility /Technical/Feasibility/Create \
         /Technical/Connections /Technical/Connections/Create /Technical/Equipment \
         /Technical/Equipment/Assign /Technical/Orders; do check "$J" "$p"; done
for p in $(first_link "$J" /Technical/Feasibility '/Technical/Feasibility/Details/') \
         $(first_link "$J" /Technical/Feasibility '/Technical/Feasibility/Edit/') \
         $(first_link "$J" /Technical/Connections '/Technical/Connections/Details/') \
         $(first_link "$J" /Technical/Orders '/Technical/Orders/Details/'); do check "$J" "$p"; done

echo "== Accounts =="
J=$(login accounts@nexos.local)
for p in /Accounts/Dashboard /Accounts/Bills /Accounts/Bills/Generate /Accounts/Payments \
         /Accounts/Payments/Create /Accounts/Dues /Accounts/Customers /Accounts/Connections; do check "$J" "$p"; done
for p in $(first_link "$J" /Accounts/Bills '/Accounts/Bills/Details/') \
         $(first_link "$J" /Accounts/Payments '/Accounts/Payments/Details/') \
         $(first_link "$J" /Accounts/Customers '/Accounts/Customers/Details/') \
         $(first_link "$J" /Accounts/Connections '/Accounts/Connections/Details/'); do check "$J" "$p"; done

echo "== Admin (baseline, should be unaffected) =="
J=$(login admin@nexos.local)
for p in /Admin/Dashboard /Admin/Customers /Admin/Orders /Admin/Payments; do check "$J" "$p"; done

echo
echo "failures: $FAIL"
rm -rf "$TMP"
exit $((FAIL > 0))
