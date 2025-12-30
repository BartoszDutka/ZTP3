FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ProductManagementAPI/ZtpLab1.csproj", "ProductManagementAPI/"]
RUN dotnet restore "ProductManagementAPI/ZtpLab1.csproj"
COPY . .
WORKDIR "/src/ProductManagementAPI"
RUN dotnet build "ZtpLab1.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZtpLab1.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZtpLab1.dll"]
