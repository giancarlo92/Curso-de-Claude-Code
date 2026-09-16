#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:property LangVersion=14.0

Console.WriteLine("Este script compila y ahora provocará un error de ejecución.");

var values = new[] { "primer elemento" };
Console.WriteLine(values[0]);
