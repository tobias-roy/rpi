# RPI
## Contents

- [Description](#description)
- [Raspberry Setup](#raspberry-pi-5-setupRaspberry)
- [Docker](#docker)
- [Mosquitto Broker](#mosquitto-broker)
- [Ngrok](#ngrok)
- [.NET Minimal API & PostgreSQL](#net-minimal-api-and-postgresql)
- [Deploying API & PostgreSQL](#deploying-api-to-raspberrypi)
- [Topics](#topics)
    - [Shelly Topics](#shelly-topics)
    - [Wemos Topics](#wemos-topics)
- [API Request/Response](#api-requestresponse)
  
## Description
This project utilizes a RaspberryPi 5 and various frameworks to get, store and visualize data from various sensors. The project uses data delivered by a Shelly UNI located on a boat in a danish harbor, and multiple wemos d1 minis located in various places.

<img src="Images\ctx.png" width="600"/>

## Raspberry Pi 5 Setup
First you need to accuire an RPI. I went with the Raspberry Pi 5 with 4gbs of RAM and preliminary a 32Gb SD Card from Sandisk.

#### OS Installation

- Donwload and install [Raspberry Pi Imager](https://www.raspberrypi.com/software/)

`brew install --cask raspberry-pi-imager`

-  Insert your SD-Card into your computer and load up Raspberry Pi Imager.
- Press _Choose Device_ and choosed the device you're going to be using.
- Press _Choose OS_ and choose _Raspberry Pi OS (Other)_
- Press _Raspberry Pi OS Lite (64-bit)_
- Press __CMD + SHIFT + X__ this opens up OS Customisation.

Now it's time to configure your RPI with the settings you prefer.

Set the hostname to what you like for your SSH connection.

Set a _Username_ and _Password_ that you can remember.

Insert the __SSID__ and __Password__ for the WiFi your RPI should connect to, and choose the country in which the WiFi is.

Choose the right locale settings and keyboard layout you're going to be using.

Press __SERVICES__ and make sure _Enable SSH_ is marked with Password Authentication
<img src="Images\SSHSetup.png" width="300"/>

- Finish the setup by writing the OS to the SD-card you are using and insert it into your Raspberry Pi and turn it on.

#### Connecting with SSH
- Make sure your PC is on the same Network as your Raspberry Pi.

- Open up a terminal and insert
`ssh username@hostname.local`

- Fill out the password you're prompted for and press Enter.

You should now be connected to your Raspberry Pi! Now it's time to make sure we're up to date with everything.
- Run the following commands

`sudo apt update` - to refresh your Debian package list.

`sudo apt upgrade` - to update your packages to the latest version.

#### Fallback WiFi for supporting multiple locations
```
ctrl_interface=DIR=/var/run/wpa_supplicant GROUP=netdev
update_config=1
country=DA

network={
    ssid="NOKIA-2081"
    psk="NAKZ7bptq6"
    priority=1
}

network={
    ssid="SibirienAP"
    psk="Siberia51244"
    priority=2
}
```


## Docker
Using docker on your RPI is not a requirement to run Mosquitto, but the containerised environment can be efficient in many use cases, so i do recommend this approach.

Installing docker is very straight forward.

- Run the following commands

`sudo apt install docker docker.io docker-compose` - Installs docker

`sudo systemctl start docker` - Starts docker

- Change out username then run 

`sudo usermod -aG docker <username>` - Adds the username to the Docker group

`sudo reboot` - Restarts the RPI

- After reconnecting when your RPI has rebooted, check to verify that docker is indeed installed correctly

`docker ps` - You should be presented with an empty list of docker containers.

## Mosquitto Broker
#### Basic installation
To setup the mosquitto broker in a container we first need a directory tree for the purpose of configuring the broker correctly.

`mkdir -p docker/mosquitto/{config,data,log}` - This will create the directories needed.

`nano docker/mosquitto/config/mosquitto.conf` - This will create and open the mosquitto.conf file. Insert the following content:

```
listener 1883
#protocol websockets
persistence true
persistence_location /mosquitto/data/
log_dest file /mosquitto/log/mosquitto.log
allow_anonymous false
```

Now it's time to create the yaml file for docker-compose (Remember to mind the indentaion YAML is 4 spaces per indentation __NOT TABS__).

`cd docker`

`nano docker-compose.yaml` - Insert the snippet
```
version: '3'

services:
    mosquitto:
        container_name: mosquitto
        restart: always
        image: eclipse-mosquitto
        ports:
            - "1883:1883"
            - "9001:9001"
        volumes: 
            - ./mosquitto/config/mosquitto.conf:/mosquitto/config/mosquitto.conf
            - ./mosquitto/data:/mosquitto/data
            - ./mosquitto/log:/mosquitto/log
        networks:
            - default

networks:
    default:
```

Now you can compose the image and run mosquitto with your configuration

`docker compose up -d`

To check and see if mosquitto is running use `docker ps` and you should be presented with a mosquitto container in the overview.

<img src="Images\mosquitto.png" width="auto"/>


#### Adding Usernames and Passwords
Having a broker completely unprotected is not a good idea, so ofcourse we're going to implement a very basic form of protection, username/password authentication.

Running the following command will execute a temporary shell in the mosquitto image making you able to add and edit the files etc.

`docker exec -it mosquitto sh`

Replace the _username_ with what you would like for authentication.

`mosquitto_passwd -c mosquitto/data/pwfile username`

Entering that command you will be prompted with the option to enter a password, enter one and remember what you wrote.

Type `exit` to exit the shell.

In order to make Mosquitto use the pwfile run the following commands

`sudo docker stop mosquitto` - To stop the instance

`sudo nano docker/mosquitto/config/mosquitto.conf` - To open up the configuration file.

Add this line to the end of your configuration file:

```
password_file mosquitto/data/pwfile
```

Now restart the broker

`sudo docker start mosquitto`

Check if the broker has restarted with `docker ps`

You should now have a functioning broker running on port 1883, without TLS/SSL but with basic authentication through username and password.

We will set up TLS/SSL later on.

#### Adding more username/password combinations
Navigate to the Docker instance of Mosquitto. 

To add a new username and password combination to the broker use the following command at the location of your passwordfile replace filename username and password with the correct values for your project.

`mosquitto_passwd -b filename username password`

## Ngrok
Currently our Raspberry Pi is restricted to our home network, but i would like to utilize the broker so it can recieve some telemetry data from the great big internet. I do not have a static IP with my provider and i do not want to open my ports up to anyone with possible mallicious intent, in comes Ngrok.

- Create a user on the [Ngrok website](https://ngrok.com/)

- Run this command to download & install the latest version og Ngrok on your RPI
```
curl -s https://ngrok-agent.s3.amazonaws.com/ngrok.asc | sudo tee /etc/apt/trusted.gpg.d/ngrok.asc >/dev/null && \
echo "deb https://ngrok-agent.s3.amazonaws.com buster main" | sudo tee /etc/apt/sources.list.d/ngrok.list && \
sudo apt update && sudo apt install ngrok
```

- Go to the Ngrok dashboard on their website and find your Auth Token

<img src="Images\authtoken.png" width="auto"/>

- Copy your authentication token and use the following command to add it to Ngrok on your Rpi

`ngrok config add-authtoken YOUR_AUTH_TOKEN`

You should now be able to setup a tunnel with the free Ngrok plan.

`ngrok tcp 1883` - This exposes 1883 through the security of Ngrok.

To connect to your MQTT broker use the Ngrok Ip and Port displayed marked with orange.

<img src="Images\ngrok.png" width="auto"/>

__This changes!__ Every time you initialize Ngrok this ip and port will be randomly generated as long as you are using the free plan.

## .NET Minimal API and PostgreSQL
This section is based on local development with deployment to the RPI, but it utilizes your local PC in the creation of the project etc.

- Create a folder for your project whereever you want.

`mkdir RpiMinimalApi`

- Create a new minimal API

`dotnet new web -n Api`

- Add a models folder

`cd Api && mkdir Models`

- Add packages for MQTT and PostgreSQL

`dotnet add package MQTTnet && dotnet add package Npgsql`

- Create the database init script in your projects root folder _RpiMinimalApi/_

```sql
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
    received_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE USER mqtt_user WITH PASSWORD 'yourpassword';
GRANT ALL PRIVILEGES ON DATABASE mqtt_data TO mqtt_user;
```

The script above is used for this JSON
```json
{
    "co2":720,
    "temperature":24.8,
    "humidity":45.2,
    "device":"WhiteBird"
}
```

In regards to the Shelly device it only delivers a payload of the voltage reading via plaintext MQTT.
```
Topic: shellies/shellyuni-<device_id>/adc/0
Payload: 12.34
```

Create the C# Models corresponding with the data you're reciving in your models folder. 
```C#
namespace Api.Models;

public class D1Payload
{
    public int Co2 { get; set; }
    public float Temperature { get; set; }
    public float Humidity { get; set; }
    public string Device { get; set; } = "";
}

public class ShellyVoltage
{
    public float Voltage { get; set; }
    public string DeviceId { get; set; } = "";
}

public class ShellyTelemetry
{
    public float Celcius { get; set; }
    public string DeviceId { get; set; } = "";
}
```

## Deploying API To RaspberryPi
To deploy the API to your RPI run the following commands in this series:

`rsync -avz ./Api --rsh=ssh roy@royhome.local:/home/roy/dotnetbuild -I` - This will transfer the files from /Api folder when you're in project root to home/roy/dotnetbuild/Api on your raspberry Pi.

`docker-compose up --build -d` - Compose Project

`docker-compose down` - De-Compose Project

`cd .. && sudo rm -rf Api` - Go up one folder and remove Api folder.

`docker logs -f mqtt-api` - To view logs in realtime on the Api

`docker system df -v` - To view the size of docker images and containers

`docker update --restart always mqtt-api` - This will make sure the API always starts on boot and if it shuts down unexpectetly

## Topics
### Wemos topics
#### ADC Reading (Voltage)
`home/birdie` - Topic for Wemos publish messages

`{"co2":840,"temperature":25.23,"humidity":43.81,"device":"WhiteBird"}` - JSON Payload
_____
### Shelly Topics
#### ADC Reading (Voltage)
`shellies/shellyuni-deviceid/adc/0` - Topic for ADC Voltage reading

`13.22` - Plaintext Payload
_____

#### Temperature Reading (C / F)

`shellies/shellyuni-deviceid/ext_temperatures` - Topic for temperature in Celcius

`{"0":{"hwID":"2876a73800000002","tC":21.0}}` - JSON Payload

_____

`shellies/shellyuni-deviceid/ext_temperatures_f` - Topic for temperature in Fahrenheit

`{"0":{"hwID":"2876a73800000002","tF":69.8}}` - JSON Payload
_____

## API Request/Response
The postgres database handles datetimes as ISO format i.e
```
2025-01-01T00:00:00Z
```

### Wemos Endpoint
#### Data format
|Property|Type|
|---|---|
|id|int|
|co2|float|
|temperature|float|
|humidity|float|
|device|string|
|recieved_at|datetime|

#### Request Url
`/wemos/historical?from=iso_datetime&to=iso_datetime`

#### Response Body within range
```
[
  {
    "id": 318,
    "co2": 857,
    "temperature": 20.5,
    "humidity": 58.12,
    "device": "WhiteBird",
    "received_at": "2025-05-28T04:12:13.978071"
  },
  {
    "id": 317,
    "co2": 890,
    "temperature": 21.02,
    "humidity": 51.09,
    "device": "BlackBird",
    "received_at": "2025-05-28T04:10:13.546069"
  }
]
```
---