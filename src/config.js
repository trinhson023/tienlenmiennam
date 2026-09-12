export const GAME_SERVER_PORT = 8000;

const isBrowser = typeof window !== "undefined" && window.location;
const defaultHost = isBrowser ? window.location.hostname : "localhost";
const defaultPort = isBrowser && window.location.port ? `:${window.location.port}` : `:${GAME_SERVER_PORT}`;
const defaultProtocol = isBrowser && window.location.protocol ? window.location.protocol : "http:";

export const GAME_SERVER_URL = `${defaultProtocol}//${defaultHost}${defaultPort}`;
export const WEB_SERVER_URL = `${defaultProtocol}//${defaultHost}${defaultPort}`;
export const APP_PRODUCTION = true;
export const LOBBY = true;
