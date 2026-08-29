# NRules Demo

This is a full stack demo application that uses NRules rules engine.

## Building and running

The solution targets .NET 10. The claims expert service, the rules and the data
generator are cross-platform; the ClaimsCenter UI is WPF and therefore Windows-only.

### Linux / macOS

Build the cross-platform projects through the solution filter, which excludes the two
WPF projects:

```
dotnet build src/ClaimsAdjudication.Linux.slnf
```

Generate the SQLite database, then start the service:

```
dotnet run --project src/DataGenerator
dotnet run --project src/ClaimsExpert.Service
```

The service listens for gRPC over plaintext HTTP/2 on the endpoint configured in
`src/ClaimsExpert.Service/appsettings.json` (`localhost:8888` by default).

The cross-platform client is `ClaimsCenter.Cli`, which issues the same actions as the WPF UI:

```
dotnet run --project src/ClaimsCenter.Cli -- list
dotnet run --project src/ClaimsCenter.Cli -- show 1
dotnet run --project src/ClaimsCenter.Cli -- adjudicate 1
```

`adjudicate` re-fetches the claim and prints it, so the alerts the rules produced are visible
straight away. `--json` switches any of the three to machine-readable output, `--version` prints
the product name and version, and `--endpoint` or the `grpcEndpointAddress` environment variable
overrides the address in `src/ClaimsCenter.Cli/appsettings.json`. The CLI exits 0 on success, 1 on
a usage error and 2 when the request fails, including when the service is not running.

### Windows

Build the full solution, which adds the WPF client:

```
dotnet build src/ClaimsAdjudication.sln
```

Generate the database and start the service as above, then run the UI:

```
dotnet run --project src/ClaimsCenter.Presentation
```

The CLI is part of the full solution and contains no platform-specific code, so the same
commands are available on Windows.

## Service configuration

The service reads `grpcEndpointHostname`, `grpcEndpointPort` and `databaseFile` from the
`appsettings.json` next to its binary. The file is located relative to the binary rather
than the current directory, so the service behaves the same however it is started; one
consequence is that the `--contentRoot` switch and the `ASPNETCORE_CONTENTROOT`
environment variable have no effect. Individual settings can still be overridden on the
command line or through the environment:

```
dotnet run --project src/ClaimsExpert.Service -- --databaseFile=/var/lib/claimsexpert/ClaimsExpert.sqlite
databaseFile=/var/lib/claimsexpert/ClaimsExpert.sqlite dotnet run --project src/ClaimsExpert.Service
```

The service listens on IPv4 only. `grpcEndpointHostname` accepts an IPv4 address, a host
name, or `*` to listen on every IPv4 interface; a host name is bound on every IPv4 address
it resolves to, skipping with a warning any that cannot be bound. An IPv6 address is
rejected at startup.

Because of that, clients should address the service by IPv4 rather than by `localhost`,
which is why `grpcEndpointAddress` in the UI is `http://127.0.0.1:8888`. Windows resolves
`localhost` to `::1` first, and .NET tries resolved addresses in order, so the connection
starts against an address nothing is listening on. Windows retransmits the refused `SYN`
rather than failing immediately, which adds a delay of up to a second or two before the
attempt on `127.0.0.1` succeeds.

### Deploying a published build

`databaseFile` defaults to a path relative to the build output, which only resolves
correctly when running from the development layout. A published build therefore has to be
told where the database lives, and fails at startup with a message naming the setting
until it is:

```
dotnet publish src/ClaimsExpert.Service -c Release -o /opt/claimsexpert
databaseFile=/var/lib/claimsexpert/ClaimsExpert.sqlite /opt/claimsexpert/NRules.Samples.ClaimsExpert.Service
```

Under systemd the equivalent is an `Environment=databaseFile=...` line in the unit. The
service integrates with systemd notification and shuts down cleanly on `SIGTERM`.

The same binary also runs as a Windows service; it detects its service manager at startup
and behaves as a console application when started from a shell on either platform:

```
sc.exe create ClaimsExpert binPath= "C:\claimsexpert\NRules.Samples.ClaimsExpert.Service.exe" DisplayName= "Claims Expert" start= auto
sc.exe description ClaimsExpert "Claims expert service"
sc.exe start ClaimsExpert
```

`sc.exe` needs an elevated prompt, and the space after each `=` is part of its syntax.
`start= auto` is what makes the service start on boot; without it the service is created
but only ever starts on demand. To remove it again, `sc.exe delete ClaimsExpert`.

Set `databaseFile` for a Windows service through the machine or service environment, since
a service has no shell to set it in.

---
Copyright &copy; 2012-2026 [Sergiy Nikolayev](https://github.com/snikolayev) under the [MIT license](LICENSE.txt).
