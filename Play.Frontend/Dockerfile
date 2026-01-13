# Stage 1 — Build the frontend
FROM node:18 AS build
WORKDIR /app

# Install dependencies
COPY Play.Frontend/play-app/package*.json ./
RUN npm install

# Copy the entire app
COPY Play.Frontend/play-app/ .

# Replace the runtime config with the Docker version (if needed)
COPY Play.Frontend/play-app/public/config.js public/config.js

# Build the production bundle
RUN npm run build


# Stage 2 — Serve with Nginx
FROM nginx:stable-alpine

# Copy the React build output
COPY --from=build /app/build /usr/share/nginx/html

# Copy your custom Nginx config
COPY Play.Frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]