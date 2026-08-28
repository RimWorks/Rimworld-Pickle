# Getting started

This guide adds a Pickle test suite to an existing mod.

## Layout

Pickle looks for a `Pickle/` folder inside your mod.

```
MyMod/
  About/
  Assemblies/
    MyMod.dll            ships to players, knows nothing about Pickle
  Pickle/
    Assemblies/
      MyMod.Steps.dll    only Pickle loads this
    Features/
      drafting.feature
    Fixtures/
      test-colony.rws
```

Your mod assembly stays clean. Nothing in `Assemblies/` references Pickle, so the mod
runs normally for players who never install Pickle.

## The steps project

Add one small csproj beside your mod's project. It has the same shape as an xunit test
project.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <OutputPath>../../Pickle/Assemblies/</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CryptikLemur.Pickle.Ref" Version="1.*"
                      ExcludeAssets="runtime" PrivateAssets="all" />
    <ProjectReference Include="../MyMod/MyMod.csproj" />
  </ItemGroup>
</Project>
```

Set `ExcludeAssets="runtime"`. The package ships a compile-time stub with the same
assembly identity as the real `Pickle.dll`. The Pickle mod loads the real one at run
time. A copy of the stub in your output would shadow it.

`MyMod.dll` resolves the same way. RimWorld loads it before Pickle loads your steps
assembly, so the reference binds by name.

## Your first scenario

Write a [Gherkin](https://cucumber.io/docs/gherkin/reference/) feature file in
`Pickle/Features/`.

```gherkin
Feature: drafting
  Scenario: a drafted colonist waits for combat
    Given the save "test-colony" is loaded
    Given a colonist "Soldier" exists
    When I draft "Soldier"
    And I wait 30 ticks
    Then "Soldier" is drafted
```

Those steps ship with Pickle. See [built-in steps](steps.md) for the rest, and the
[authoring guide](authoring.md) to write your own.

## Version binding

Pickle skips a steps assembly built against a newer major version of the reference
package. The report says which assembly it skipped and why. Match the major version of
the Pickle mod you run.
