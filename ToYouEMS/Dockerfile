# Step 1: Use .NET SDK to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build


# Set the working directory inside the container
WORKDIR /app

# Copy the project files
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build the project
COPY . ./
RUN dotnet publish -c Release -o out || exit 1

# Step 2: Use a smaller runtime image for deployment
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install networking tools
# Switch to root user to install packages
USER root

# Install networking tools
RUN apt-get update && \
    apt-get install -y curl iputils-ping net-tools procps && \
    rm -rf /var/lib/apt/lists/*


# Set the working directory
WORKDIR /app

# Copy the built files from the previous step
COPY --from=build /app/out .
COPY --from=build /app/Storage ./StorageCopy
COPY init.sh /app/init.sh

# Expose the port the app runs on
EXPOSE 8080

# Define the entry point
CMD ["sh", "-c", "/app/init.sh && sleep 10 && dotnet ToYouEMS.dll"]

