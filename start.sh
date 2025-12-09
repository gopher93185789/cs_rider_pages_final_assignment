#!/bin/bash

echo "Docker containers bouwen en starten..."
docker compose up --build -d

echo ""
echo "Wachten tot services gezond zijn..."
sleep 5

echo ""
echo "Service Status:"
docker compose ps

echo ""
echo "Applicatie is aan het opstarten!"
echo ""
echo "Webapplicatie: http://localhost:8081"
echo "PostgreSQL: localhost:3101"
echo ""
echo "Bekijk logs met: docker compose logs -f"
echo "Stop met: docker compose down"
