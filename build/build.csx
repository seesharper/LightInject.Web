
#load "common.csx"

private const string projectName = "LightInject.Web";

private const string portableClassLibraryProjectTypeGuid = "{786C830F-07A1-408B-BD7F-6EE04809D6DB}";
private const string csharpProjectTypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

string pathToSourceFile = @"..\src\LightInject.Web\LightInject.Web.cs";
string pathToBuildDirectory = @"tmp/";
private string version = GetVersionNumberFromSourceFile(pathToSourceFile);

private string fileVersion = Regex.Match(version, @"(^[\d\.]+)-?").Groups[1].Captures[0].Value;

WriteLine("LightInject.Web version {0}" , version);

Execute(() => InitializBuildDirectories(), "Preparing build directories");
Execute(() => RenameSolutionFiles(), "Renaming solution files");
Execute(() => PatchAssemblyInfo(), "Patching assembly information");
Execute(() => PatchProjectFiles(), "Patching project files");
Execute(() => PatchPackagesConfig(), "Patching packages config");
Execute(() => InternalizeSourceVersions(), "Internalizing source versions");
Execute(() => RestoreNuGetPackages(), "NuGet");
Execute(() => BuildAllFrameworks(), "Building all frameworks");
Execute(() => RunAllUnitTests(), "Running unit tests");
Execute(() => AnalyzeTestCoverage(), "Analyzing test coverage");
Execute(() => CreateNugetPackages(), "Creating NuGet packages");

private void CreateNugetPackages()
{
	string pathToNuGetBuildDirectory = Path.Combine(pathToBuildDirectory, "NuGetPackages");
	DirectoryUtils.Delete(pathToNuGetBuildDirectory);
	
		
	Execute(() => CopySourceFilesToNuGetLibDirectory(), "Copy source files to NuGet lib directory");		
	Execute(() => CopyBinaryFilesToNuGetLibDirectory(), "Copy binary files to NuGet lib directory");
	
	Execute(() => CreateSourcePackage(), "Creating source package");		
	Execute(() => CreateBinaryPackage(), "Creating binary package");
    string myDocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    RoboCopy(pathToBuildDirectory, myDocumentsFolder, "*.nupkg");		
}

private void CopySourceFilesToNuGetLibDirectory()
{	
	CopySourceFile("NET45", "net45");
	CopySourceFile("NET46", "net46");				
}

private void CopyBinaryFilesToNuGetLibDirectory()
{
	CopyBinaryFile("NET45", "net45");
	CopyBinaryFile("NET46", "net46");				
}

private void CreateSourcePackage()
{	    
	string pathToMetadataFile = Path.Combine(pathToBuildDirectory, "NugetPackages/Source/package/LightInject.Web.Source.nuspec");	
    PatchNugetVersionInfo(pathToMetadataFile, version);		    
	NuGet.CreatePackage(pathToMetadataFile, pathToBuildDirectory);   		
}

private void CreateBinaryPackage()
{	    
	string pathToMetadataFile = Path.Combine(pathToBuildDirectory, "NugetPackages/Binary/package/LightInject.Web.nuspec");
	PatchNugetVersionInfo(pathToMetadataFile, version);
	NuGet.CreatePackage(pathToMetadataFile, pathToBuildDirectory);
}

private void CopySourceFile(string frameworkMoniker, string packageDirectoryName)
{
	string pathToMetadata = "../src/LightInject.Web/NuGet";
	string pathToPackageDirectory = Path.Combine(pathToBuildDirectory, "NugetPackages/Source/package");	
	RoboCopy(pathToMetadata, pathToPackageDirectory, "LightInject.Web.Source.nuspec");	
	string pathToSourceFile = "tmp/" + frameworkMoniker + "/Source/LightInject.Web";
	string pathToDestination = Path.Combine(pathToPackageDirectory, "content/" + packageDirectoryName + "/LightInject.Web");
	RoboCopy(pathToSourceFile, pathToDestination, "LightInject.Web.cs");
	FileUtils.Rename(Path.Combine(pathToDestination, "LightInject.Web.cs"), "LightInject.Web.cs.pp");
	ReplaceInFile(@"namespace \S*", "namespace $rootnamespace$.LightInject.Web", Path.Combine(pathToDestination, "LightInject.Web.cs.pp"));
}

private void CopyBinaryFile(string frameworkMoniker, string packageDirectoryName)
{
	string pathToMetadata = "../src/LightInject.Web/NuGet";
	string pathToPackageDirectory = Path.Combine(pathToBuildDirectory, "NugetPackages/Binary/package");
	RoboCopy(pathToMetadata, pathToPackageDirectory, "LightInject.Web.nuspec");
	string pathToBinaryFile =  ResolvePathToBinaryFile(frameworkMoniker);
	string pathToDestination = Path.Combine(pathToPackageDirectory, "lib/" + packageDirectoryName);
	RoboCopy(pathToBinaryFile, pathToDestination, "LightInject.Web.*");
}

private string ResolvePathToBinaryFile(string frameworkMoniker)
{
	var pathToBinaryFile = Directory.GetFiles("tmp/" + frameworkMoniker + "/Binary/LightInject.Web/bin/Release","LightInject.Web.dll", SearchOption.AllDirectories).First();
	return Path.GetDirectoryName(pathToBinaryFile);		
}

private void BuildAllFrameworks()
{	
	Build("Net45");
	Build("Net46");			
}

private void Build(string frameworkMoniker)
{
	var pathToSolutionFile = Directory.GetFiles(Path.Combine(pathToBuildDirectory, frameworkMoniker + @"\Binary\"),"*.sln").First();	
	WriteLine(pathToSolutionFile);
	MsBuild.Build(pathToSolutionFile);
	pathToSolutionFile = Directory.GetFiles(Path.Combine(pathToBuildDirectory, frameworkMoniker + @"\Source\"),"*.sln").First();
	MsBuild.Build(pathToSolutionFile);
}

private void RestoreNuGetPackages()
{	
	RestoreNuGetPackages("net45");
	RestoreNuGetPackages("net46");			
}

private void RestoreNuGetPackages(string frameworkMoniker)
{
	string pathToProjectDirectory = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Web");
	NuGet.Restore(pathToProjectDirectory);
	pathToProjectDirectory = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Web.Tests");
	NuGet.Restore(pathToProjectDirectory);
	pathToProjectDirectory = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Source/LightInject.Web");
	NuGet.Restore(pathToProjectDirectory);
	pathToProjectDirectory = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Source/LightInject.Web.Tests");
	NuGet.Restore(pathToProjectDirectory);
    NuGet.Update(GetFile(Path.Combine(pathToBuildDirectory, frameworkMoniker, "Binary"), "*.sln"));        
}

private void RunAllUnitTests()
{	
	DirectoryUtils.Delete("TestResults");
	Execute(() => RunUnitTests("Net45"), "Running unit tests for Net45");
	Execute(() => RunUnitTests("Net46"), "Running unit tests for Net46");
		
}

private void RunUnitTests(string frameworkMoniker)
{
	string pathToTestAssembly = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Web.Tests/bin/Release/LightInject.Web.Tests.dll");
	string testAdapterSearchDirectory = Path.Combine(pathToBuildDirectory, frameworkMoniker, @"Binary/packages/");
    string pathToTestAdapterDirectory = ResolveDirectory(testAdapterSearchDirectory, "xunit.runner.visualstudio.testadapter.dll");
	MsTest.Run(pathToTestAssembly, pathToTestAdapterDirectory);	
}

private void AnalyzeTestCoverage()
{	
	Execute(() => AnalyzeTestCoverage("NET45"), "Analyzing test coverage for NET45");
	Execute(() => AnalyzeTestCoverage("NET46"), "Analyzing test coverage for NET46");
}

private void AnalyzeTestCoverage(string frameworkMoniker)
{	
    string pathToTestAssembly = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Web.Tests/bin/Release/LightInject.Web.Tests.dll");
	string pathToPackagesFolder = Path.Combine(pathToBuildDirectory, frameworkMoniker, @"Binary/packages/");
    string pathToTestAdapterDirectory = ResolveDirectory(pathToPackagesFolder, "xunit.runner.visualstudio.testadapter.dll");		
    MsTest.RunWithCodeCoverage(pathToTestAssembly, pathToPackagesFolder,pathToTestAdapterDirectory, "LightInject.Web.dll");
}

private void InitializBuildDirectories()
{
	DirectoryUtils.Delete(pathToBuildDirectory);	
	Execute(() => InitializeNugetBuildDirectory("NET45"), "Preparing Net45");
	Execute(() => InitializeNugetBuildDirectory("NET46"), "Preparing Net46");							
}

private void InitializeNugetBuildDirectory(string frameworkMoniker)
{
	var pathToBinary = Path.Combine(pathToBuildDirectory, frameworkMoniker +  "/Binary");		
    CreateDirectory(pathToBinary);
	RoboCopy("../src", pathToBinary, "/e /XD bin obj .vs NuGet TestResults packages");	
				
	var pathToSource = Path.Combine(pathToBuildDirectory,  frameworkMoniker +  "/Source");	
	CreateDirectory(pathToSource);
	RoboCopy("../src", pathToSource, "/e /XD bin obj .vs NuGet TestResults packages");
	
	if (frameworkMoniker.StartsWith("DNX"))
	{
		var pathToJsonTemplateFile = Path.Combine(pathToBinary, "LightInject.Web/project.json_");
		var pathToJsonFile = Path.Combine(pathToBinary, "LightInject.Web/project.json");
		File.Move(pathToJsonTemplateFile, pathToJsonFile);
		pathToJsonTemplateFile = Path.Combine(pathToSource, "LightInject.Web/project.json_");
		pathToJsonFile = Path.Combine(pathToSource, "LightInject.Web/project.json");
		File.Move(pathToJsonTemplateFile, pathToJsonFile);
	}				  
}

private void RenameSolutionFile(string frameworkMoniker)
{
	string pathToSolutionFolder = Path.Combine(pathToBuildDirectory, frameworkMoniker +  "/Binary");
	string pathToSolutionFile = Directory.GetFiles(pathToSolutionFolder, "*.sln").First();
	string newPathToSolutionFile = Regex.Replace(pathToSolutionFile, @"(\w+)(.sln)", "$1_" + frameworkMoniker + "_Binary$2");
	File.Move(pathToSolutionFile, newPathToSolutionFile);
	WriteLine("{0} (Binary) solution file renamed to : {1}", frameworkMoniker, newPathToSolutionFile);
	
	pathToSolutionFolder = Path.Combine(pathToBuildDirectory, frameworkMoniker +  "/Source");
	pathToSolutionFile = Directory.GetFiles(pathToSolutionFolder, "*.sln").First();
	newPathToSolutionFile = Regex.Replace(pathToSolutionFile, @"(\w+)(.sln)", "$1_" + frameworkMoniker + "_Source$2");
	File.Move(pathToSolutionFile, newPathToSolutionFile);
	WriteLine("{0} (Source) solution file renamed to : {1}", frameworkMoniker, newPathToSolutionFile);
}

private void RenameSolutionFiles()
{	
	RenameSolutionFile("NET45");
	RenameSolutionFile("NET46");		
}

private void Internalize(string frameworkMoniker)
{
	string[] exceptTheseTypes = new string[] {
		"IProxy",
		"IInvocationInfo", 
		"IMethodBuilder", 
		"IDynamicMethodSkeleton", 
		"IProxyBuilder", 
		"IInterceptor", 
		"MethodInterceptorFactory",
		"TargetMethodInfo",
		"OpenGenericTargetMethodInfo",
		"DynamicMethodBuilder",
		"CachedMethodBuilder",
		"TargetInvocationInfo",
		"InterceptorInvocationInfo",
		"CompositeInterceptor",
		"InterceptorInfo",
		"ProxyDefinition",
		"LightInjectHttpModuleInitializer"
		}; 
	
	string pathToSourceFile = Path.Combine(pathToBuildDirectory, frameworkMoniker + "/Source/LightInject.Web/LightInject.Web.cs");
	Internalizer.Internalize(pathToSourceFile, frameworkMoniker, exceptTheseTypes);
}

private void InternalizeSourceVersions()
{
	Execute (()=> Internalize("NET45"), "Internalizing NET45");
	Execute (()=> Internalize("NET46"), "Internalizing NET46");	
}

private void PatchPackagesConfig()
{
	PatchPackagesConfig("net45");
	PatchPackagesConfig("net45");	
}

private void PatchPackagesConfig(string frameworkMoniker)
{
	string pathToPackagesConfig = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Web/packages.config");
	ReplaceInFile(@"(targetFramework=\"").*(\"".*)", "$1"+ frameworkMoniker + "$2", pathToPackagesConfig);
	
	pathToPackagesConfig = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Source/LightInject.Web/packages.config");
	ReplaceInFile(@"(targetFramework=\"").*(\"".*)", "$1"+ frameworkMoniker + "$2", pathToPackagesConfig);
}

private void PatchAssemblyInfo()
{
	Execute(() => PatchAssemblyInfo("Net45"), "Patching AssemblyInfo (Net45)");
	Execute(() => PatchAssemblyInfo("Net46"), "Patching AssemblyInfo (Net46)");			
}

private void PatchAssemblyInfo(string framework)
{	
	var pathToAssemblyInfo = Path.Combine(pathToBuildDirectory, framework + @"\Binary\LightInject.Web\Properties\AssemblyInfo.cs");	
	PatchAssemblyVersionInfo(version, fileVersion, framework, pathToAssemblyInfo);
	pathToAssemblyInfo = Path.Combine(pathToBuildDirectory, framework + @"\Source\LightInject.Web\Properties\AssemblyInfo.cs");
	PatchAssemblyVersionInfo(version, fileVersion, framework, pathToAssemblyInfo);	
	PatchInternalsVisibleToAttribute(pathToAssemblyInfo);		
}

private void PatchInternalsVisibleToAttribute(string pathToAssemblyInfo)
{
	var assemblyInfo = ReadFile(pathToAssemblyInfo);   
	StringBuilder sb = new StringBuilder(assemblyInfo);
	sb.AppendLine(@"[assembly: InternalsVisibleTo(""LightInject.Web.Tests"")]");
	WriteFile(pathToAssemblyInfo, sb.ToString());
}

private void PatchProjectFiles()
{
	Execute(() => PatchProjectFile("NET45", "4.5"), "Patching project file (NET45)");
	Execute(() => PatchProjectFile("NET46", "4.6"), "Patching project file (NET46)");		
}

private void PatchProjectFile(string frameworkMoniker, string frameworkVersion)
{
	var pathToProjectFile = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Binary/LightInject.Web/LightInject.Web.csproj");
	PatchProjectFile(frameworkMoniker, frameworkVersion, pathToProjectFile);
	pathToProjectFile = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"/Source/LightInject.Web/LightInject.Web.csproj");
	PatchProjectFile(frameworkMoniker, frameworkVersion, pathToProjectFile);
	PatchTestProjectFile(frameworkMoniker);
}
 
private void PatchProjectFile(string frameworkMoniker, string frameworkVersion, string pathToProjectFile)
{
	WriteLine("Patching {0} ", pathToProjectFile);	
	SetProjectFrameworkMoniker(frameworkMoniker, pathToProjectFile);
	SetProjectFrameworkVersion(frameworkVersion, pathToProjectFile);		
	SetHintPath(frameworkMoniker, pathToProjectFile);	
}

private void SetProjectFrameworkVersion(string frameworkVersion, string pathToProjectFile)
{
	XDocument xmlDocument = XDocument.Load(pathToProjectFile);
	var frameworkVersionElement = xmlDocument.Descendants().SingleOrDefault(p => p.Name.LocalName == "TargetFrameworkVersion");
	frameworkVersionElement.Value = "v" + frameworkVersion;
	xmlDocument.Save(pathToProjectFile);
}

private void SetProjectFrameworkMoniker(string frameworkMoniker, string pathToProjectFile)
{
	XDocument xmlDocument = XDocument.Load(pathToProjectFile);
	var defineConstantsElements = xmlDocument.Descendants().Where(p => p.Name.LocalName == "DefineConstants");
	foreach (var defineConstantsElement in defineConstantsElements)
	{
		defineConstantsElement.Value = defineConstantsElement.Value.Replace("NET46", frameworkMoniker); 
	}	
	xmlDocument.Save(pathToProjectFile);
}

private void SetHintPath(string frameworkMoniker, string pathToProjectFile)
{
	if (frameworkMoniker == "PCL_111")
	{
		frameworkMoniker = "portable-net45+win81+wpa81+MonoAndroid10+MonoTouch10+Xamarin.iOS10";
	}
	ReplaceInFile(@"(.*\\packages\\LightInject.*\\lib\\).*(\\.*)","$1"+ frameworkMoniker + "$2", pathToProjectFile);	
}

private void SetTargetFrameworkProfile()
{
	
	var pathToProjectFile = Path.Combine(pathToBuildDirectory, @"PCL_111/Binary/LightInject.Web/LightInject.Web.csproj");
	XDocument xmlDocument = XDocument.Load(pathToProjectFile);	
	var frameworkVersionElement = xmlDocument.Descendants().SingleOrDefault(p => p.Name.LocalName == "TargetFrameworkProfile");
	if (frameworkVersionElement == null)
	{
		throw new Exception(pathToProjectFile);
	}
	
	frameworkVersionElement.Value = "Profile111";
	XElement projectTypeGuidsElement = new XElement(frameworkVersionElement.Name.Namespace +  "ProjectTypeGuids");
	projectTypeGuidsElement.Value = portableClassLibraryProjectTypeGuid + ";" + csharpProjectTypeGuid;			
	frameworkVersionElement.AddAfterSelf(projectTypeGuidsElement);
	
	var importElement = xmlDocument.Descendants().SingleOrDefault(p => p.Name.LocalName == "Import");
	importElement.Attributes().First().Value = @"$(MSBuildExtensionsPath32)\Microsoft\Portable\$(TargetFrameworkVersion)\Microsoft.Portable.CSharp.targets";	
	xmlDocument.Save(pathToProjectFile);
}

private void PatchTestProjectFile(string frameworkMoniker)
{
	var pathToProjectFile = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"\Binary\LightInject.Web.Tests\LightInject.Web.Tests.csproj");
	WriteLine("Patching {0} ", pathToProjectFile);	
	SetProjectFrameworkMoniker(frameworkMoniker, pathToProjectFile);
	pathToProjectFile = Path.Combine(pathToBuildDirectory, frameworkMoniker + @"\Source\LightInject.Web.Tests\LightInject.Web.Tests.csproj");
	WriteLine("Patching {0} ", pathToProjectFile);
	SetProjectFrameworkMoniker(frameworkMoniker, pathToProjectFile);
}