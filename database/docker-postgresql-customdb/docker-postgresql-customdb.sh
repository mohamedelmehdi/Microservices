#!/bin/bash

set -e
set -u

function create_database() {
	local database=keycloak
	echo "  Checking if database '$database' exists"
	DB_EXISTS=$(psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname=postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$database'")
	
	if [ "$DB_EXISTS" = "1" ]; then
		echo "  Database '$database' already exists, skipping creation."
    else
        echo "  Creating database '$database'"
        psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname=postgres <<-EOSQL
            CREATE DATABASE $database;
        EOSQL
    fi
}

if [ -n "$POSTGRES_MULTIPLE_DATABASES" ]; then
	echo "database creation requested"
	do
		create_database
	done
	echo "database keycloak created"
fi
