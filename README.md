# FileMaker Data Api

Looking for a .NET wrapper for the new FileMaker Data API? Check out https://github.com/fuzzzerd/FMData. 

# fmDotNet: a wrapper for the FileMaker XML API

This is a fork of [the original fmDotNet](http://fmdotnet.sourceforge.net/). It includes additional coverage of the FileMaker XML API that was not present in the original version. Most notibly support for the `-findquery` operation via the [fmDotNet.Requests.CompoundFind](https://github.com/fuzzzerd/fmDotNet/blob/master/src/fmDotNet/Requests/CompoundFind.cs) class. Our fork also includes a set of unit/integration tests.

If you are familiar with FileMaker Pro 6 CDML Web Publishing or the FileMaker PHP API, fmDotNet will feel familiar to you. Many of the core principles and techniques are the same. The operations and vocabulary are the same as in FileMaker Pro.

The integration tests provided with this library have been tested against the latest FileMaker Server with all tests passing.

## Getting Started with fmDotNet

### fmDotNet is available on NuGet!

To install fmDotNet to your project, from within [Visual Studio](http://www.microsoft.com/visualstudio/eng/products/visual-studio-express-products), run the following command in the [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console)

	PM> Install-Package fmDotNet
	
Nuget Package Site: https://nuget.org/packages/fmDotNet/

| NuGet Downloads | License |
|---|---|
| ![NuGet](https://img.shields.io/nuget/dt/fmDotNet.svg) | [![license](https://img.shields.io/github/license/fuzzzerd/fmDotNet.svg?style=flat-square)](https://github.com/fuzzzerd/fmDotNet/blob/master/LICENSE) |

### Using fmDotNet in Code

You can start querying data from your FileMaker database with just a few lines of code:

	var fms = new fmDotNet.FMSAxml("YourServerName", "user", "passw0rd");
	fms.SetDatabase("yourDatabase");
	fms.SetLayout("yourLayout");
	var request = fms.CreateFindRequest(Enumerations.SearchType.Subset);
	request.AddSearchField("YourFieldName", "value-to-query-for");
	var response = request.Execute();

You can query against related data too:

	var fms = new fmDotNet.FMSAxml("YourServerName", "user", "passw0rd");
	fms.SetDatabase("yourDatabase");
	fms.SetLayout("yourLayout");
	var request = fms.CreateFindRequest(Enumerations.SearchType.Subset);
	request.AddSearchField("RELATEDTALE::RelatedField", "value-to-query-for");
	var response = request.Execute();
	
*Note: the search is on related fields via Table::Field where the related field is on `yourLayout`*.

You can perform complex finds with code like the following:

	var fms = new fmDotNet.FMSAxml("YourServerName", "user", "passw0rd");
	fms.SetDatabase("yourDatabase");
	fms.SetLayout("yourLayout");
	var cpfRequest = fms.CreateCompoundFindRequest();
	cpfRequest.AddSearchCriterion("Colors::Name", "Blue", true, false);
	cpfRequest.AddSearchCriterion("Colors::Name", "Red", true, false);
	var response = cpfRequest.Execute();

This finds all items where the color is Red **OR** Blue.

### Additional fmDotNet Example Usage

Browse the code in the test project fmDotNet.Tests for basic usage of the library. This is the automated test code that is used to ensure that fmDotNet functions correctly. It is a good place to see how specific tasks are completed using fmDotNet. In the future, we would love to have a full sample application showing usage of fmDotNet in the context of a real application.

### Repository Statistics

[![GitHub commit activity the past week, 4 weeks, year](https://img.shields.io/github/commit-activity/y/fuzzzerd/fmDotNet.svg?style=flat-square)](https://github.com/fuzzzerd/fmDotNet/commits/master)

[![GitHub issues](https://img.shields.io/github/issues/fuzzzerd/fmDotNet.svg?style=flat-square)](https://github.com/fuzzzerd/fmDotNet/issues)

[![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/fuzzzerd/fmDotNet.svg?style=flat-square)](https://github.com/fuzzzerd/fmDotNet/commits/master)

[![GitHub language count](https://img.shields.io/github/languages/count/fuzzzerd/fmDotNet.svg?style=flat-square)](https://github.com/fuzzzerd/fmDotNet/commits/master)

### Key Differences 

There are several differences between this fork of fmDotNet and the original. This version

 1. Uses HTTP POST for all requests to FileMaker Server
 2. Has method, property, and field names in line with [MSDN Naming Guidelines](http://msdn.microsoft.com/en-us/library/vstudio/ms229002.aspx)
 3. Does not include support for ADODB RecordSets
 4. Targets .NET Standard 2.0

### Contributing to fmDotNet

 1. Submit an issue/bug with steps to reproduce it.
 2. Create a wiki page with more detailed examples, tutorials, or documentation.
 
Please make sure that that changes you submit to the core fmDotNet project pass the tests in fmDotNet.Tests or explain why they don't and why the test should be updated. These are integration tests, so you'll need to run the included FileMaker database on a copy of FileMaker Server.

### FileMaker Documentation

Documentation for the FileMaker APIs that fmDotNet covers are linked below.

 - [FileMaker Server 16 Web Publishing Guide](https://fmhelp.filemaker.com/docs/16/en/fms16_cwp_guide.pdf)
 - [FileMaker Server 15 Web Publishing Guide](https://fmhelp.filemaker.com/docs/15/en/fms15_cwp_guide.pdf)
 - [FileMaker Server 14 Web Publishing Guide](https://fmhelp.filemaker.com/docs/14/en/fms14_cwp_guide.pdf)
  - [FileMaker Server 13 Web Publishing with XML](https://fmhelp.filemaker.com/docs/13/en/fms13_cwp_xml.pdf)
 - [FileMaker Server 12 Web Publishing with XML](http://www.filemaker.com/support/product/docs/12/fms/fms12_cwp_xml_en.pdf)
 - [FileMaker Server 11 Web Publishing with XSLT and XML](http://www.filemaker.com/support/product/docs/fms/fms11_cwp_xslt_en.pdf)

### Versioning

We use [Semantic Versioning](http://semver.org/). Using the Major.Minor.Patch syntax, we attempt to follow the basic rules

 1. MAJOR version when you make incompatible API changes,
 2. MINOR version when you add functionality in a backwards-compatible manner, and
 3. PATCH version when you make backwards-compatible bug fixes.
