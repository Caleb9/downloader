const { createProxyMiddleware } = require("http-proxy-middleware");

module.exports = function (app) {
  app.use(
    "/api",
    createProxyMiddleware({
      target: "http://localhost:5000",
    })
  );
  app.use(
    "/hub",
    createProxyMiddleware({
      target: "ws://localhost:5000",
      ws: true,
      logLevel: "warn",
    })
  );
};
