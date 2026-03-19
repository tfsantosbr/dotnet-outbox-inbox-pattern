Create EF Core migrations for one or more microservices in this project.

Arguments: $ARGUMENTS

The argument is a comma-separated list of microservice names. Supported values: `orders`, `notification`, `inventory`.

Use this mapping to resolve each microservice to its project path and DbContext:

| Microservice  | Project path                                      | DbContext               |
|---------------|---------------------------------------------------|-------------------------|
| orders        | src/Orders/Orders.API                             | OrdersDbContext         |
| notification  | src/Notification/Notification.Consumer            | NotificationDbContext   |
| inventory     | src/Inventory/Inventory.Consumer                  | InventoryDbContext       |

Steps:
1. Parse the argument splitting by comma and trimming whitespace from each entry.
2. Ask the user for the migration name if they didn't provide one after a colon (e.g. `orders:AddOrderTable`). If no name is given, use `InitialCreate` as default.
3. For each microservice in the list, run the following Bash command from the repository root:

```
dotnet ef migrations add <MigrationName> --project <ProjectPath> --output-dir Infrastructure/Migrations
```

4. Report the result of each command to the user.

If an unrecognized microservice name is provided, inform the user and skip it.
