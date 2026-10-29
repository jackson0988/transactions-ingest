export const environment = {
  production: true,
  // prod: SPA is served by the API itself (wwwroot), so same origin.
  // if the UI is hosted separately put the full API url here, e.g. https://api.example.com/api
  apiBaseUrl: '/api',
  defaultPageSize: 25,
};
