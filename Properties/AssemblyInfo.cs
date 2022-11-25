using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Lightbucket")]
[assembly: AssemblyDescription("Send session information to the Lightbucket API")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Lightbucket")]
[assembly: AssemblyProduct("Lightbucket.NINAPlugin")]
[assembly: AssemblyCopyright("Copyright ©  2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyMetadata("Homepage", "https://app.lightbucket.co")]

// The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.0")]

// The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
// The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
// The repository where your plugin is hosted
[assembly: AssemblyMetadata("Repository", "https://github.com/lightbucket-co/lightbucket-nina-plugin")]

[assembly: AssemblyMetadata("FeaturedImageURL", "https://app.lightbucket.co/nina_plugin/lightbucket-logo.png")]


[assembly: AssemblyMetadata("LongDescription", @"Lightbucket is a tool for tracking your astrophotography imaging sessions so you can easily see data about your journey through the cosmos.
After installing this plugin and configuring your API credentials, NINA will send information about your LIGHT frames as they are saved over
the course of a sequence.

The following information will be sent to Lightbucket when images are saved:

- Target Name, RA, Dec, and Rotation
- Telescope Name (from your Profile)
- Camera Name
- Image Filter, Duration, Gain, Offset, Binning, and Total RMS (in arcseconds)
- A small (300px) thumbnail of the stretched frame.

This logging is automatically enabled and can be toggled on or off using the Enabled setting below.")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("2d7210df-05e0-4b02-b8e5-fbf914b54fb0")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]
[assembly: NeutralResourcesLanguage("en")]
