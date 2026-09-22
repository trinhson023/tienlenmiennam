const host = (typeof window !== "undefined" && window.location && window.location.hostname) ? window.location.hostname : "localhost";
const protocol = (typeof window !== "undefined" && window.location && window.location.protocol === "https:") ? "https:" : "http:";
window.__APP_CONFIG__ = {
  apiBaseUrl: `${protocol}//${host}:8888`
};
