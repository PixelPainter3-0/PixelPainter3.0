###
### This script runs on startup of the docker container containing the sql server
###
### MUST ENSURE LINE ENDINGS ARE SET TO UNIX STYLE LF ###

#!/bin/sh
# Start SQL Server
echo "Starting..."
/opt/mssql/bin/sqlservr &


# Wait for the SQL Server to come up
until /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalPassword123" -Q "SELECT 1" -C;
do
  sleep 1;
done

# Run the setup script to create the DB and the schema in the DB
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "LocalPassword123" -i init.sql -C;

# Keep the container running
wait