# cs_rider_pages_final_assignment

Dit is een volledig functionele blog-applicatie gebouwd met ASP.NET Core Razor Pages, PostgreSQL en Docker.

## Snel Starten met Docker

```bash
# Start de applicatie
./start.sh

# Of handmatig
docker compose up --build -d
```

Toegang tot de applicatie via **http://localhost:8080**

Voor gedetailleerde Docker instructies, zie [DOCKER.md](DOCKER.md)

## Vereisten

- [x] In-memory key-value store met vervaldatum
- [x] Authenticatie - login/logout alleen voor admins
  - [x] Auth token in cookie
- [x] Blog posts
  - [x] Gebruikersrol kan posts lezen en reacties toevoegen, maar deze zijn nog niet zichtbaar voor andere gebruikers
  - [x] Admin kan posts en concepten aanmaken
  - [x] Admin kan posts bijwerken
  - [x] Admin kan posts verwijderen
  - [x] Admin moet gebruikersreacties goedkeuren om zichtbaar te zijn onder een post
  - [x] Post moet tags, titel, body en afbeelding hebben
  - [x] Post kan meerdere versies hebben: een gepubliceerde en meerdere concepten
    - [x] Posts scheduler
    - [x] Admin kan instellen wanneer een post gepubliceerd/zichtbaar wordt voor gebruikers
    - [x] Gebruikers kunnen posts filteren op tags (client-side filtering)
- [x] Volledige tekstzoekopdracht in posts (uitgevoerd in PostgreSQL)
- [x] Navigeren door posts met slugs /posts/:id
- [x] Frontend met Razor Pages
  - [x] Minimaal donker thema design
  - [x] Homepagina met client-side zoeken en tag filtering
  - [x] Individuele post pagina's met reacties
  - [x] Admin login pagina
  - [x] Admin dashboard voor het beheren van posts
  - [x] Admin pagina voor het maken/bewerken van posts
  - [x] Admin pagina voor het modereren van reacties
  - [x] sitemap.xml generatie
