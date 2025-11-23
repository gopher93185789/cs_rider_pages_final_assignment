# Docker Deployment Handleiding

Deze handleiding beschrijft hoe u de applicatie kunt runnen met Docker.

## Snel Starten

Bouw en start de volledige applicatie met Docker Compose:

```bash
docker compose up --build
```

De applicatie zal beschikbaar zijn op:

- **Webapplicatie**: http://localhost:8080
- **PostgreSQL Database**: localhost:3100

## Services

### Webapplicatie (webapp)

- Gebouwd vanuit multi-stage Dockerfile
- Draait op .NET 9.0
- Beschikbaar op poort 8080
- Maakt automatisch verbinding met PostgreSQL database
- Health checks geactiveerd

### PostgreSQL Database (postgres)

- PostgreSQL 17
- Database geïnitialiseerd met `init.sql`
- Data persistent opgeslagen in Docker volume `postgres_data`
- Health checks geactiveerd

## Commando's

### Services starten

```bash
docker compose up -d
```

### Services stoppen

```bash
docker compose down
```

### Logs bekijken

```bash
# Alle services
docker compose logs -f

# Specifieke service
docker compose logs -f webapp
docker compose logs -f postgres
```

### Opnieuw bouwen na code wijzigingen

```bash
docker compose up --build -d
```

### Alles opschonen (inclusief volumes)

```bash
docker compose down -v
```

## Omgevingsvariabelen

U kunt het volgende aanpassen in `compose.yml`:

- `JWT_SECRET`: Geheime sleutel voor JWT tokens
- `DB_CONNECTION`: PostgreSQL verbindingsstring
- `POSTGRES_USER`: Database gebruikersnaam
- `POSTGRES_PASSWORD`: Database wachtwoord
- `POSTGRES_DB`: Database naam

## Database Verbinding

De applicatie maakt verbinding met PostgreSQL met:

- **Host**: `postgres` (intern Docker netwerk)
- **Poort**: `5432` (intern)
- **Database**: `beijing`
- **Gebruikersnaam**: `john_xina`
- **Wachtwoord**: `1234567`

Externe toegang (vanaf host):

- **Host**: `localhost`
- **Poort**: `3100`

## Probleemoplossing

### Service status controleren

```bash
docker compose ps
```

### Toegang tot webapp container shell

```bash
docker exec -it blog_webapp bash
```

### Toegang tot database

```bash
docker exec -it blog_database psql -U john_xina -d beijing
```

### Webapp gezondheid controleren

```bash
curl http://localhost:8080/
```
