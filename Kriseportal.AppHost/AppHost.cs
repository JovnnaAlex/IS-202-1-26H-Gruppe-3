var builder = DistributedApplication.CreateBuilder(args);

// Passordet brukes bare til lokal utvikling på egen maskin.
const string dbPassord = "rootpassword";

// MariaDB startes som en Docker-container av Aspire.
// Port 3307 på PC-en, så den ikke kolliderer med den gamle databasen på 3306.
var mariadb = builder.AddContainer("mariadb", "mariadb", "11.4")
    .WithEnvironment("MARIADB_ROOT_PASSWORD", dbPassord)
    .WithEnvironment("MARIADB_DATABASE", "kriseportal")
    .WithEndpoint(port: 3307, targetPort: 3306, name: "tcp")
    .WithVolume("kriseportal-data", "/var/lib/mysql")
    .WithLifetime(ContainerLifetime.Persistent);

var apiService = builder.AddProject<Projects.Kriseportal_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Kriseportal_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WaitFor(mariadb);

builder.Build().Run();
