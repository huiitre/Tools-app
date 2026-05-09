import { ref, onMounted } from 'vue'

type ReleaseAsset = {
  name: string
  browser_download_url: string
}

type Release = {
  tag_name: string
  published_at: string
  body: string
  assets: ReleaseAsset[]
}

export type ChangelogEntry = {
  text: string
  hash: string
  url: string
}

export type ParsedRelease = {
  version: string
  date: string
  changelog: ChangelogEntry[]
  windows: string | null
  linux: string | null
}

function parseChangelog(body: string): ChangelogEntry[] {
  const lines = body.split('\n').filter(line => line.trim().startsWith('*'))
  return lines.map(line => {
    const match = line.match(/^\*\s+(.*?)\s+\(\[([a-f0-9]+)\]\((https?:\/\/[^\)]+)\)\)/)
    if (match) {
      return { text: match[1], hash: match[2], url: match[3] }
    }
    return { text: line.replace(/^\*\s+/, '').trim(), hash: '', url: '' }
  })
}

function parseRelease(release: Release): ParsedRelease {
  const windows = release.assets.find(a => a.name.endsWith('.exe'))?.browser_download_url ?? null
  const linux = release.assets.find(a => a.name.endsWith('.AppImage'))?.browser_download_url ?? null
  const changelog = release.body ? parseChangelog(release.body) : []

  return {
    version: release.tag_name.replace(/^v/, ''),
    date: release.published_at.split('T')[0],
    changelog,
    windows,
    linux,
  }
}

export function useReleases() {
  const latest = ref<ParsedRelease | null>(null)
  const releases = ref<ParsedRelease[]>([])
  const loading = ref(true)
  const error = ref(false)

  onMounted(async () => {
    try {
      const res = await fetch('https://api.github.com/repos/huiitre/Tools-web/releases')
      const data: Release[] = await res.json()

      const parsed = data.map(parseRelease)
      latest.value = parsed[0] ?? null
      releases.value = parsed.slice(1)
    } catch {
      error.value = true
    } finally {
      loading.value = false
    }
  })

  return { latest, releases, loading, error }
}