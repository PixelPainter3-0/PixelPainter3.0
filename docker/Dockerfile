FROM phusion/baseimage:noble-1.0.2

RUN apt update && apt install -y git dotnet-sdk-8.0 npm 

RUN mkdir -p /usr/app
WORKDIR /usr/app

RUN git clone https://github.com/JAH-guy/PixelPainter3.0.git --depth 1 --branch ndobo-test-branch

# Install npm 
WORKDIR /usr/app/PixelPainter3.0/mytestvueapp.client
RUN npm install --legacy-peer-deps

# Setup https cert
RUN mkdir -p /root/.aspnet/https/
RUN dotnet dev-certs https --export-path /root/.aspnet/https/mytestvueapp.client.pem

RUN npm run build

WORKDIR /usr/app/PixelPainter3.0/MyTestVueApp.Server

COPY appsettings.json Configuration/appsettings.json
COPY entrypoint.sh ../entrypoint.sh
RUN chmod +x ../entrypoint.sh
