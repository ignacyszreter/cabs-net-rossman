#!/usr/bin/env bash
# crime-scene.sh — the whole repository history in numbers, in one run. Reads history, never writes to it.
# Usage: crime-scene.sh [repo] [out] — the report lands in crime-scene.md
set -euo pipefail
export LC_ALL=C   # pins sort order, so two machines break ties the same way

TOP=20            # rows in the file and pair tables
MIN_SHARED=3      # a pair below this many shared commits is noise
MIN_J=0.30        # a pair below this J does not bind two files into one cluster
CROSS_SHARED=4    # a pair across two clusters is drawn only from this many shared commits

REPO=${1:-.}
OUT=${2:-crime-scene.md}
git -C "$REPO" rev-parse --git-dir >/dev/null 2>&1 || { echo "not a git repository: $REPO" >&2; exit 1; }
git -C "$REPO" rev-parse --verify -q HEAD >/dev/null || { echo "no commits yet in $REPO" >&2; exit 1; }

TAB=$(printf '\t')
CACHE=$(mktemp -d)
trap 'rm -rf "$CACHE"' EXIT
G() { git -C "$REPO" "$@"; }

COMMITS=$(G rev-list --count HEAD)
HEAD_SHORT=$(G rev-parse --short HEAD)
HEAD_NAME=$(G describe --tags --exact-match HEAD 2>/dev/null || G rev-parse --abbrev-ref HEAD)

# hash<TAB>file for every commit in the history
G log HEAD --format='#%h' --name-only |
  awk '/^#/ { h = substr($0, 2); next } NF { print h "\t" $0 }' > "$CACHE/stream"
stream() { cat "$CACHE/stream"; }

# shared<TAB>J<TAB>A commits<TAB>B commits<TAB>A<TAB>B — every pair at or over MIN_SHARED
stream | awk -F'\t' -v min="$MIN_SHARED" '
  function emit(  i, j, a, b, t) {
    for (i = 1; i < n; i++) for (j = i + 1; j <= n; j++) {
      a = f[i]; b = f[j]; if (a > b) { t = a; a = b; b = t }
      pc[a "\t" b]++
    }
    n = 0
  }
  { if ($1 != h) { emit(); h = $1 } f[++n] = $2; fc[$2]++ }
  END {
    emit()
    for (k in pc) if (pc[k] >= min) {
      split(k, p, "\t"); u = fc[p[1]] + fc[p[2]] - pc[k]
      printf "%d\t%.2f\t%d\t%d\t%s\t%s\n", pc[k], pc[k] / u, fc[p[1]], fc[p[2]], p[1], p[2]
    }
  }' | sort -t"$TAB" -k1,1nr -k2,2nr -k5,5 -k6,6 > "$CACHE/pairs"
pairs() { cat "$CACHE/pairs"; }

# cluster<TAB>file<TAB>commits — files joined by edges at or over MIN_SHARED and MIN_J,
# clusters numbered by weight (sum of member commit counts), members by their own count
pairs | awk -F'\t' -v minj="$MIN_J" '
  function find(x) { while (p[x] != x) { p[x] = p[p[x]]; x = p[x] } return x }
  $2 + 0 >= minj {
    if (!($5 in p)) p[$5] = $5; if (!($6 in p)) p[$6] = $6
    fc[$5] = $3; fc[$6] = $4
    a = find($5); b = find($6); if (a != b) p[a] = b
  }
  END {
    for (f in p) { r = find(f); size[r]++; sum[r] += fc[f] }
    for (f in p) { r = find(f); printf "%d\t%d\t%s\t%d\t%s\n", sum[r], size[r], r, fc[f], f }
  }' |
  sort -t"$TAB" -k1,1nr -k2,2nr -k3,3 -k4,4nr -k5,5 |
  awk -F'\t' '{ if ($3 != r) { r = $3; c++ } printf "%d\t%s\t%d\n", c, $5, $4 }' > "$CACHE/clusters"
clusters() { cat "$CACHE/clusters"; }

short() { awk -F/ '{ print (NF > 1 ? $(NF-1) "/" $NF : $NF) }'; }
h2() { printf '\n## %s\n\n' "$1"; }

exec 3>&1 >"$OUT"   # the report goes to the file, fd 3 keeps the terminal for the closing line

printf '# crime scene · %s · %s (%s)\n\n' "$(G rev-parse --show-toplevel | short)" "$HEAD_NAME" "$HEAD_SHORT"
printf '| field | value |\n|---|---|\n'
printf '| commits | %s |\n' "$COMMITS"
printf '| period | %s → %s |\n' "$(G log HEAD --format=%ad --date=short | tail -1)" "$(G log HEAD -1 --format=%ad --date=short)"
printf '| authors | %s |\n' "$(G log HEAD --format=%an | sort -u | tr '\n' ',' | sed 's/,$//; s/,/, /g')"
printf '| thresholds | top %s · pair floor %s shared · cluster floor J %s |\n' "$TOP" "$MIN_SHARED" "$MIN_J"
if [ "$COMMITS" -lt 50 ]; then
  printf '\n> **WARNING** — %s commits only. Change coupling from a history this shallow is unreliable.\n' "$COMMITS"
fi
if [ "$(G log HEAD --format=%an | sort -u | wc -l | tr -d ' ')" -le 1 ]; then
  printf '\n> **WARNING** — one author over the whole range (import from another VCS?). Dates and authorship may be false.\n'
fi

h2 "1. Most-changed files"
printf '| commits | lines | file |\n|---:|---:|---|\n'
stream | cut -f2 | sort | uniq -c | sort -k1,1nr -k2,2 |
  while read -r c p; do
    [ "${shown:=0}" -lt "$TOP" ] || break
    G cat-file -e "HEAD:$p" 2>/dev/null || continue
    printf '| %d | %d | `%s` |\n' "$c" "$(G show "HEAD:$p" | awk 'END { print NR }')" "$p"
    shown=$((shown + 1))
  done

h2 "2. File pairs that change together"
echo 'shared = commits with both files · J = shared / union of both histories · A, B = commits of each file.'
echo
echo 'shared says how loud the pair is, J says how tight: a busy file pairs with everything and scores a low J.'
echo
if [ -s "$CACHE/pairs" ]; then
  printf '| shared | J | A | B | file A | file B |\n|---:|---:|---:|---:|---|---|\n'
  pairs | awk -F'\t' -v top="$TOP" 'NR <= top { printf "| %d | %s | %d | %d | `%s` | `%s` |\n", $1, $2, $3, $4, $5, $6 }'
else
  printf 'No pair reaches %s shared commits.\n' "$MIN_SHARED"
fi

h2 "3. Clusters — files that move as one"
printf 'An edge is a pair at or over %s shared commits and J %s; a cluster is what the edges connect.\n\n' "$MIN_SHARED" "$MIN_J"
echo 'The joint commits are the commits that touch two or more files of the cluster — the ones to read first.'
if [ -s "$CACHE/clusters" ]; then
  cut -f1 "$CACHE/clusters" | uniq | while read -r c; do
    awk -F'\t' -v c="$c" '$1 == c { print $2 }' "$CACHE/clusters" > "$CACHE/members"
    stream | awk -F'\t' 'NR == FNR { m[$1]; next } ($2 in m) { n[$1]++; if (n[$1] == 2) print $1 }' "$CACHE/members" - > "$CACHE/joint"
    printf '\n### cluster %s · %s files · %s joint commits\n\n' "$c" "$(wc -l < "$CACHE/members" | tr -d ' ')" "$(wc -l < "$CACHE/joint" | tr -d ' ')"
    printf '| commits | file |\n|---:|---|\n'
    awk -F'\t' -v c="$c" '$1 == c { printf "| %d | `%s` |\n", $3, $2 }' "$CACHE/clusters"
    pairs | awk -F'\t' '
      NR == FNR { m[$1]; next }
      ($5 in m) && ($6 in m) {
        if (!k++) print "\n**Edges**\n\n| shared | J | file A | file B |\n|---:|---:|---|---|"
        printf "| %d | %s | `%s` | `%s` |\n", $1, $2, $5, $6
      }' "$CACHE/members" -
    if [ -s "$CACHE/joint" ]; then
      printf '\n**Joint commits** — newest first\n\n| commit | date | author | subject |\n|---|---|---|---|\n'
      while read -r j; do G show -s --format='%h%x09%ad%x09%an%x09%s' --date=short "$j"; done < "$CACHE/joint" |
        awk -F'\t' '
          function esc(t,   a, n, i, r) { n = split(t, a, "|"); r = a[1]; for (i = 2; i <= n; i++) r = r bs "|" a[i]; return r }
          BEGIN { bs = sprintf("%c", 92) }
          { printf "| `%s` | %s | %s | %s |\n", $1, $2, esc($3), esc($4) }'
    fi
  done
else
  printf '\nNo cluster: no pair clears both floors.\n'
fi

h2 "4. Graph (Mermaid)"
if [ -s "$CACHE/clusters" ]; then
  printf 'One subgraph per cluster, one node per file, edge label = shared commits; a dotted edge crosses clusters (from %s shared).\n\n' "$CROSS_SHARED"
  echo '```mermaid'
  pairs | awk -F'\t' -v crossmin="$CROSS_SHARED" '
    function short(p,   a, n) { n = split(p, a, "/"); return (n > 1 ? a[n-1] "/" a[n] : a[n]) }
    function id(f) { if (!(f in nid)) nid[f] = "n" (++nn); return nid[f] }
    NR == FNR { cl[$2] = $1; if (!($1 in cname)) { cname[$1] = 1; order[++cn] = $1 } members[$1] = members[$1] "\034" $2; next }
    ($5 in cl) && ($6 in cl) && (cl[$5] == cl[$6] || $1 >= crossmin) { e++; ea[e] = $5; eb[e] = $6; ew[e] = $1; cross[e] = (cl[$5] != cl[$6]) }
    END {
      print "graph LR"
      for (i = 1; i <= cn; i++) {
        c = order[i]
        printf "  subgraph c%s[\"cluster %s\"]\n", c, c
        m = split(members[c], ms, "\034")
        for (j = 2; j <= m; j++) printf "    %s[\"%s\"]\n", id(ms[j]), short(ms[j])
        print "  end"
      }
      for (i = 1; i <= e; i++) printf "  %s %s|%d| %s\n", id(ea[i]), (cross[i] ? "-.-" : "---"), ew[i], id(eb[i])
      for (i = 1; i <= e; i++) printf "  linkStyle %d stroke-width:%dpx\n", i - 1, (ew[i] > 6 ? 6 : ew[i])
    }' "$CACHE/clusters" -
  echo '```'
else
  echo 'No cluster to draw.'
fi

exec >&3 3>&-
printf '%s · %s lines\n' "$OUT" "$(wc -l < "$OUT" | tr -d ' ')"
