#!/usr/bin/env sh
# The most-changed files in this repository, straight out of the log.
# Pass a revision to stop at a point in history: most-changed.sh start

git log --no-merges --pretty=format: --name-only "$@" |
  grep -v '^$' |
  sort |
  uniq -c |
  sort -rn |
  head -20
