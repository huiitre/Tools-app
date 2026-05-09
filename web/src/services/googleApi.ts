// src/services/googleApi.ts
const CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID_WEB
const API_KEY = import.meta.env.VITE_GOOGLE_API_KEY
const SCOPES = 'https://www.googleapis.com/auth/calendar.events'

// charge gapi + initialise le client Calendar
export async function initGapi() {
  return new Promise<void>((resolve, reject) => {
    const check = () => {
      if (window.gapi) {
        window.gapi.load('client', async () => {
          try {
            await window.gapi.client.init({
              apiKey: API_KEY,
              discoveryDocs: [
                'https://www.googleapis.com/discovery/v1/apis/calendar/v3/rest',
              ],
            })
            resolve()
          } catch (err) {
            reject(err)
          }
        })
      } else setTimeout(check, 100)
    }
    check()
  })
}

// nouvelle authentification via Google Identity Services
export function requestAccessToken(): Promise<string> {
  return new Promise((resolve, reject) => {
    const tokenClient = google.accounts.oauth2.initTokenClient({
      client_id: CLIENT_ID,
      scope: SCOPES,
      callback: (resp: { access_token?: string; error?: string }) => {
        if (resp.error) reject(resp)
        else resolve(resp.access_token!)
      },
    })
    tokenClient.requestAccessToken()
  })
}
