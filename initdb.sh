#!/bin/bash
POSTGRES_HOST_default="localhost"
POSTGRES_PORT_default="5432"
read -p "Enter your database username: " POSTGRES_USERNAME
read -p "Enter your database password: " POSTGRES_PASSWORD
read -p "Enter your database name: " POSTGRES_DATABSE
read -p "Please enter your database hostname(leave empty if your hostname is localhost): " POSTGRES_HOST && [[ -z "$POSTGRES_HOST" ]] && POSTGRES_HOST="$POSTGRES_HOST_default"
read -p "Please enter your database port number(leave empty if your portnumber is 5432): " POSTGRES_PORT && [[ -z "$POSTGRES_PORT" ]] && POSTGRES_PORT="$POSTGRES_PORT_default"
sql="$(<"./drop.sql")"
# Connect to the database, run the query, then disconnect
PGPASSWORD="${POSTGRES_PASSWORD}" `which psql` -t -A \
-h "${POSTGRES_HOST}" \
-p "${POSTGRES_PORT}" \
-d "${POSTGRES_DATABASE}" \
-U "${POSTGRES_USERNAME}" \
-c "${sql}"
rm -rf ./Migrations/
~/.dotnet/tools/dotnet-ef migrations add "Add_new_tables"
~/.dotnet/tools/dotnet-ef database update

