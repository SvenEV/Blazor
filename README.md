# Xamzor
### _E**x**tensible **A**pplication **M**arkup for Bla**zor**_

Xamzor is an experimental project in which I try to develop a set of reusable Blazor components that are familiar to XAML developers.

### Goals
* Provide WPF-style layout primitives including `Grid`, `StackPanel` and `Border`, supporting properties like  `Width`, `Height`, `HorizontalAlignment`, `VerticalAlignment`, `Margin` and more

* Render to HTML, but don't rely on CSS layout like grid or flexbox. Instead, layout calculations are done in C# and mapped to simple absolute positioning in CSS. Added benefit: Consistency across browsers!

* Be a library, not a framework

* Ideally Xamzor should allow mixing-and-matching Xamzor components and "normal" HTML. Layouting may get tricky, though.

### Non-Goals
* Be 100% syntax-compatible to XAML
* Replicate WPF features for scenarios that can be solved more elegantly in Razor (e.g. we don't need `{Binding}` or `ICommand`)

# About Blazor
Blazor is "an experimental web UI framework using C#/Razor and HTML, running in the browser via WebAssembly". It is an experimental project from the ASP.NET team at Microsoft.

[**Read the official README at the source repository**](https://github.com/aspnet/Blazor)
