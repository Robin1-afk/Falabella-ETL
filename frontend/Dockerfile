# ── Etapa de compilación con Node ─────────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json .
RUN npm ci --prefer-offline
COPY . .
RUN npm run build -- --configuration production

# ── Etapa de producción con nginx ─────────────────────────────────────────────
FROM nginx:1.27-alpine AS runtime
# El builder de Angular 18 genera los estáticos en dist/frontend/browser/
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
