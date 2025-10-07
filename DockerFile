# ----- build stage -----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# copy everything and build
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# ----- runtime stage -----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
# Render provides PORT env var; make Kestrel listen on it
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
# (Optional) enable container-friendly thread pool
ENV DOTNET_ReadyToRun=0
ENTRYPOINT ["dotnet", "YourApiApp.dll"]
