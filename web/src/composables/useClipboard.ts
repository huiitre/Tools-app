import toast from '@/services/toast'

export function useClipboard() {
  const copy = async (value: string | number, label?: string) => {
    try {
      await navigator.clipboard.writeText(String(value))
      toast.success(label ? `${label} copié` : 'Copié')
    } catch {
      toast.error('Échec de la copie')
    }
  }

  return { copy }
}