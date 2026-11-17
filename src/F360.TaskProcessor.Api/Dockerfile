FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/F360.TaskProcessor.Domain/F360.TaskProcessor.Domain.csproj", "src/F360.TaskProcessor.Domain/"]
COPY ["src/F360.TaskProcessor.Application/F360.TaskProcessor.Application.csproj", "src/F360.TaskProcessor.Application/"]
COPY ["src/F360.TaskProcessor.Infrastructure/F360.TaskProcessor.Infrastructure.csproj", "src/F360.TaskProcessor.Infrastructure/"]
COPY ["src/F360.TaskProcessor.Api/F360.TaskProcessor.Api.csproj", "src/F360.TaskProcessor.Api/"]

RUN dotnet restore "src/F360.TaskProcessor.Api/F360.TaskProcessor.Api.csproj"

COPY src/ src/
WORKDIR "/src/src/F360.TaskProcessor.Api"
RUN dotnet publish "F360.TaskProcessor.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "F360.TaskProcessor.Api.dll"]
