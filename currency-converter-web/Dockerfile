# currency-converter-web/Dockerfile
# Stage 1: Base - Node.js for building
FROM node:20-alpine AS base
WORKDIR /app

# Stage 2: Dependencies - Install dependencies
FROM base AS dependencies
COPY package*.json ./
RUN npm ci --only=production && \
    cp -R node_modules /prod_node_modules && \
    npm ci

# Stage 3: Build - Build the application
FROM dependencies AS build
COPY . .
RUN npm run build

# Stage 4: Test - Run tests (optional target)
FROM dependencies AS test
COPY . .
RUN npm run lint && \
    npm run test:run && \
    npm run build

# Stage 5: Runtime - Nginx for serving
FROM nginx:alpine AS runtime

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built application
COPY --from=build /app/dist /usr/share/nginx/html

# Add healthcheck
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost/health || exit 1

EXPOSE 80

# Add labels for better container management
LABEL org.opencontainers.image.title="Currency Converter Web"
LABEL org.opencontainers.image.description="React frontend for Currency Converter"

CMD ["nginx", "-g", "daemon off;"]
