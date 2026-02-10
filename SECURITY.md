# Security Guide

Detailed security guidance for SQL Server MCP. For an overview of the security model (query validation, parameter blocking, rate limiting), see the [Security section in the README](README.md#security).

## Connection Security and Credential Management

**Recommended: Use Windows Authentication or Azure Managed Identity**

The most secure authentication methods avoid storing credentials in configuration files entirely:

**Windows Authentication (on-premises or domain-joined environments):**
```json
{
  "SqlServerMcp": {
    "Servers": {
      "production": {
        "ConnectionString": "Server=myserver;Database=master;Integrated Security=True;TrustServerCertificate=False;Encrypt=True;"
      }
    }
  }
}
```

**Azure Managed Identity (Azure SQL Database):**
```json
{
  "SqlServerMcp": {
    "Servers": {
      "azure-prod": {
        "ConnectionString": "Server=myserver.database.windows.net;Database=master;Authentication=Active Directory Managed Identity;TrustServerCertificate=False;Encrypt=True;"
      }
    }
  }
}
```

**If SQL Authentication is Required:**

When Windows Authentication or Managed Identity are not available, follow these practices:

1. **Never commit credentials to source control** — `appsettings.json` is already gitignored, but ensure you never commit credentials in example files or documentation

2. **Use .NET configuration environment variable overrides** — .NET's `IConfiguration` system supports overriding any config value via environment variables using the `__` (double-underscore) separator. This is the recommended approach for injecting credentials without putting them in config files:

   Start with a connection string template in `appsettings.json` (no password):
   ```json
   {
     "SqlServerMcp": {
       "Servers": {
         "production": {
           "ConnectionString": "Server=myserver;Database=master;User Id=sqlreader;Encrypt=True;TrustServerCertificate=False;"
         }
       }
     }
   }
   ```

   Then override the full connection string (including the password) via an environment variable:
   ```bash
   export SqlServerMcp__Servers__production__ConnectionString="Server=myserver;Database=master;User Id=sqlreader;Password=your-secure-password;Encrypt=True;TrustServerCertificate=False;"
   dotnet run --project SqlServerMcp
   ```

   > **Note:** Some MCP clients (e.g., Claude Desktop) support `${ENV_VAR}` substitution syntax in their own configuration files, but this is **not a .NET feature** — .NET's `IConfiguration` system does not resolve `${...}` placeholders in values. Do not rely on this syntax in `appsettings.json`. Use the `__` environment variable override pattern shown above, or inject credentials through your MCP client's own environment variable support.

3. **Use secure credential stores:**
   - **Azure Key Vault** — for Azure deployments, integrate with `Azure.Extensions.AspNetCore.Configuration.Secrets`
   - **AWS Secrets Manager** — for AWS deployments, use the AWS SDK to retrieve secrets
   - **HashiCorp Vault** — for on-premises, use Vault for centralized secrets management
   - **Windows Credential Manager** — for local development on Windows

4. **Rotate credentials regularly** — if using SQL authentication, implement a credential rotation policy (e.g., every 90 days)

5. **Use strong passwords** — if SQL authentication is required, use passwords with:
   - Minimum 16 characters
   - Mix of uppercase, lowercase, numbers, and special characters
   - Generated randomly (not dictionary words or patterns)

**Connection String Encryption:**

Always use encrypted connections to protect credentials in transit:
- Set `Encrypt=True` in all connection strings
- Use `TrustServerCertificate=False` for production (only use `True` for development with self-signed certificates)
- Ensure SQL Server has a valid SSL/TLS certificate from a trusted CA

## SQL Server Account Recommendations

The SQL account used by this MCP server should follow least-privilege principles:

- **Grant read-only access** — the account only needs `SELECT` permission on the databases and schemas it should access. Do not grant `db_datawriter`, `db_ddladmin`, or server-level roles like `sysadmin`.
- **Do not grant EXECUTE on unsafe CLR assemblies** — `SELECT` statements can call user-defined functions, including CLR functions. If a CLR assembly is registered with `EXTERNAL_ACCESS` or `UNSAFE` permission sets, it can perform file I/O, network calls, and other side effects when invoked from a SELECT. The service account should not have EXECUTE permission on any such assemblies.
- **Use a dedicated service account** — do not reuse accounts shared with other applications. A dedicated account makes it easy to audit activity and revoke access independently.
- **Restrict database access** — if the account should only query specific databases, grant access only to those databases. Three-part name queries (`OtherDb.dbo.Table`) are allowed by design, so database-level permissions are the control point.
- **Consider Resource Governor** — for production SQL Server instances, place the service account in a Resource Governor workload group with CPU and memory limits to prevent expensive queries from impacting other workloads.
