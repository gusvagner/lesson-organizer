# Running the task API locally

This API has **no registration and no login**. It trusts a JWT signed with a shared key and takes the user id from the token's `sub` claim, which must name a row in `Users`. So, to call it locally, you play the two outside systems yourself: you put a user into the database, and you sign a token for that user with the same key the API checks.

All commands are bash and run from `generative-ai-tools/`. On Windows, use Git Bash. On Linux and macOS, any bash works.

## What you need

- .NET SDK 10.0.403 (`global.json` pins it)
- Docker
- `openssl` and `curl` (both included with Git for Windows; usually already installed on Linux and macOS)

## 1. Start a SQL Server for development

```bash
export SA_PASSWORD="Dev-$(openssl rand -hex 12)-Aa1!"
docker run -d --name taskmgmt-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="$SA_PASSWORD" \
  -p 14333:1433 mcr.microsoft.com/mssql/server:2022-latest
for attempt in $(seq 1 45); do
  MSYS_NO_PATHCONV=1 docker exec taskmgmt-sql /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1 && echo "SQL Server ready" && break
  [ "$attempt" -eq 45 ] && echo "SQL Server did not start in 90 s; see: docker logs taskmgmt-sql"
  sleep 2
done
```

The loop waits up to 90 seconds, then stops with a message. If `docker run` itself failed, for example because the port is taken, it prints the error before the loop starts. Remove the container with `docker rm -f taskmgmt-sql` before trying again.

The password exists only in your shell and, after step 2, in your user secrets. Nothing is written to the repository. If port 14333 is taken, choose another one here and in the connection string in step 2. `MSYS_NO_PATHCONV=1` stops Git Bash from turning `/opt/...` into a Windows path; on other systems it does nothing.

## 2. Set the two secrets

```bash
export TASKS_JWT_SIGNING_KEY="$(openssl rand -hex 32)"
dotnet user-secrets set Jwt:SigningKey "$TASKS_JWT_SIGNING_KEY" --project src/TaskManagement.Api
dotnet user-secrets set ConnectionStrings:TaskManagement \
  "Server=127.0.0.1,14333;Database=TaskManagement;User Id=sa;Password=$SA_PASSWORD;TrustServerCertificate=true" \
  --project src/TaskManagement.Api
```

- **`Jwt:SigningKey`** must be at least 32 bytes, or the API refuses to start. Keep it ASCII, because the token script in step 5 uses it as raw bytes.
- `Jwt:Issuer` and `Jwt:Audience` are not secrets and are already in `appsettings.json`.
- User secrets are stored in your user profile, outside the repository. The API loads them only in the `Development` environment, which is what `dotnet run` uses here.

## 3. Create the database

```bash
dotnet tool restore
dotnet ef database update --project src/TaskManagement.Api
```

`dotnet ef` reads `ConnectionStrings:TaskManagement` from the same user secrets, or from the `ConnectionStrings__TaskManagement` environment variable. Without either, it falls back to a placeholder server named `design-time-only`, which is enough for `migrations add` but not for `database update`: that would fail with a "server not found" error after a network timeout.

## 4. Add a user

The API has no endpoint for users; another system owns them. Insert one row directly and keep its id:

```bash
export USER_ID=$(MSYS_NO_PATHCONV=1 docker exec taskmgmt-sql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -C -d TaskManagement -h -1 -W \
  -Q "SET NOCOUNT ON; DECLARE @id uniqueidentifier = NEWID(); INSERT INTO Users (Id, Email, Name) VALUES (@id, 'ana@example.test', 'Ana'); SELECT @id;" \
  | tr -d '\r')
echo "$USER_ID"
```

## 5. Get a token

```bash
export TOKEN=$(bash tools/mint-dev-token.sh "$USER_ID")
```

`tools/mint-dev-token.sh` signs an HS256 JWT with `TASKS_JWT_SIGNING_KEY`. The token carries:

- `sub` = the user id, which the script checks is a Guid;
- the issuer `https://identity.task-management.local` and the audience `task-management-api`, the values of `Jwt:Issuer` and `Jwt:Audience` in `appsettings.json`. If you change either one there, export `TASKS_JWT_ISSUER` / `TASKS_JWT_AUDIENCE` with the new value before minting. Otherwise every call returns a bare 401, because the API doesn't say why it rejected a token;
- a lifetime of 60 minutes. Pass a second argument (1–99999) to change it.

Call it with `bash`, as shown. That way it runs whether or not the file kept its executable bit when it was checked out.

This is the simplest honest stand-in for the identity provider that would issue tokens in a real deployment:

- it uses no extra package and no extra service;
- every claim is visible in a 20-line script;
- it uses the same key the API is configured with.

`dotnet user-jwts` was not used: it writes its own signing-key and scheme configuration (`Authentication:Schemes:Bearer`), which this API does not read.

## 6. Run the API

In a **second terminal**:

```bash
dotnet run --project src/TaskManagement.Api
```

It listens on `http://localhost:5180` (the `http` profile in `Properties/launchSettings.json`, environment `Development`).

## 7. Call it

Back in the first terminal, which still has `TOKEN` set:

```bash
API=http://localhost:5180/api/tasks
AUTH="Authorization: Bearer $TOKEN"

# create → 201 with Location
curl -i -X POST "$API" -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"title":"Plan lesson 3","description":"Past simple","dueDate":"2026-12-01"}'

# take the id from the response (or from its Location header)
TASK_ID=<id from the response>

# list → 200 (paged; ?status=Todo|InProgress|Done, ?page=1, ?pageSize=20)
curl -i "$API" -H "$AUTH"

# get one → 200
curl -i "$API/$TASK_ID" -H "$AUTH"

# replace → 200 (PUT sends the whole task; status is required)
curl -i -X PUT "$API/$TASK_ID" -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"title":"Plan lesson 3","description":"Past simple vs present perfect","status":"InProgress","dueDate":"2026-12-02"}'

# delete → 204
curl -i -X DELETE "$API/$TASK_ID" -H "$AUTH"

# the same id again → 404 ProblemDetails
curl -i "$API/$TASK_ID" -H "$AUTH"

# no token → 401 ProblemDetails
curl -i "$API"
```

Errors are `application/problem+json` (RFC 9457). Validation errors are keyed by camelCase field name, for example `{"errors":{"title":["title is required; got an empty or missing value."]}}`.

## 8. Clean up

Stop the API with Ctrl+C, then:

```bash
docker rm -f taskmgmt-sql
dotnet user-secrets clear --project src/TaskManagement.Api
```

## HTTPS

The API itself serves plain HTTP and has no HTTPS redirection or HSTS.

- **In a deployment**, TLS must end in front of it, at a reverse proxy, load balancer or platform ingress. That component holds the certificate and sends HSTS.
- **Locally**, the token and the data cross only `localhost`.

Do not expose the HTTP port directly on a network: a bearer token sent over plain HTTP can be copied and reused until it expires.
