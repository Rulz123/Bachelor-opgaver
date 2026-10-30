# Partial country based rollout - FeatureHubDemo

Standalone DLS exercise, reconstructed from the supplied starter files because
neither FeatureHubDemo nor its ZIP was found. No Happy Headlines code is used.
The submission for **Partial country based rollout** is the complete
`Demo/Program.cs`. FeatureHub, not an application-side country condition, decides
whether TimelyGreetings is enabled. The earlier global-toggle exercise and its
verification evidence are preserved below as history.

## Start

```bash
cd /home/josephine/Bachelor-opgaver-w43/FeatureHubDemo
ENV_FILE=/home/josephine/Bachelor-opgaver-1/FeatureHubDemo/.env
compose=(docker compose -p featurehubdemo --env-file "$ENV_FILE")
dotnet build Demo/Demo.csproj
"${compose[@]}" config --quiet
"${compose[@]}" up -d --no-deps --build app
"${compose[@]}" logs -f app
```

FeatureHub: http://localhost:8088 (observed version: **1.10.1**).
Inside Docker the app connects to **http://featurehub:8085**, not port 8088.
For `dotnet run`, set `FEATUREHUB_EDGE_URL=http://localhost:8088` in the process
environment and supply the key privately. `dotnet run` does not load `.env`.

Josephine is checked out in `/home/josephine/Bachelor-opgaver-w43` at the exercise
baseline `011a8ce19b8c37454e57b01c4761eaef9545ad3b`. The original repository
directory remains on Mathias with its other local work untouched. Commands above
reuse the existing Compose project **featurehubdemo** and its original ignored
`.env`; they do not create a second FeatureHub server or database volume.
Do not run an unqualified `docker compose up` in the other worktree.
`.env.example` contains placeholders only.
Do not print `docker compose config` without `--quiet`: it can expose the key.
If 8088 becomes occupied, change `FEATUREHUB_HOST_PORT` in `.env` to an unused
host port. Do not change the internal 8085 port or stop unrelated projects.

## Country input and real FeatureHub targeting

The app accepts a simulated two-letter ISO country code, for example
`--country DK` or `--country SE`. This is exercise input, not IP geolocation or
an authorization mechanism. Missing country means that no country attribute
is sent to FeatureHub.

SDK 3.0.0 does **not** serialize Denmark as `DK`:
`Country(StrategyAttributeCountryName.Denmark)` serializes `country=denmark`.
Its Country method delegates to the same attribute store exposed by `Attr`.
The implementation converts ISO input using .NET RegionInfo's English name,
normalizes it to lower-case/underscores, and calls `context.Attr("country", ...)`.
Thus DK/dk sends `denmark` and SE sends `sweden`. This only translates user data;
it never selects a greeting or enables the flag based on country locally.

The context is populated **before** Build starts the SSE connection. Build/Poll
initialization waits at most ten seconds; a safe readiness message confirms that
the SDK received an initial state. No raw key-bearing SDK logging is enabled.

The existing server-evaluated READ key is reused. Final Local configuration:

- Application: `FeatureHubDemo`, portfolio: `Gruppe 1`.
- Feature key: `TimelyGreetings`, type: BOOLEAN, default value: **false**.
- Application strategy: `Denmark only`.
- Rule: Country **equals Denmark**, represented by `fieldName=country`,
  `conditional=EQUALS`, `type=STRING`, `values=["denmark"]`.
- No percentage or user-key restrictions: all matching Danish users receive true.
- Attach the application strategy to TimelyGreetings in **Local**, serving **true**.
- All nonmatching or absent countries receive the default false.
- The existing `production` environment is untouched.

To inspect/recreate this in FeatureHub 1.10.1: open Application Strategies for
FeatureHubDemo and create/reuse `Denmark only` with the Country/equals/Denmark
condition. On Features, open TimelyGreetings's **Local** cell, keep default OFF,
use Show available app strategies to attach that strategy, serve ON and Save.

**Disable/re-enable the rule in Local by detaching/re-attaching its application
strategy reference and saving.** Keep the reusable strategy definition and the
default OFF. In this observed server version, setting `disabled=true` on a
custom rule was persisted but Edge still returned true for DK; setting that
property on the environment instance did not persist it. Those attempts were
not counted as passing live tests. The verified method removes/restores the
strategy's reference in `rolloutStrategyInstances`; it does not change the
application code or restart the app.

Compose starts the main app with `--country ${GREETING_COUNTRY:-DK}`.
The country is stable for that app process. Changing the process's input requires
recreation, but changing FeatureHub targeting does not. Independent test runs:

```bash
# Use the compose array and ENV_FILE defined in Start.
"${compose[@]}" run --rm --no-deps app --country DK
"${compose[@]}" run --rm --no-deps app --country SE
"${compose[@]}" run --rm --no-deps --entrypoint dotnet app Demo.dll
"${compose[@]}" run --rm --no-deps app --self-test
```

The entrypoint override in the third command removes Compose's default country
arguments, testing genuinely missing country. For a host run, privately configure
FEATUREHUB_API_KEY and use `FEATUREHUB_EDGE_URL=http://localhost:8088` with
`dotnet run --project Demo/Demo.csproj -- --country DK`. Never print the key.

## Verified country rollout

All three app processes connected to the actual local FeatureHub server, logging
readiness for `denmark`, `sweden` and `not supplied`. No flag values were simulated.
The main DK app was rebuilt/recreated once **before** the live-test baseline.
All log timestamps below are UTC; Copenhagen was at 09:22-09:25, so the correct
enabled greeting was Good Morning, World.

| Actual input/configuration | Observed new Docker log |
| --- | --- |
| DK, Denmark rule attached | 2026-10-30T08:22:16.621521287Z Good Morning, World |
| SE, Denmark rule attached again | 2026-10-30T08:25:52.719247976Z Hello World |
| No country, Denmark rule attached again | 2026-10-30T08:25:53.005562149Z Hello World |
| DK, rule detached; API saved at 08:25:34.539Z, feature version 9 | 2026-10-30T08:25:36.639298070Z Hello World |
| DK, rule reattached; API saved at 08:25:48.685Z, feature version 10 | 2026-10-30T08:25:51.639462843Z Good Morning, World |

Every relevant management API update returned HTTP 200. Container inspection
before, during and after rule changes, including a final asserted comparison,
showed unchanged values:

```text
ID: f45f8b02167a752f506d1293caa06843bca8d833df0498416a4bc0278ec7e4aa
StartedAt: 2026-10-30T08:22:16.251992938Z
RestartCount: 0
```

Host and final Docker runtime self-tests passed for DK/dk/SE/missing country,
all greeting time boundaries, missing SDK state and Danish winter/summer time.
Build and Compose config validation passed. Final state: default OFF with the
Denmark-only strategy **attached and serving ON**. Required country-rollout tests
have all passed. Temporary SE/no-country test containers are removed after the
checks; the main DK app and existing FeatureHub remain running.

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

## Earlier global-toggle exercise

The authenticated global OFF / ON / OFF test below predates country targeting.
It is retained as historical evidence, not the current configuration. With the
current Denmark strategy attached, default OFF still serves ON to Denmark.
Use the country-rollout procedure above for the current exercise.

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

The lightweight self-test also checks ISO country conversion and
04:59/05:00, 11:59/12:00, 17:59/18:00, disabled
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

## Historical authenticated OFF / ON / OFF

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

At the end of that earlier test the flag was left OFF. For the current rule
state see Verified country rollout above. The FeatureHub database volume and other projects were
not stopped or modified. No Program.cs change was needed for the integration
test. The optional real-server retired-flag check was not run; missing-state
behaviour was previously covered by the SDK repository and fallback tests.

The SDK API was checked against the installed FeatureHub.SDK 3.0.0 README and
its source commit `9dcbd1efbd5ce7b1b4f472250bdfa424d88f27f9`:
https://github.com/featurehub-io/featurehub-dotnet-sdk/tree/9dcbd1efbd5ce7b1b4f472250bdfa424d88f27f9

## Files and data

- `Demo/Program.cs`: country input, SDK context, greeting loop and optional self-test.
- `Demo/Demo.csproj`: .NET 10 console app, FeatureHub.SDK 3.0.0.
- `Demo/Dockerfile`: official SDK/runtime multistage build, non-root runtime.
- `docker-compose.yml`: isolated app/server, environment forwarding, local port.
- `.env.example`, ignored `.env`, `.gitignore`, `.dockerignore`: local secrets
  configuration; secrets and build outputs excluded from the Docker context.
- `README.md`: startup, setup, semantics and verification status.

FeatureHub data is stored in the Compose volume `featurehub-h2-data` (currently
`featurehubdemo_featurehub-h2-data`). Keep it. Do not run `docker compose down -v`
or remove the volume. `docker compose stop` stops only this demo and retains data.
Country rollout is now configured and verified. No commit, push or Moodle
submission was performed for this exercise.