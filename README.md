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
`src/ClaimsExpert.Service/appsettings.json` (`localhost:8888` by default). There is no
cross-platform client; use any gRPC client built from `src/ClaimsExpert.Contract`.

### Windows

Build the full solution, which adds the WPF client:

```
dotnet build src/ClaimsAdjudication.sln
```

Generate the database and start the service as above, then run the UI:

```
dotnet run --project src/ClaimsCenter.Presentation
```

---
Copyright &copy; 2012-2026 [Sergiy Nikolayev](https://github.com/snikolayev) under the [MIT license](LICENSE.txt).
