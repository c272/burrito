<div align="center">
<img src="https://raw.githubusercontent.com/c272/burrito/master/logo.png"/>
<img src="https://img.shields.io/github/issues/c272/burrito"> <img src="https://img.shields.io/travis/c272/burrito"> <img src="https://img.shields.io/badge/%2ENET->=4.7.1-blue">
</div>
<br>

A simple, easy to use command line API  wrapper generator, that will bundle up your favourite JSON APIs and  create a C# project out of it, and will **automagically** generate classes for the data that is sent back from the API. The most basic command usage for Burrito is including a single source file, like below:

```bash
burrito -s schema.json
```
Burrito also supports asynchronous methods using `Task`, and can include `POST`, `GET` and other HTTP methods, with custom URL parameters that are automatically added to API methods.

## Getting Started
To get started using Burrito on Windows, all you'll need is .NET Framework 4.7.1 or later, and you can download one of the prebuilt binaries from the [Releases tab](https://github.com/c272/burrito/releases) on the main repository page.

If you're using Linux, however, you'll have to build the project using Mono and `mkbundle`. Make sure you have the following dependencies installed as a nuget packages before attempting to build with `mkbundle`:

 - `Newtonsoft.Json 12.0.0.0` (**version specific**)
 - `Microsoft.CodeAnalysis.CSharp`
 - `ILRepack`

Once this is done, you can use a simple `mkbundle` command such as the example below to create a native executable for your distro:
```bash
mkbundle -o burrito --simple bin/Debug/burrito.exe --machine-config /etc/mono/4.5/machine.config --no-config --nodeps bin/Debug/*.dll
```

## Usage
### Basics
To create an API wrapper with Burrito, you first need to write an **API schema**. These are extremely simple representations of the routes that you're trying to add to the wrapper. A barebones simple example is below:
```json
{
	"name": "ExampleAPI",
	"root": "https://www.example.com/",
	"sections": [
		{
			"name": "API",
			"routes": [
				{
					"route": "test/",
					"type": "GET",
					"returns": "TestData"
				}
			]
		}
	]
}
```
There are many parts about each route that you can customize, which are defined in the [API Schema wiki page,]() as well as the manual.

Once you've created your API schema, you can simply feed it into Burrito and it will generate an API wrapper project.

```bash
burrito -s example.json
```
There are many console flags that you can apply for different outputs of project, such as only creating a `.dll` and no project files, or generating both asynchronous and synchronous methods. Those are shown on the [Command Line Arguments wiki page]().

### Examples
As an example of how to set up and use a Burrito API, see the "[DnD5e-cs](https://github.com/c272/dnd5e-cs)" project, set up as an example of how an API wrapper can be implemented for NuGet.
