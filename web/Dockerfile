FROM node:22-alpine AS builder
WORKDIR /app
COPY web/package.json web/tsconfig.json web/vite.config.ts web/index.html ./
RUN npm install --no-audit --no-fund
COPY web/src ./src
COPY web/public ./public
RUN npm run build

FROM nginx:1.27-alpine
COPY web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /app/dist /usr/share/nginx/html
EXPOSE 80
