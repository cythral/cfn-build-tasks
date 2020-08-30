# CloudFormation MSBuild Tasks

This is a library that adds MSBuild tasks for packaging, deploying and updating CloudFormation templates from MSBuild without needing to install the entire AWS CLI.  **This library is currently experimental, use at your own risk.**

Currently includes tasks for:

- Packaging and deploying templates
- Updating CodeUri location (global regex find/replace)

Future:

- Update Code/CodeUri location (specify exact path)
- Full support for all packagable resources (currently only some resources are supported)
- Update IAM permissions

## Installation

```shell
dotnet add package Cythral.CloudFormation.BuildTasks
```

## Packages

|                                   |                                                                                              |
| --------------------------------- | -------------------------------------------------------------------------------------------- |
| Cythral.CloudFormation.BuildTasks | ![Nuget](https://img.shields.io/nuget/v/Cythral.CloudFormation.BuildTasks?style=flat-square) |


## License

This project is licensed under the [MIT License](LICENSE.txt)


