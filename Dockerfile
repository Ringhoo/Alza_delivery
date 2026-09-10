# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

#Development stage so I can easily call migrations and such
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
WORKDIR /src
#COPY ["Alza_delivery.csproj", "."]
#RUN dotnet restore "./Alza_delivery.csproj"
#COPY . .
EXPOSE 8080
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Alza_delivery.csproj", "."]
RUN dotnet restore "./Alza_delivery.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Alza_delivery.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Alza_delivery.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Alza_delivery.dll"]
