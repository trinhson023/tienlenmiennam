import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'

const baseURL = window.__APP_CONFIG__?.apiBaseUrl || 'http://localhost:8888'
export const ACCESS_TOKEN_KEY = 'royal.access_token'
export const REFRESH_TOKEN_KEY = 'royal.refresh_token'

export const api = axios.create({ baseURL, timeout: 12000 })

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem(ACCESS_TOKEN_KEY)
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let refreshPromise: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY)
  if (!refreshToken) return null
  try {
    const response = await axios.post(`${baseURL}/api/auth/refresh`, { refreshToken })
    const next = response.data
    localStorage.setItem(ACCESS_TOKEN_KEY, next.accessToken)
    localStorage.setItem(REFRESH_TOKEN_KEY, next.refreshToken)
    return next.accessToken as string
  } catch {
    localStorage.removeItem(ACCESS_TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
    return null
  }
}

api.interceptors.response.use(response => response, async (error: AxiosError) => {
  const original = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined
  if (!original || error.response?.status !== 401 || original._retried || original.url?.includes('/api/auth/refresh')) {
    return Promise.reject(error)
  }
  original._retried = true
  refreshPromise ??= refreshAccessToken().finally(() => { refreshPromise = null })
  const token = await refreshPromise
  if (!token) return Promise.reject(error)
  original.headers.Authorization = `Bearer ${token}`
  return api.request(original)
})
