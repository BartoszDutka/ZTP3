FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ShoppingCart.Api/ShoppingCart.Api.csproj", "ShoppingCart.Api/"]
COPY ["ShoppingCart.Application/ShoppingCart.Application.csproj", "ShoppingCart.Application/"]
COPY ["ShoppingCart.Domain/ShoppingCart.Domain.csproj", "ShoppingCart.Domain/"]
COPY ["ShoppingCart.Infrastructure/ShoppingCart.Infrastructure.csproj", "ShoppingCart.Infrastructure/"]
RUN dotnet restore "ShoppingCart.Api/ShoppingCart.Api.csproj"
COPY . .
WORKDIR "/src/ShoppingCart.Api"
RUN dotnet build "ShoppingCart.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShoppingCart.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShoppingCart.Api.dll"]
