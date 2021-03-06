<!--title:Jasper Command Line Support-->

<[info]>
Jasper uses the related [Oakton](https://jasperfx.github.io/oakton) project for command line parsing and its command runner extensibility. "Oakton" is
a small community just to the North of Jasper's namesake.
<[/info]>

At this time, the Jasper team is focused on hosting applications either in IIS (or nginx) or as a console application that would be suitable for
running in a Docker container. To that end, we've added the `JasperHost` static class as a helper for standing up Jasper in a console application. You obviously want to run the application from a command line, and Jasper certainly does that, but the real value is the additional diagnostic commands
documented on this page that will help you diagnose problems or just generally understand your Jasper application better. The command line usage is also extensible.

If you're using `IWebHostBuilder` to bootstrap your application, you can opt into Jasper's expanded command line support with code similar to this hybrid MVC Core / Jasper application that utilizes an extension method called `RunJasper(args)` to execute a Jasper application at the command line:

<[sample:MvcCoreHybrid.Program]>

Likewise, this sample usage from the <[linkto:documentation/getting_started;title=getting started]> topic for a headless Jasper application
 could look like this:

<[sample:QuickStartConsoleMain]>

At runtime, `JasperHost` can use the `JasperRegistry` you hand it to <[linkto:documentation/bootstrapping;title=bootstrap a IJasperHost]> and run the application until the console process is stopped.

Or again, if you'd prefer to bootstrap with `IWebHostBuilder`, you can still use `JasperHost` like this:

<[sample:simplest-aspnetcore-run-from-command-line]>

You can also use the command line arguments to customize how the application runs like this:

<[sample:JasperHost-programmatic-customization]>

Or like this:

<[sample:JasperAgent-programmatic-construction]>

Let's say that your Jasper application compiles to `MyApp.exe` and uses the `JasperHost` class
to run the commands. In that case you can run your application simply by typing `MyApp` at the
command line with no arguments.

## Overriding the Environment Name

For example, you can also use this syntax to run your application in "Development" mode:

```
MyApp run --environment Development
```

or 

```
MyApp run -e Development
```

If you run this command, your application will start with `JasperRegistry.EnvironmentName` equal to _Development_. If you programmatically set the environment name in your `JasperRegistry`, that
setting will win out over the command line flag.

## Running with Verbose Console Tracing

Likewise, to see more verbose information on start up and runtime console tracing, use:

```
MyApp run --verbose
```

or 

```
MyApp run -v
```

The `-v / --verbose` flags add console and debug logging to your system. It's the equivalent to calling:

## Overriding the LogLevel

You can also override the log level of your application to any valid value of the [LogLevel enumeration](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel?view=aspnetcore-2.0) like this:

```
MyApp run --log-level Information
```


## Validating the Configured Application

You may want to simply try to bootstrap the application and run all the <[linkto:documentation/bootstrapping/environment_tests;title=environment tests]> and report out the results. That syntax is:

```
MyApp validate
```

which also respects the same `--environment` and `--verbose` flags as the run command. This command will bootstrap the application, run all the environment tests and start up validations, report on the success or failure, and shut down the application. Do note that if any environment tests fail, this command will return a non-zero return code that should be sufficient to let any
build scripting tool you're using know that the validation failed.

The `validate` command will also validate the Lamar configuration and run any environment tests exposed through Lamar.

## List Registered Services

Jasper only supports the [Lamar container](https://github.com/jasperfx/lamar) (the replacement for the venerable [StructureMap](http://structuremap.github.io) container). To query the current state of service registrations, use this command:

```
MyApp services
```

And again, this command respects both the `--environment` and `--verbose` flags



## Preview Generated Code

One of the easiest ways to debug message or HTTP handlers -- or just to understand their behavior -- is to read the generated code
that Jasper is using to actually handle a specific message type or HTTP route. You can preview that code by using this command:

```
dotnet run -- code
```

Or to only see the code for message handlers:

```
dotnet run -- code messages
```

Or to only see the code for any HTTP handlers:

```
dotnet run -- code routes
```

Finally, to dump the results to a file, use the `--file` flag like this:

```
dotnet run -- code --file generated.cs
```

or 

```
dotnet run -- code -f generated.cs
```

As usual, this command also respects both the `--environment` and `--verbose` flags


## Describe Command

Just to preview information about your Jasper application, there's also the `describe` command that is used like this:

```
dotnet run -- describe
```

This will at least tell you what message types are handled and some basic information about any HTTP hosting.

## Custom Commands

The Jasper.Console package uses the [Oakton](http://jasperfx.github.io/oakton) library for its command line support. You can add custom commands to your Jasper application by simply including `OaktonCommand<T>` classes in either the main application assembly or in any assembly that is decorated with the `[JasperModule]` attribute like so:

<[sample:UseJasperModule-with-Extension]>

or without any kind of extension like so:

<[sample:AppWithExtensions]>

If you want to write a command that uses the actual Jasper application, use the `JasperInput` class as either the input to your
command or as the superclass to your input class:

<[sample:JasperInput]>

To make that more concrete, here is how the built in `services` command uses `JasperInput` type to build out and use the running system:

<[sample:ServicesCommand]>

Do note that the command will be responsible for disposing and shutting down the running `IJasperHost`.


## Message Storage

See <[linkto:documentation/messaging/transports/durable]> for documentation on using the command line tooling to manage message storage.

