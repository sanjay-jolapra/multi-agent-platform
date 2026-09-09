# Build context for this Dockerfile is samples/SampleInvoiceProcessor itself (NOT the repo
# root): docker build -f samples/SampleInvoiceProcessor/Dockerfile samples/SampleInvoiceProcessor
# This is correct because SampleInvoiceProcessor.csproj has no ProjectReference to any part of
# the main platform (see SampleInvoiceProcessor.csproj comments) - the project is fully
# self-contained, so it only needs its own directory as build context.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY SampleInvoiceProcessor.csproj .
RUN dotnet restore SampleInvoiceProcessor.csproj

COPY . .
RUN dotnet publish SampleInvoiceProcessor.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app .
RUN mkdir -p /app/App_Data /app/incoming /app/outgoing

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SampleInvoiceProcessor.dll"]
