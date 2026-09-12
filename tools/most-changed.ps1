# The most-changed files in this repository, straight out of the log.
# Pass a revision to stop at a point in history: .\most-changed.ps1 start

git log --no-merges --pretty=format: --name-only @args |
  Where-Object { $_ -ne '' } |
  Group-Object |
  Sort-Object -Property Count -Descending |
  Select-Object -First 20 -Property Count, Name
