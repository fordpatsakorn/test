## Release 1.0

### Warning Rules

| Rule ID   | Category         | Severity | Notes                                                                                                              |
|-----------|------------------|----------|--------------------------------------------------------------------------------------------------------------------|
| FAWRN0001 | Entity Framework | Warning  | LINQ query must use at least one property with [NotOptional] attribute in the where clause.                        |
| FAWRN0002 | Entity Framework | Warning  | Using DbContext directly is discouraged. Please use other data access methods in IDataHandler instead if possible. |


### Info Rules
| Rule ID   | Category         | Severity | Notes                                    |
|-----------|------------------|----------|------------------------------------------|
| FAINF0001 | Common           | Warning  | Avoid Blocking Task with Wait/Result     |
| FAINF0002 | Entity Framework | Warning  | Avoid directly using raw SQL if possible |


### Error Rules
| Rule ID   | Category         | Severity | Notes                                                                                       |
|-----------|------------------|----------|---------------------------------------------------------------------------------------------|
