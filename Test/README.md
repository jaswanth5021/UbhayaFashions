# Ubhaya Fashions test projects

All unit-test projects are organized under this folder:

```text
Test/
├── Frontend unittest/
│   ├── Controllers/
│   ├── LadiesDressStore.Web.Tests.csproj
│   └── README.md
└── Backend unittest/
    ├── Controllers/
    ├── UbhayaFashions.Tests.csproj
    └── README.md
```

## Run tests

Run commands from the repository root.

Frontend / ASP.NET Core MVC:

```bash
dotnet test "Test/Frontend unittest/LadiesDressStore.Web.Tests.csproj"
```

Backend / ASP.NET Core API:

```bash
dotnet test "Test/Backend unittest/UbhayaFashions.Tests.csproj"
```

The projects are independent. Frontend tests reference the MVC application under `frontend/`; backend tests reference the API project under `backend/`.

## Coverage status

These are starter unit-test suites. They do not yet cover every method/action, and neither suite has been executed in this environment. Run both commands and address compile/runtime failures before treating the tests as verified.
