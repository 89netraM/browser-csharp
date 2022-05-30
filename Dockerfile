FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

RUN curl -fsSL https://deb.nodesource.com/setup_16.x | sh -
RUN apt-get update && apt-get install -y nodejs

WORKDIR /app
COPY . ./
RUN npm ci
WORKDIR /app/example
RUN npm ci
RUN npx webpack-cli --mode production

FROM nginx:1.22.0
COPY --from=build /app/example/dist /usr/share/nginx/html
