# Royal Game Platform V3

This folder is the new .NET 8 + Vue 3 microservice platform. The legacy React/Node/boardgame.io app remains at repository root as a behavior/reference implementation during migration.

## First milestone

```bash
cd platform
docker compose up --build
```

Expected endpoints after M1 foundation is complete:

- Web: http://localhost:8080
- Gateway: http://localhost:8888
- Identity health through gateway: http://localhost:8888/identity/health
- Lobby health through gateway: http://localhost:8888/lobby/health
- Tiến Lên health through gateway: http://localhost:8888/tienlen/health
- RabbitMQ management: http://localhost:15672
- PostgreSQL: localhost:5432

See `../docs/V3_MICROSERVICES_PLAN.md` for the complete migration plan.
