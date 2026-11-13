#!/usr/bin/env python3
"""Run Guest tests using disposable PostgreSQL/.NET containers, without host ports."""

import os
from pathlib import Path
import secrets
import shutil
import subprocess
import sys
import tempfile
import time


ROOT = Path(__file__).resolve().parent.parent
SDK_IMAGE = "mcr.microsoft.com/dotnet/sdk:8.0"
POSTGRES_IMAGE = "postgres:16.4"
TEST_PROJECT = "tests/CinemaTicketBooking.Api.Tests/CinemaTicketBooking.Api.Tests.csproj"


def run(*args, capture=False, check=True):
    return subprocess.run(args, check=check, text=True, capture_output=capture)


def main():
    if not shutil.which("docker"):
        raise RuntimeError("Docker CLI is required. Start Docker with Linux containers enabled.")
    run("docker", "version", "--format", "{{.Server.Version}}", capture=True)
    token = secrets.token_hex(6)
    network = "cinema-be2-0098-" + token
    postgres = network + "-postgres"
    sdk = network + "-sdk"
    database = "cinema_be2_test_" + token
    made_network = False
    owned_containers = []

    with tempfile.TemporaryDirectory(prefix="cinema-be2-tests-") as temporary:
        temporary = Path(temporary)
        password = secrets.token_urlsafe(32)
        pg_environment = temporary / "postgres.env"
        pg_environment.write_text(
            f"POSTGRES_USER=cinema_be2_test\nPOSTGRES_PASSWORD={password}\nPOSTGRES_DB={database}\n",
            encoding="utf-8",
        )
        test_environment = temporary / "test.env"
        test_environment.write_text(
            f"CINEMA_TEST_CONNECTION=Host={postgres};Database={database};Username=cinema_be2_test;Password={password}\n"
            "DOTNET_CLI_TELEMETRY_OPTOUT=1\nDOTNET_NOLOGO=1\n",
            encoding="utf-8",
        )
        for file in [pg_environment, test_environment]:
            file.chmod(0o600)

        workspace = temporary / "workspace"
        for relative in ["src/backend/CinemaTicketBooking.Api", "tests/CinemaTicketBooking.Api.Tests"]:
            shutil.copytree(ROOT / relative, workspace / relative,
                            ignore=shutil.ignore_patterns("bin", "obj", "TestResults", ".env", ".env.*", "secrets.json"))
        (workspace / "database").mkdir()
        for file in ["database-schema.sql", "seed-data.sql"]:
            shutil.copy2(ROOT / "database" / file, workspace / "database" / file)

        try:
            run("docker", "network", "create", network, capture=True)
            made_network = True
            # Register names before creation so a partially successful Docker run
            # is still cleaned up. Names are unique to this invocation.
            owned_containers.append(postgres)
            run("docker", "run", "-d", "--name", postgres, "--network", network,
                "--env-file", str(pg_environment), "--tmpfs", "/var/lib/postgresql/data",
                POSTGRES_IMAGE, capture=True)
            deadline = time.monotonic() + 45
            while time.monotonic() < deadline:
                ready = run("docker", "exec", postgres, "pg_isready", "-U", "cinema_be2_test",
                            "-d", database, capture=True, check=False)
                if ready.returncode == 0:
                    break
                time.sleep(0.5)
            else:
                raise RuntimeError("The isolated PostgreSQL instance did not become ready within 45 seconds.")

            owned_containers.append(sdk)
            run("docker", "run", "-d", "--name", sdk, "--network", network,
                "--env-file", str(test_environment), "-w", "/workspace", SDK_IMAGE,
                "sleep", "infinity", capture=True)
            run("docker", "cp", str(workspace) + os.sep + ".", sdk + ":/workspace", capture=True)
            print("Running Guest API tests against a new isolated PostgreSQL 16.4 database.", flush=True)
            result = run("docker", "exec", sdk, "dotnet", "test", TEST_PROJECT, "--nologo",
                         "--logger", "trx;LogFileName=guest-context.trx", check=False)
            artifacts = ROOT / "tests/CinemaTicketBooking.Api.Tests/TestResults"
            artifacts.mkdir(exist_ok=True)
            copied = run("docker", "cp", sdk + ":/workspace/tests/CinemaTicketBooking.Api.Tests/TestResults/.",
                         str(artifacts), capture=True, check=False)
            if copied.returncode == 0:
                print("Test results: tests/CinemaTicketBooking.Api.Tests/TestResults/guest-context.trx", flush=True)
            return result.returncode
        finally:
            for container in reversed(owned_containers):
                run("docker", "rm", "-f", container, capture=True, check=False)
            if made_network:
                run("docker", "network", "rm", network, capture=True, check=False)
            print("Removed this run's containers and network; shared databases were not accessed.", flush=True)


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (RuntimeError, subprocess.CalledProcessError) as error:
        # Do not print command arguments or environment files containing test credentials.
        print(str(error) if isinstance(error, RuntimeError) else "A Docker/test command failed; check Docker availability.",
              file=sys.stderr)
        sys.exit(1)
