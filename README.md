# PixelPainter

A browser-based pixel art editor that allows users to create pixel art and share it with others. It is built with Vue.js and .NET Core.

## Required IDE Setup

[Node.JS](https://nodejs.org/en)

[Visual Studio](https://visualstudio.microsoft.com/)

[VS Code](https://code.visualstudio.com/)

[GIT](https://git-scm.com/downloads)

[Docker Desktop](https://www.docker.com/get-started/)

## Essential VS Code Extensions

Prettier - Code formatter <- *Set as default formatter*

Path Intellisense

vscode-icons

## Setup for local development

### Frontend setup in VS Code

1. Open the .Client project folder in VS Code by clicking 'File' -> 'Open Folder' and selecting the .Client inside the cloned repository folder
1. Open terminal with 'ctrl+`' *The key left of 1*
1. Run the following commands:
	```
	npm install --legacy-peer-deps
	```

### Database Setup in Visual Studio
1. Open the .sln file in the root directory of the project in Visual Studio
1. CD to return to the root directory and run the following commands:
	```
	docker-compose up -d
	```
	*This will run the database in a docker container and create the schema defined in the init.sql file*
1. Open Microsoft SQL Server Management Studio and connect to the database using the following credentials:
	```
	Server: localhost,1433
	Login: sa
	Password: PASSWORD_HERE
	Encryption: Optional
	Trust Server Certificate: Yes
	```
	*Use the same password as defined in your docker-compose.yml file*

### Backend setup in Visual Studio

1. Open the .sln file in the root directory of the project in Visual Studio
1. Create your appsettings.json file in the .server folder with key/value pairs for the following:
	- ConnectionString
	- ClientId
	- ClientSecret
	- HomeUrl
1. Run the project with the Start button in Visual Studio

## Missing HTTPS Certificates

If there is an HTTPS certificate error when running 'npm run dev', manually creating the certificate might be needed.

1. Route to the 'Roaming' folder
2. Create a new folder called 'ASP.NET' if it is not already present
3. Create a new folder in ASP.NET called 'https'
4. Copy the directory of the 'https' folder
5. Open terminal and run the following command:
	
	dotnet dev-certs https --export-path 'your directory here'/mytestvueapp.client.pem 
	
