FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/F360.TaskProcessor.Domain/F360.TaskProcessor.Domain.csproj", "src/F360.TaskProcessor.Domain/"]
COPY ["src/F360.TaskProcessor.Application/F360.TaskProcessor.Application.csproj", "src/F360.TaskProcessor.Application/"]
COPY ["src/F360.TaskProcessor.Infrastructure/F360.TaskProcessor.Infrastructure.csproj", "src/F360.TaskProcessor.Infrastructure/"]
COPY ["src/F360.TaskProcessor.Worker/F360.TaskProcessor.Worker.csproj", "src/F360.TaskProcessor.Worker/"]

RUN dotnet restore "src/F360.TaskProcessor.Worker/F360.TaskProcessor.Worker.csproj"

COPY src/ src/
WORKDIR "/src/src/F360.TaskProcessor.Worker"
RUN dotnet publish "F360.TaskProcessor.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "F360.TaskProcessor.Worker.dll"]
