# Web #
[![AppVeyor](https://img.shields.io/appveyor/ci/gruntjs/grunt.svg?maxAge=2592000)](https://ci.appveyor.com/project/seesharper/lightinject-web)
[![NuGet](https://img.shields.io/nuget/v/LightInject.Web.svg?maxAge=2592000)]()
[![GitHub tag](https://img.shields.io/github/tag/seesharper/LightInject.Web.svg?maxAge=2592000)]()

Enables **LightInject** to be used in a web application and provides support for **PerWebRequest** scoped service instances.

## Installing ##

**LightInject.Web** provides two distribution models via NuGet

### Binary ###

<div class="nuget-badge" >
   <p>
         <code>PM&gt; Install-Package LightInject.Web </code>
   </p>
</div>

This adds a reference to the LightInject.Web.dll in the target project.

### Source ###

<div class="nuget-badge" >
   <p>
         <code>PM&gt; Install-Package LightInject.Web.Source </code>
   </p>
</div>

This will install a single file (LightInject.Web.cs) into the current project.

The following example shows how to enable **LightInject** in the **Application_Start** event.

	protected void Application_Start()
    {
		var container = new ServiceContainer();
        container.EnablePerWebRequestScope();                   
		container.Register<IFoo, Foo>(new PerScopeLifetime()); 		
	}

A service	 registered with **PerScopeLifetime** is scoped per web request and is disposed at the end of the request if it implements **IDisposable**.