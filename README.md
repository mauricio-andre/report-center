# Generate migrations

Criar migrations para o contexto de administração

```bash
dotnet ef migrations add Initials -p ./src/Providers/ReportCenter.Postgres -s ./src/Apps/ReportCenter.App.DbMigrator -o Migrations/AdministrationDbContextMigrations -c PostgresAdministrationDbContext
```

Criar migrations para o contexto do Core

```bash
dotnet ef migrations add Initials -p ./src/Providers/ReportCenter.Postgres -s ./src/Apps/ReportCenter.App.DbMigrator -o Migrations/CoreDbContextMigrations -c CoreDbContext
```

Executar as migrations

Execute o projeto DbMigrator ou use os comandos abaixo

```bash
dotnet ef database update -p ./src/Apps/ReportCenter.App.DbMigrator -c AdministrationDbContext

dotnet ef database update -p ./src/Apps/ReportCenter.App.DbMigrator -c CoreDbContext
```

TODO: Criar parâmetro para mudar estratégia de leitura de sharedString, entre atual e uso com dictionary
TODO: Criar parâmetro para permitir criar a pasta de arquivos temporários em lugar alternativo ao caminho de arquivos temporários
