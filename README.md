# DeveloperExceptionText
Middleware that captures synchronous and asynchronous exceptions from the pipeline and generates text error responses.

## Overview
The ASP.NET Core `DeveloperExceptionPageMiddleware` generates HTML error responses which are less helpful in a Web API
project accessed via a REST client. This middleware instead returns error responses in plain text that is easier to
read.

## Getting started
Install the NuGet package into your application.

### Package Manager
```
Install-Package DeveloperExceptionText
```

### .NET CLI
```
dotnet add package DeveloperExceptionText
```

## Usage
Add the following to the `Configure` method in the `Startup` class.

```csharp
if (env.IsDevelopment())
{
	app.UseDeveloperExceptionText();
}
```

Put `UseDeveloperExceptionText` before any middleware you want to catch exceptions in, such as `app.UseMvc`.
