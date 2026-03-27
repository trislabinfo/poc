# pgAdmin configuration

- **servers.json** – Pre-defines the Postgres server in pgAdmin so it appears under "Servers" when you open pgAdmin. The host `dr-development-db` is the Aspire/Docker resource name for the Postgres container. Credentials are read from the **pgpass** file (see below).
- **pgpass** – Created by the setup script (`scripts/setup-dev-environment.ps1`) in the AppHost directory (one level up from `conf`). It contains one line: `dr-development-db:5432:*:datarizen:datarizen!` so pgAdmin can connect without prompting. Do not commit `pgpass`; add it to `.gitignore` if present.

AppHost mounts these into the pgAdmin container when the files exist. Run the setup script once so `pgpass` is created; then start AppHost and open pgAdmin – you log in with `admin@datarizen.com` / `dr-development`, and the "Datarizen Dev" server is already listed and connects without asking for the Postgres password.
