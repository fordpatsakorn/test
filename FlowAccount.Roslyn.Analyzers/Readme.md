# FlowAccount Roslyn Analyzer

## Content

### FlowAccount.Roslyn.Analyzers

A .NET project with implementations of analyzers and code fix providers.
**You must reference this project as an analyzer to see the results (warnings) in the IDE.**
```xml
<ItemGroup>
  <ProjectReference Include="..\FlowAccount.Roslyn.Analyzers\FlowAccount.Roslyn.Analyzers\FlowAccount.Roslyn.Analyzers.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>
</ItemGroup>
```

### FlowAccount.Roslyn.Analyzers.Tests

Unit tests for the analyzers and code fix provider. The easiest way to develop language-related features is to start with unit tests.

## How To?

### How can I determine which syntax nodes I should expect?

Consider using the Roslyn Visualizer toolwindow, witch allow you to observe syntax tree.

### Learn more about wiring analyzers

The complete set of information is available at [roslyn github repo wiki](https://github.com/dotnet/roslyn/blob/main/docs/wiki/README.md).

## More info
https://flowaccount.atlassian.net/wiki/spaces/fa/pages/142377106/Static+Code+Analysis
