FROM node:24-alpine AS web-build
WORKDIR /web
COPY AzerothWebUI.Web/package*.json ./
RUN npm ci
COPY AzerothWebUI.Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY AzerothWebUI.Api/ AzerothWebUI.Api/
COPY AzerothWebUI.Core/ AzerothWebUI.Core/
RUN dotnet publish AzerothWebUI.Api -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=api-build /app/publish .
COPY --from=web-build /web/dist ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AzerothWebUI.Api.dll"]
