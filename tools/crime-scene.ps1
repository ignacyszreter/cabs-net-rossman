# crime-scene.ps1 — the whole repository history in numbers, in one run. Reads history, never writes to it.
# Usage: .\crime-scene.ps1 [repo] [out] — the report lands in crime-scene.md
param(
  [string]$Repo = '.',
  [string]$Out = 'crime-scene.md'
)

$ErrorActionPreference = 'Stop'
# git exit codes are read by hand below, so a non-zero git must not throw
if (Test-Path variable:PSNativeCommandUseErrorActionPreference) { $PSNativeCommandUseErrorActionPreference = $false }

$Inv = [cultureinfo]::InvariantCulture

$Top = 20            # rows in the file and pair tables
$MinShared = 3       # a pair below this many shared commits is noise
$MinJ = 0.30         # a pair below this J does not bind two files into one cluster
$CrossShared = 4     # a pair across two clusters is drawn only from this many shared commits

function G { git -C $Repo @args }

function Fail([string]$message) { [Console]::Error.WriteLine($message); exit 1 }

G rev-parse --git-dir 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { Fail "not a git repository: $Repo" }
G rev-parse --verify -q HEAD | Out-Null
if ($LASTEXITCODE -ne 0) { Fail "no commits yet in $Repo" }

# rounds the way C printf does, so bash and PowerShell break the same ties; InvariantCulture keeps the dot
function FmtJ([double]$value) { [Math]::Round($value, 2, [MidpointRounding]::ToEven).ToString('0.00', $Inv) }
function Short([string]$path) { $a = $path -split '/'; if ($a.Count -gt 1) { "$($a[-2])/$($a[-1])" } else { $a[-1] } }
function Ord([string]$a, [string]$b) { [string]::CompareOrdinal($a, $b) }
function SortRows($rows, [scriptblock]$comparison) {
  $list = [System.Collections.Generic.List[object]]::new()
  foreach ($r in $rows) { $list.Add($r) }
  $list.Sort([System.Comparison[object]]$comparison)
  , $list
}

$commits = [int](G rev-list --count HEAD)
$headShort = G rev-parse --short HEAD
$headName = G describe --tags --exact-match HEAD 2>$null
if ($LASTEXITCODE -ne 0) { $headName = G rev-parse --abbrev-ref HEAD }

# one entry per commit, with the files it touched
$history = [System.Collections.Generic.List[object]]::new()
$current = $null
foreach ($line in (G log HEAD --format='#%h' --name-only)) {
  if ($line.StartsWith('#')) {
    $current = [pscustomobject]@{ Hash = $line.Substring(1); Files = [System.Collections.Generic.List[string]]::new() }
    $history.Add($current)
    continue
  }
  if ($line -ne '' -and $null -ne $current) { $current.Files.Add($line) }
}

$fileCount = @{}
foreach ($commit in $history) { foreach ($file in $commit.Files) { $fileCount[$file] = 1 + [int]$fileCount[$file] } }

# shared commits of every unordered pair
$shared = @{}
foreach ($commit in $history) {
  $files = $commit.Files
  for ($i = 0; $i -lt $files.Count - 1; $i++) {
    for ($j = $i + 1; $j -lt $files.Count; $j++) {
      $a = $files[$i]; $b = $files[$j]
      if ((Ord $a $b) -gt 0) { $a, $b = $b, $a }
      $key = "$a`t$b"
      $shared[$key] = 1 + [int]$shared[$key]
    }
  }
}

$pairs = foreach ($key in $shared.Keys) {
  $n = $shared[$key]
  if ($n -lt $MinShared) { continue }
  $a, $b = $key -split "`t", 2
  $union = $fileCount[$a] + $fileCount[$b] - $n
  [pscustomobject]@{
    Shared = $n
    J = [Math]::Round($n / $union, 2, [MidpointRounding]::ToEven)
    JText = FmtJ ($n / $union)
    CountA = $fileCount[$a]
    CountB = $fileCount[$b]
    A = $a
    B = $b
  }
}
$pairs = SortRows $pairs {
  param($x, $y)
  if ($x.Shared -ne $y.Shared) { return $y.Shared - $x.Shared }
  if ($x.J -ne $y.J) { return [Math]::Sign($y.J - $x.J) }
  $c = [string]::CompareOrdinal($x.A, $y.A)
  if ($c -ne 0) { return $c }
  return [string]::CompareOrdinal($x.B, $y.B)
}

# files joined by edges at or over MinShared and MinJ; clusters numbered by weight, members by their own count
$parent = @{}
function Root([string]$x) {
  while ($parent[$x] -ne $x) { $parent[$x] = $parent[$parent[$x]]; $x = $parent[$x] }
  $x
}
foreach ($pair in $pairs) {
  if ($pair.J -lt $MinJ) { continue }
  if (-not $parent.ContainsKey($pair.A)) { $parent[$pair.A] = $pair.A }
  if (-not $parent.ContainsKey($pair.B)) { $parent[$pair.B] = $pair.B }
  $ra = Root $pair.A; $rb = Root $pair.B
  if ($ra -ne $rb) { $parent[$ra] = $rb }
}

$clustered = @($parent.Keys)
$weight = @{}; $size = @{}
foreach ($file in $clustered) {
  $r = Root $file
  $size[$r] = 1 + [int]$size[$r]
  $weight[$r] = $fileCount[$file] + [int]$weight[$r]
}
$members = SortRows @(foreach ($file in $clustered) {
  $r = Root $file
  [pscustomobject]@{ Weight = $weight[$r]; Size = $size[$r]; RootFile = $r; Count = $fileCount[$file]; File = $file }
}) {
  param($x, $y)
  if ($x.Weight -ne $y.Weight) { return $y.Weight - $x.Weight }
  if ($x.Size -ne $y.Size) { return $y.Size - $x.Size }
  $c = [string]::CompareOrdinal($x.RootFile, $y.RootFile)
  if ($c -ne 0) { return $c }
  if ($x.Count -ne $y.Count) { return $y.Count - $x.Count }
  return [string]::CompareOrdinal($x.File, $y.File)
}

$clusters = [System.Collections.Generic.List[object]]::new()
$lastRoot = $null
foreach ($m in $members) {
  if ($m.RootFile -ne $lastRoot) {
    $lastRoot = $m.RootFile
    $clusters.Add([pscustomobject]@{ Number = $clusters.Count + 1; Files = [System.Collections.Generic.List[object]]::new() })
  }
  $clusters[-1].Files.Add([pscustomobject]@{ File = $m.File; Count = $m.Count })
}

$md = [System.Collections.Generic.List[string]]::new()
function W([string]$line = '') { $md.Add($line) }
function H2([string]$title) { W; W "## $title"; W }
function Esc([string]$cell) { $cell.Replace('|', '\|') }

$dates = G log HEAD --format=%ad --date=short
$authors = SortRows ((G log HEAD --format=%an) | Select-Object -Unique) { param($x, $y) [string]::CompareOrdinal($x, $y) }

W ("# crime scene · {0} · {1} ({2})" -f (Short (G rev-parse --show-toplevel)), $headName, $headShort)
W
W '| field | value |'
W '|---|---|'
W "| commits | $commits |"
W ("| period | {0} → {1} |" -f $dates[-1], $dates[0])
W ("| authors | {0} |" -f ($authors -join ', '))
W ("| thresholds | top {0} · pair floor {1} shared · cluster floor J {2} |" -f $Top, $MinShared, $MinJ.ToString('0.00', $Inv))
if ($commits -lt 50) {
  W
  W "> **WARNING** — $commits commits only. Change coupling from a history this shallow is unreliable."
}
if ($authors.Count -le 1) {
  W
  W '> **WARNING** — one author over the whole range (import from another VCS?). Dates and authorship may be false.'
}

H2 '1. Most-changed files'
W '| commits | lines | file |'
W '|---:|---:|---|'
$ranked = SortRows @(foreach ($file in $fileCount.Keys) { [pscustomobject]@{ Count = $fileCount[$file]; File = $file } }) {
  param($x, $y)
  if ($x.Count -ne $y.Count) { return $y.Count - $x.Count }
  return [string]::CompareOrdinal($x.File, $y.File)
}
$shown = 0
foreach ($row in $ranked) {
  if ($shown -ge $Top) { break }
  G cat-file -e "HEAD:$($row.File)" 2>$null | Out-Null
  if ($LASTEXITCODE -ne 0) { continue }
  $lines = @(G show "HEAD:$($row.File)").Count
  W ("| {0} | {1} | ``{2}`` |" -f $row.Count, $lines, $row.File)
  $shown++
}

H2 '2. File pairs that change together'
W 'shared = commits with both files · J = shared / union of both histories · A, B = commits of each file.'
W
W 'shared says how loud the pair is, J says how tight: a busy file pairs with everything and scores a low J.'
W
if ($pairs.Count -gt 0) {
  W '| shared | J | A | B | file A | file B |'
  W '|---:|---:|---:|---:|---|---|'
  foreach ($p in ($pairs | Select-Object -First $Top)) {
    W ("| {0} | {1} | {2} | {3} | ``{4}`` | ``{5}`` |" -f $p.Shared, $p.JText, $p.CountA, $p.CountB, $p.A, $p.B)
  }
} else {
  W "No pair reaches $MinShared shared commits."
}

H2 '3. Clusters — files that move as one'
W ("An edge is a pair at or over {0} shared commits and J {1}; a cluster is what the edges connect." -f $MinShared, $MinJ.ToString('0.00', $Inv))
W
W 'The joint commits are the commits that touch two or more files of the cluster — the ones to read first.'
if ($clusters.Count -gt 0) {
  foreach ($cluster in $clusters) {
    $set = @{}
    foreach ($f in $cluster.Files) { $set[$f.File] = $true }
    $joint = foreach ($commit in $history) {
      $hits = 0
      foreach ($f in $commit.Files) { if ($set.ContainsKey($f)) { $hits++ } }
      if ($hits -ge 2) { $commit.Hash }
    }
    $joint = @($joint)
    W
    W ("### cluster {0} · {1} files · {2} joint commits" -f $cluster.Number, $cluster.Files.Count, $joint.Count)
    W
    W '| commits | file |'
    W '|---:|---|'
    foreach ($f in $cluster.Files) { W ("| {0} | ``{1}`` |" -f $f.Count, $f.File) }
    $edges = @($pairs | Where-Object { $set.ContainsKey($_.A) -and $set.ContainsKey($_.B) })
    if ($edges.Count -gt 0) {
      W
      W '**Edges**'
      W
      W '| shared | J | file A | file B |'
      W '|---:|---:|---|---|'
      foreach ($e in $edges) { W ("| {0} | {1} | ``{2}`` | ``{3}`` |" -f $e.Shared, $e.JText, $e.A, $e.B) }
    }
    if ($joint.Count -gt 0) {
      W
      W '**Joint commits** — newest first'
      W
      W '| commit | date | author | subject |'
      W '|---|---|---|---|'
      foreach ($hash in $joint) {
        $h, $date, $author, $subject = (G show -s --format='%h%x09%ad%x09%an%x09%s' --date=short $hash) -split "`t", 4
        W ("| ``{0}`` | {1} | {2} | {3} |" -f $h, $date, (Esc $author), (Esc $subject))
      }
    }
  }
} else {
  W
  W 'No cluster: no pair clears both floors.'
}

H2 '4. Graph (Mermaid)'
if ($clusters.Count -gt 0) {
  W ("One subgraph per cluster, one node per file, edge label = shared commits; a dotted edge crosses clusters (from {0} shared)." -f $CrossShared)
  W
  W '```mermaid'
  $clusterOf = @{}
  foreach ($cluster in $clusters) { foreach ($f in $cluster.Files) { $clusterOf[$f.File] = $cluster.Number } }
  $nodeId = @{}
  function NodeId([string]$file) {
    if (-not $nodeId.ContainsKey($file)) { $nodeId[$file] = "n$($nodeId.Count + 1)" }
    $nodeId[$file]
  }
  $edges = @(foreach ($p in $pairs) {
    if (-not ($clusterOf.ContainsKey($p.A) -and $clusterOf.ContainsKey($p.B))) { continue }
    if ($clusterOf[$p.A] -ne $clusterOf[$p.B] -and $p.Shared -lt $CrossShared) { continue }
    $p
  })
  W 'graph LR'
  foreach ($cluster in $clusters) {
    W ("  subgraph c{0}[`"cluster {0}`"]" -f $cluster.Number)
    foreach ($f in $cluster.Files) { W ("    {0}[`"{1}`"]" -f (NodeId $f.File), (Short $f.File)) }
    W '  end'
  }
  foreach ($e in $edges) {
    $link = if ($clusterOf[$e.A] -ne $clusterOf[$e.B]) { '-.-' } else { '---' }
    W ("  {0} {1}|{2}| {3}" -f (NodeId $e.A), $link, $e.Shared, (NodeId $e.B))
  }
  for ($i = 0; $i -lt $edges.Count; $i++) {
    W ("  linkStyle {0} stroke-width:{1}px" -f $i, [Math]::Min($edges[$i].Shared, 6))
  }
  W '```'
} else {
  W 'No cluster to draw.'
}

$outPath = [System.IO.Path]::GetFullPath($Out)
[System.IO.File]::WriteAllText($outPath, ($md -join "`n") + "`n", [System.Text.UTF8Encoding]::new($false))
"$Out · $($md.Count) lines"
