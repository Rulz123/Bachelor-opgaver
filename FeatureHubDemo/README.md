# TimelyGreetings - FeatureHubDemo

Standalone DLS exercise, reconstructed from the supplied starter files because
neither FeatureHubDemo nor its ZIP was found. No Happy Headlines code is used.
The submission is the complete `Demo/Program.cs`.

## Start

```bash
cd /home/josephine/Bachelor-opgaver-1/FeatureHubDemo
dotnet build Demo/Demo.csproj
docker compose config --quiet
docker compose up -d --build
docker compose logs -f app
```

FeatureHub: http://localhost:8088 (observed version: **1.10.1**).
Inside Docker the app connects to **http://featurehub:8085**, not port 8088.
For `dotnet run`, set `FEATUREHUB_EDGE_URL=http://localhost:8088` in the process
environment and supply the key privately. `dotnet run` does not load `.env`.

Compose reads the ignored local `.env`; `.env.example` contains placeholders only.
Do not print `docker compose config` without `--quiet`: it can expose the key.
If 8088 becomes occupied, change `FEATUREHUB_HOST_PORT` in `.env` to an unused
host port. Do not change the internal 8085 port or stop unrelated projects.

## Complete local setup in FeatureHub 1.10.1

Local setup is complete: portfolio `Gruppe 1`, application `FeatureHubDemo`,
environment `Local`, BOOLEAN flag `TimelyGreetings`, and service account
`FeatureHubDemo-local` with READ only in Local. The existing `production`
environment was left untouched. The SDK key is configured in the ignored `.env`
with file permissions 600. The following steps are retained for reference;
do not repeat setup or reset the existing account or keys.
Enter passwords only directly in the browser, never in chat.

1. Open http://localhost:8088/setup. Enter Name, Email address, Password and
   Confirm Password, then Next. Complete the organization setup prompts.
2. In Portfolios, use the existing exercise portfolio `Gruppe 1`.
3. In Applications, create `FeatureHubDemo` with a description. A `Production`
   environment is created automatically. In the application's environment
   settings rename that environment to `Local` (or create a separate `Local`
   environment and select it for every subsequent step).
4. In Features, create a BOOLEAN feature with the exact key `TimelyGreetings`.
   The initial boolean value is OFF and normally locked. Leave it OFF initially.
5. In Service Accounts, create `FeatureHubDemo-local`. Use Add access for the
   application, or Application Settings / Service Account Permissions. Grant
   only READ for the Local environment. The SDK does not need write permission.
6. In API Keys, select this service account, application and Local environment.
   Copy the **Server eval API Key** directly into `FEATUREHUB_API_KEY=` in the
   ignored `.env`. A Client eval key is also supported by the implementation.
   Do not paste the key into chat, source code, documentation or Moodle.
7. Run `docker compose up -d app` once to inject the new environment variable.
   This initial configuration change recreates the app. Subsequent feature
   changes must not recreate or restart it.

## Verify the real integration after setup

```bash
docker compose ps -q app
docker compose logs -f --since 1s app
```

Record the container ID. In Features select Local, unlock the feature if needed,
and save these values while watching fresh log lines:

1. OFF: `Hello World`.
2. ON: the Danish-time greeting below, normally within five seconds of delivery.
3. OFF again: output changes back in the same running container.
4. Run `docker compose ps -q app` again: the ID must be unchanged. Also compare
   `docker inspect --format '{{.State.StartedAt}}' <container-id>` before and after
   the switches to exclude a restart of the same container.
5. Optionally retire the flag temporarily in this exercise's Local environment:
   output must return to `Hello World`. Un-retire it and leave it OFF.

These are real integration checks, not proven by a simulated flag or self-test.
The authenticated OFF / ON / OFF test passed; its exact evidence is below.

## Behaviour and tests

The toggle point is the call to `Greeting` in the output loop. The toggle router
is the SDK context's `IsEnabled("TimelyGreetings")` decision. The boolean flag
selects the existing Hello World path or the timely greeting path.

Times are interpreted in Europe/Copenhagen, including daylight saving time:

| Danish local time | Enabled output |
| --- | --- |
| 05:00-11:59 | Good Morning, World |
| 12:00-17:59 | Good Day, World |
| 18:00-04:59 | Good Evening, World |

These boundaries and the Danish time zone are our interpretation, not explicit
requirements in the original exercise. Disabled, missing, unset and initially
unavailable flags produce exactly `Hello World`.

```bash
dotnet run --project Demo/Demo.csproj -- --self-test
docker compose run --rm --no-deps app --self-test
```

The lightweight self-test checks 04:59/05:00, 11:59/12:00, 17:59/18:00, disabled
and missing state, an actual empty SDK repository, and winter/summer conversion.
It runs against the same greeting function and needs no clock changes or test
framework. Passing in the final runtime image verifies its time zone support.

SDK initialization waits at most ten seconds; its SSE connection continues in
the background after a timeout. The loop rereads the SDK flag every five seconds.
Transport failures use the SDK's reconnection/backoff mechanism. SDK 3.0.0 keeps
previously received values in memory on connection loss, so the app may continue
using the **last known value**, including ON, while offline. This is not a new
valid server response. If no value was ever received, the fallback is Hello World.
Authentication/non-503 HTTP failures can permanently close the SDK stream;
correct the key/URL and recreate the app rather than expecting automatic recovery.

Raw SDK debug/error logging is deliberately not attached: its URLs and some
exceptions can include the SDK key. Initialization errors use a fixed safe message.

Verified locally before account setup:

- `dotnet build`: zero warnings and errors.
- `docker compose config --quiet`: valid, no key output.
- Docker image build and both services started successfully.
- Host and final runtime self-tests passed, including Europe/Copenhagen.
- Running app with no key repeatedly printed Hello World.
- An isolated app using an explicitly fake key and an unreachable address
  continued with Hello World after the ten-second initialization bound.
- Editor diagnostics found no errors in Program.cs, Demo.csproj or Compose.

## Verified authenticated OFF / ON / OFF

Using the existing logged-in browser session and the documented FeatureHub
v1.10.1 management API, the Local environment's actual flag was changed. Every
update returned HTTP 200. No simulated SDK values were used. The READ SDK key
was transferred locally without printing it, and Compose forwarded it along
with `FEATUREHUB_EDGE_URL=http://featurehub:8085` to the app.

Only the app container was recreated once to load the initial key, before the
test baseline was recorded. No rebuild or restart occurred during the test.

All timestamps below are UTC, as reported by the running environment and Docker:

| Actual flag | Observe logs after API response | New Docker log line |
| --- | --- | --- |
| OFF, version 2 | 2026-10-30T07:45:28.124Z | 2026-10-30T07:45:29.841509965Z Hello World |
| ON, version 3 | 2026-10-30T07:45:40.821Z | 2026-10-30T07:45:44.843669311Z Good Morning, World |
| OFF, version 4 | 2026-10-30T07:46:04.206Z | 2026-10-30T07:46:04.847062348Z Hello World |

Europe/Copenhagen was at 08:45 during the ON observation, so Good Morning was
the correct greeting. Container inspection before, during and after the test
showed the same values; the final comparison was asserted in the terminal:

```text
ID: af4004e594a59aa8006b97d2269433c2d3aedba1de7dae03fd9cbad513562b18
StartedAt: 2026-10-30T07:45:14.377356985Z
RestartCount: 0
```

The flag is left OFF. The FeatureHub database volume and other projects were
not stopped or modified. No Program.cs change was needed for the integration
test. The optional real-server retired-flag check was not run; missing-state
behaviour was previously covered by the SDK repository and fallback tests.

The SDK API was checked against the installed FeatureHub.SDK 3.0.0 README and
its source commit `9dcbd1efbd5ce7b1b4f472250bdfa424d88f27f9`:
https://github.com/featurehub-io/featurehub-dotnet-sdk/tree/9dcbd1efbd5ce7b1b4f472250bdfa424d88f27f9

## Files and data

- `Demo/Program.cs`: submitted implementation and optional self-test.
- `Demo/Demo.csproj`: .NET 10 console app, FeatureHub.SDK 3.0.0.
- `Demo/Dockerfile`: official SDK/runtime multistage build, non-root runtime.
- `docker-compose.yml`: isolated app/server, environment forwarding, local port.
- `.env.example`, ignored `.env`, `.gitignore`, `.dockerignore`: local secrets
  configuration; secrets and build outputs excluded from the Docker context.
- `README.md`: startup, setup, semantics and verification status.

FeatureHub data is stored in the Compose volume `featurehub-h2-data` (currently
`featurehubdemo_featurehub-h2-data`). Keep it. Do not run `docker compose down -v`
or remove the volume. `docker compose stop` stops only this demo and retains data.
No country rollout, commit, push or Moodle submission is included.