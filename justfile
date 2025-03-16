default: up 
up: down build run

build:
    cd ToYouEMS && docker build -t toyosoft-ems-back .
    cd ../resume-mentor && docker build -t toyosoft-ems-front .

run:
    docker compose up -d

down:
    docker compose down react-app
    docker compose down dotnet-app
    
log:
    docker compose logs -f


commit:
    git add --all
    -git commit -m "update"
    cd ../resume-mentor
    git add --all
    -git commit -m "update"

