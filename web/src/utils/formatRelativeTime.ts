export function formatRelativeTime(date: Date | string): string {
  const ms = Date.now() - new Date(date).getTime()
  const seconds = Math.floor(ms / 1000)
  const minutes = Math.floor(ms / (1000 * 60))
  const hours = Math.floor(ms / (1000 * 60 * 60))
  const days = Math.floor(ms / (1000 * 60 * 60 * 24))
  const weeks = Math.floor(days / 7)
  const months = Math.floor(days / 30)
  const years = Math.floor(days / 365)

  if (seconds < 10) return "à l'instant"
  if (seconds < 60) return `il y a ${seconds} sec`
  if (minutes < 60) return `il y a ${minutes} minute${minutes > 1 ? 's' : ''}`
  if (hours < 24) return `il y a ${hours} heure${hours > 1 ? 's' : ''}`
  if (days < 7) return `il y a ${days} jour${days > 1 ? 's' : ''}`
  if (weeks < 4) return `il y a ${weeks} semaine${weeks > 1 ? 's' : ''}`
  if (months < 12) return `il y a ${months} mois`
  return `il y a ${years} an${years > 1 ? 's' : ''}`
}
