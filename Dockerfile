FROM node:26-alpine AS frontend
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props nuget.config .editorconfig ./
COPY src/LessonOrganizer.Domain/LessonOrganizer.Domain.csproj src/LessonOrganizer.Domain/
COPY src/LessonOrganizer.Application/LessonOrganizer.Application.csproj src/LessonOrganizer.Application/
COPY src/LessonOrganizer.Infrastructure/LessonOrganizer.Infrastructure.csproj src/LessonOrganizer.Infrastructure/
COPY src/LessonOrganizer.Api/LessonOrganizer.Api.csproj src/LessonOrganizer.Api/
RUN dotnet restore src/LessonOrganizer.Api/LessonOrganizer.Api.csproj
COPY src/ src/
RUN dotnet publish src/LessonOrganizer.Api/LessonOrganizer.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN mkdir -p /home/app/keys && chown "$APP_UID" /home/app/keys
COPY --from=backend /out ./
COPY --from=frontend /src/frontend/dist/lesson-organizer/browser ./wwwroot
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "LessonOrganizer.Api.dll"]
