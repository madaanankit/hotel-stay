#!/usr/bin/env bash
set -e

# Create solution and projects
mkdir -p hotel-stay
cd hotel-stay

echo "Creating solution HotelStay.sln"
dotnet new sln -n HotelStay

echo "Creating Minimal API project HotelStay.Api"
dotnet new webapi -minimal -o HotelStay.Api -f net10.0

echo "Creating xUnit test project HotelStay.Tests"
dotnet new xunit -o HotelStay.Tests -f net10.0

cd HotelStay.Tests

echo "Adding project reference to HotelStay.Api"
dotnet add reference ../HotelStay.Api/HotelStay.Api.csproj

cd ..

echo "Adding projects to solution"
dotnet sln add HotelStay.Api/HotelStay.Api.csproj
dotnet sln add HotelStay.Tests/HotelStay.Tests.csproj

echo "Done. You can build with: dotnet build HotelStay.sln"
