# Custom converters
The Elements library is meant to be expanded by users, and so we intend to make it possible for the community to author their own converters as well.  Only Revit converters exist right now, but look out for more.

# Samples
All samples are code that is included with the Hypar Revit Plugin installer.  You don't need to install them, they are to be used as code samples for educational purposes.
The folder "SampleCustomConverters" has converters that are each in separate projects demonstrating the setup for a converter that a user could create and test, refer to these first.
The folder "SampleConverters" shows converters that are more core to the plugin and contain more advanced logic.  They may be re-structured into individual projects in the future.

# Revit
A revit convert is simply a class that implements the IRevitConverter interface from the [Hypar.Elements.Conversion.Revit](https://www.nuget.org/packages/Hypar.Elements.Conversion.Revit/) nuget package.

For a detailed walkthrough of authoring a Revit converter you can refer to [this video](https://youtu.be/si__iV6oJKw).

```C#
public interface IRevitConverter
{
    bool CanConvertToRevit { get; }
    bool CanConvertFromRevit { get; }

    FilteredElementCollector AddElementFilters(FilteredElementCollector collector);
    Element[] FromRevit(Autodesk.Revit.DB.Element revitElement, Document document);
    Element[] OnlyLoadableElements(Element[] allElements);
    ElementId[] ToRevit(Element hyparElement, LoadContext context);
}
```
