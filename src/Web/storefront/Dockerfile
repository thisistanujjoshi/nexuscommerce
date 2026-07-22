# Build the React storefront, then serve the static bundle with nginx.
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# The browser runs on the host, so the bundle targets the host-published
# service ports (overridable at build time via VITE_* build args).
ARG VITE_CATALOG_API=http://localhost:5101
ARG VITE_ORDERS_API=http://localhost:5102
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
