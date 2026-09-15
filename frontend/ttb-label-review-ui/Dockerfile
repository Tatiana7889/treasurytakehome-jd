# ---- Build stage ----
    FROM node:18-alpine AS build
    WORKDIR /app
    
    COPY package*.json ./
    RUN npm install
    
    COPY . .
    
    # Vite bakes VITE_* variables into the bundle at BUILD time (import.meta.env),
    # not read at runtime like a normal env var — so this has to be set before
    # `npm run build`, not passed to the running container afterward.
    # Render automatically exposes a Docker-based service's dashboard env vars
    # as build ARGs, so setting VITE_API_URL in the Render dashboard flows
    # through to here automatically.
    ARG VITE_API_URL
    ENV VITE_API_URL=$VITE_API_URL
    RUN npm run build
    
    # ---- Runtime stage: serve the static build ----
    FROM nginx:alpine AS runtime
    COPY --from=build /app/dist /usr/share/nginx/html
    COPY nginx.conf /etc/nginx/conf.d/default.conf
    
    EXPOSE 80
    CMD ["nginx", "-g", "daemon off;"]
    
