[![Build status](https://ci.appveyor.com/api/projects/status/9u1w8cyyr22kbcwi?svg=true)](https://ci.appveyor.com/project/wojtpl2/extendedxmlserializer) [![NuGet](https://img.shields.io/nuget/v/ExtendedXmlSerializer.svg)](https://www.nuget.org/packages/ExtendedXmlSerializer/)
# ExtendedXmlSerializer
Extended Xml Serializer for .NET

Support platforms
* .NET 4.5 
* .NET Platform Standard 1.6

Support framework:
* ASP.NET Core
* WebApi

Support features
* deserialization xml from standard XMLSerializer
* serialization class, struct, generic class, primitive type, generic list and dictionary, array, enum
* serialization class with property interface
* serialization circular reference and reference Id
* deserialization old version of xml
* custom serializer
* POCO - all configurations (migrations, custom serializer...) is outside the class

## Serialization
```C#
ExtendedXmlSerializer serializer = new ExtendedXmlSerializer();
var obj = new TestClass();
var xml = serializer.Serialize(obj);
```

## Deserialization
```C#
var obj2 = serializer.Deserialize<TestClass>(xml);
```

## Serialization of dictionary
You can serialize generic Dictionary<TKey, TValue>. Dictionary can store any type.
```C#
public class TestClass
{
	public Dictionary<int, string> Dictionary { get; set; }
}
var obj = new TestClass
{
	Dictionary = new Dictionary<int, string>
	{
		{1, "First"},
		{2, "Second"},
		{3, "Other"},
	}
};
```
Output xml will look like:
```xml
<TestClass type="Samples.TestClass">
  <Dictionary>
    <Item>
        <Key>1</Key>
        <Value>First</Value>
    </Item>
    <Item>
        <Key>2</Key>
        <Value>Second</Value>
    </Item>
    <Item>
        <Key>3</Key>
        <Value>Other</Value>
    </Item>
  </Dictionary>
</TestClass>
```

## Custom serialization
If your class has to be serialized in a non-standard way:
```C#
    public class TestClass
    {
        public TestClass(string paramStr)
        {
            PropStr = paramStr;
        }

        public string PropStr { get; private set; }
    }
```
You must configure custom serializer:
```C#
	public class TestClassConfig : ExtendedXmlSerializerConfig<TestClass>
    {
        public TestClassConfig()
        {
            CustomSerializer(Serializer, Deserialize);
        }

        public TestClass Deserialize(XElement element)
        {
            return new TestClass(element.Element("String").Value);
        }

        public void Serializer(XmlWriter writer, TestClass obj)
        {
            writer.WriteElementString("String", obj.PropStr);
        }
    }
```
Then you must register your TestClassConfig class. See point configuration.

## Deserialize old version of xml
If you had a class:
```C#
    public class TestClass
    {
        public int Id { get; set; }
        public string Type { get; set; } 
    }
```
and generated xml look like:
```xml
<?xml version="1.0" encoding="utf-8"?>
<TestClass type="Samples.TestClass">
  <Id>1</Id>
  <Type>Type</Type>
</TestClass>
```
Then you renamed property:
```C#
    public class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } 
    }
```
and generated xml look like:
```xml
<?xml version="1.0" encoding="utf-8"?>
<TestClass type="Samples.TestClass" ver="1">
  <Id>1</Id>
  <Name>Type</Name>
</TestClass>
```
Then you added new property and you want to calculate a new value while deserialization.
```C#
    public class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } 
        public string Value { get; set; }
    }
```
and new xml should look like:
```xml
<?xml version="1.0" encoding="utf-8"?>
<TestClass type="Samples.TestClass" ver="2">
  <Id>1</Id>
  <Name>Type</Name>
  <Value>Calculated</Value>
</TestClass>
```
You can migrate (read) old version of xml using migrations:
```C#
	public class TestClassConfig : ExtendedXmlSerializerConfig<TestClass>
    {
        public TestClassConfig()
        {
            AddMigration(MigrationV0).AddMigration(MigrationV1);
        }

        public static void MigrationV0(XElement node)
        {
            var typeElement = node.Elements().FirstOrDefault(x => x.Name == "Type");
            // Add new node
            node.Add(new XElement("Name", typeElement.Value));
            // Remove old node
            typeElement.Remove();
        }

        public static void MigrationV1(XElement node)
        {
            // Add new node
            node.Add(new XElement("Value", "Calculated"));
        }
    }
```
Then you must register your TestClassConfig class. See point configuration.

## Object reference and circular reference
If you have a class:
```C#
    public class Person
    {
        public int Id { get; set; }
     
	    public string Name { get; set; }

        public Person Boss { get; set; }
    }

    public class Company
    {
        public List<Person> Employees { get; set; }
    }
```

then you create object with circular reference, like this:
```C#
    var boss = new Person {Id = 1, Name = "John"};
    boss.Boss = boss; //himself boss
    var worker = new Person {Id = 2, Name = "Oliver"};
    worker.Boss = boss;
    var obj = new Company
    {
        Employees = new List<Person>
        {
            worker,
            boss
        }
    };
```

You must configure Person class as reference object:
```C#
    public class PersonConfig : ExtendedXmlSerializerConfig<Person>
    {
        public PersonConfig()
        {
            ObjectReference(p => p.Id);
        }
    }
```
Then you must register your PersonConfig class. See point configuration.

Output xml will look like this:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Company type="Samples.Company">
   <Employees>
      <Person type="Samples.Person" id="2">
         <Id>2</Id>
         <Name>Oliver</Name>
         <Boss type="Samples.Person" ref="1" />
      </Person>
      <Person type="Samples.Person" id="1">
         <Id>1</Id>
         <Name>John</Name>
         <Boss type="Samples.Person" ref="1" />
      </Person>
   </Employees>
</Company>
```

## Configuration
For use config class you must register them in ExtendedXmlSerializer. You can do this in two ways.

#### Use SimpleSerializationToolsFactory class
```C#
var toolsFactory = new SimpleSerializationToolsFactory();
// Register your config class
toolsFactory.Configurations.Add(new TestClassConfig());

ExtendedXmlSerializer serializer = new ExtendedXmlSerializer(toolsFactory);
```

#### Use Autofac integration
```C#
var builder = new ContainerBuilder();
// Register ExtendedXmlSerializer module
builder.RegisterModule<AutofacExtendedXmlSerializerModule>();

// Register your config class
builder.RegisterType<TestClassConfig>().As<ExtendedXmlSerializerConfig<TestClass>>().SingleInstance();

var containter = builder.Build();

// Resolve ExtendedXmlSerializer
var serializer = containter.Resolve<IExtendedXmlSerializer>();
```

## ASP.NET Core integration
You can integrate the ExtendedXmlSerializer with ASP.NET Core, so that your services will generate XML using a ExtendedXmlSerializer. You only need to install [ExtendedXmlSerializer.AspCore](https://www.nuget.org/packages/ExtendedXmlSerializer.AspCore/) and configure it in Startup.cs.

#### Use SimpleSerializationToolsFactory class
This configuration is very simple. You just need create configuration for ExtendedXmlSerializer and add formatters to MVC.
```C#
public void ConfigureServices(IServiceCollection services)
{
    // Custom create ExtendedXmlSerializer
    SimpleSerializationToolsFactory factory = new SimpleSerializationToolsFactory();
    factory.Configurations.Add(new TestClassConfig());
    IExtendedXmlSerializer serializer = new ExtendedXmlSerializer(factory);

    // Add services to the collection.
    services.AddMvc(options =>
    {
        options.RespectBrowserAcceptHeader = true; // false by default

        //Add ExtendedXmlSerializer's formatter
        options.OutputFormatters.Add(new ExtendedXmlSerializerOutputFormatter(serializer));
        options.InputFormatters.Add(new ExtendedXmlSerializerInputFormatter(serializer));
    });
}
```
#### Use Autofac integration
This configuration is more difficult but recommended. You have to install [Autofac.Extensions.DependencyInjectio](www.nuget.org/packages/Autofac.Extensions.DependencyInjection/) and read Autofac [documentation](docs.autofac.org/en/latest/integration/aspnetcore.html). The following code adds a MVC service and creates a container AutoFac.
```C#
public IServiceProvider ConfigureServices(IServiceCollection services)
{
    // Add services to the collection.
    services.AddMvc(options =>
    {
        options.RespectBrowserAcceptHeader = true; // false by default

        //Resolve ExtendedXmlSerializer
        IExtendedXmlSerializer serializer = ApplicationContainer.Resolve<IExtendedXmlSerializer>();

        //Add ExtendedXmlSerializer's formatter
        options.OutputFormatters.Add(new ExtendedXmlSerializerOutputFormatter(serializer));
        options.InputFormatters.Add(new ExtendedXmlSerializerInputFormatter(serializer));
    });

    // Create the container builder.
    var builder = new ContainerBuilder();

    // Register dependencies, populate the services from
    // the collection, and build the container. If you want
    // to dispose of the container at the end of the app,
    // be sure to keep a reference to it as a property or field.
    builder.Populate(services);
    builder.RegisterModule<AutofacExtendedXmlSerializerModule>();
    builder.RegisterType<TestClassConfig>().As<ExtendedXmlSerializerConfig<TestClass>>().SingleInstance();
    this.ApplicationContainer = builder.Build();

    // Create the IServiceProvider based on the container.
    return new AutofacServiceProvider(this.ApplicationContainer);
}
```
In this case you can also inject IExtendedXmlSerializer into your controller:
```C#
    [Route("api/[controller]")]
    public class TestClassController : Controller
    {
        private readonly IExtendedXmlSerializer _serializer;

        public TestClassController(IExtendedXmlSerializer serializer)
        {
            _serializer = serializer;
        }

        ...
    } 
```

## WebApi integration
You can integrate ExtendedXmlSerializer with WebApi, so that your services will generate XML using a ExtendedXmlSerializer. You only need to install [ExtendedXmlSerializer.WebApi](www.nuget.org/packages/ExtendedXmlSerializer.WebApi/) and configure it in WebApi configuration. You can do it using autofac or SimpleSerializationToolsFactory e.g.:
```C#
public static void Register(HttpConfiguration config)
{
    // Manual creation of IExtendedXmlSerializer or resolve it from AutoFac.
    var simpleConfig = new SimpleSerializationToolsFactory();
    simpleConfig.Configurations.Add(new TestClassConfig());
    var serializer = new ExtendedXmlSerializer(simpleConfig);

    config.RegisterExtendedXmlSerializer(serializer);

    // Web API routes
    config.MapHttpAttributeRoutes();

    config.Routes.MapHttpRoute(
        name: "DefaultApi",
        routeTemplate: "api/{controller}/{id}",
        defaults: new { id = RouteParameter.Optional }
    );
}
```
