FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY be.csproj ./
RUN dotnet restore "be.csproj"

COPY . ./
RUN dotnet publish "be.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV PORT=8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "be.dll"]
