##fmDotNet for FileMaker Server XML

This is a fork of [the original fmDotNet](http://fmdotnet.sourceforge.net/). It includes additional coverage of the FileMaker XML API that was not present in the original version. Most notibly support for the `-findquery` operation via the [fmDotNet.Requests.CompoundFind](https://github.com/WizardSoftware/fmDotNet/blob/master/src/fmDotNet/Requests/CompoundFind.cs) class.

If you are familiar with FM6 web publishing or FileMaker's PHP API, fmDotNet will feel familiar to you as the core principles are very similar. 

###FileMaker Documentation
 1. [FileMaker Server 12 Web Publishing with XML](http://www.filemaker.com/support/product/docs/12/fms/fms12_cwp_xml_en.pdf)
 2. [FileMaker Server 11 Web Publishing with XML](http://www.filemaker.com/support/product/docs/fms/fms11_cwp_xslt_en.pdf)

### Getting Started

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

This finds all items where the color is Red **OR** Blue.

### Additional Usage Examples
For additional usage examples, browse the code in fmDotNet.Tests. This is the code that is used to ensure that fmDotNet functions correctly and is a good place to see how specific tasks are completed using fmDotNet.

### License
[Common Public License Version 1.0](http://opensource.org/licenses/cpl1.0.txt)