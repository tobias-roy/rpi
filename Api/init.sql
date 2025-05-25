CREATE DATABASE mqtt_data;

\connect mqtt_data;

CREATE TABLE wemos_data (
    id SERIAL PRIMARY KEY,
    device TEXT NOT NULL,
    co2 INT,
    temperature REAL,
    humidity REAL,
    received_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE shelly_data (
    id SERIAL PRIMARY KEY,
    device TEXT NOT NULL,
    voltage REAL,
    temperature REAL,
    received_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE USER mqtt_user WITH PASSWORD 'P@ssw0rd';
GRANT ALL PRIVILEGES ON DATABASE mqtt_data TO mqtt_user;