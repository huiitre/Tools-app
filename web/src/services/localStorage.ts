const LS_TEST_KEY = 'LS_TEST_KEY'

const isLSAvailable = () => {
  try {
    localStorage.setItem(LS_TEST_KEY, 'A random value 42 51 pastis 69 la trick')
    localStorage.removeItem(LS_TEST_KEY)
    return true
  } catch (error) {
    return false
  }
}

const LS = {
  get: (key: string) => {
    if (!isLSAvailable()) return false

    const rawValue: any = localStorage.getItem(key)

    try {
      return JSON.parse(rawValue)
    } catch {
        return rawValue
    }
  },

  set: (key: string, value: any) => {
    if (!isLSAvailable()) return false
    localStorage.setItem(key, JSON.stringify(value))
    return true
  },

  delete: (key: string) => {
    if (!isLSAvailable()) return false

    localStorage.removeItem(key)

    return true
  },

  clear: () => {
    if (!isLSAvailable()) return false

    return localStorage.clear()
  },
}

export default LS