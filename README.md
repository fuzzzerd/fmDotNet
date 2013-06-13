##fmDotNet: a wrapper for the FileMaker Server XML API

This is a fork of [the original fmDotNet](http://fmdotnet.sourceforge.net/). It includes additional coverage of the FileMaker XML API that was not present in the original version. Most notibly support for the `-findquery` operation via the [fmDotNet.Requests.CompoundFind](https://github.com/WizardSoftware/fmDotNet/blob/master/src/fmDotNet/Requests/CompoundFind.cs) class. Our fork also includes a set of unit/integration tests.

If you are familiar with FM6 web publishing or FileMaker's PHP API, fmDotNet will feel familiar to you as the core principles are very similar. 

### Getting Started with fmDotNet

You can start querying data from your FileMaker database with just a few lines of code:

    var fms = new fmDotNet.FMSAxml("YourServerName", "user", "passw0rd");
    fms.SetDatabase("yourDatabase");
    fms.SetLayout("yourLayout");
    var request = fms.CreateFindRequest(Enumerations.SearchType.Subset);
    request.AddSearchField("YourFieldName", "value-to-query-for");
    var response = request.Execute();

You can query on related data too:

    var fms = new fmDotNet.FMSAxml("YourServerName", "user", "passw0rd");
    fms.SetDatabase("yourDatabase");
    fms.SetLayout("yourLayout");
    var request = fms.CreateFindRequest(Enumerations.SearchType.Subset);
    request.AddSearchField("RELATEDTALE::RelatedField", "value-to-query-for");
    var response = request.Execute();

You can edit also perform complex finds with code like the following:

    var fms = new fmDotNet.FMSAxml("YourServerName", "user", "passw0rd");
    fms.SetDatabase("yourDatabase");
    fms.SetLayout("yourLayout");
    var cpfRequest = fms.CreateCompoundFindRequest();
    cpfRequest.AddSearchCriterion("Colors::Name", "Blue", true, false);
    cpfRequest.AddSearchCriterion("Colors::Name", "Red", true, false);
    var response = cpfRequest.Execute();

This finds all items where the color is Red **OR** Blue. Note the search on related fields via *Table::Field*.

### Additional fmDotNet Example Usage

Browse the code in the [test project fmDotNet.Tests](https://github.com/WizardSoftware/fmDotNet/tree/master/src/fmDotNet.Tests) for basic usage of the library. This is the code that is used to ensure that fmDotNet functions correctly. It is a good place to see how specific tasks are completed using fmDotNet. In the future, we would love to have a full sample application showing specific tasks in context.

### Contributing to fmDotNet

 1. Fork us, hit hte Fork button at the top right! Make improvements, add additional tests, and submit a pull request. 
 2. Submit an issue/bug with steps to reproduce it.
 3. Submit a wiki page with more detailed examples.
 
Please make sure that any changes submitted to the core fmDotNet project pass the tests in fmDotNet.Tests.

### FileMaker Documentation

Documentation for the FileMaker APIs that fmDotNet covers.

 1. [FileMaker Server 12 Web Publishing with XML](http://www.filemaker.com/support/product/docs/12/fms/fms12_cwp_xml_en.pdf)
 2. [FileMaker Server 11 Web Publishing with XSLT and XML](http://www.filemaker.com/support/product/docs/fms/fms11_cwp_xslt_en.pdf)

### License

[Common Public License Version 1.0](http://opensource.org/licenses/cpl1.0.txt)