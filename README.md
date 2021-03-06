# Pōmōna. The DTO-free way to REST for lazy people!

Pōmōna is all about exposing your domain model as a REST API. With less pain.

It was born out of my personal experience with being bored from manually implementing
a whole new layer of DTO (Data Transfer Object) and mapping, with the feeling that I
was repeating myself over and over.

So the goal is that Pōmōna will offer a way to do this mapping and (de)serialization
by convention, and remove the need for DTO's all together. This shall be achieved by:

* Supporting custom conventions, with a good set of default ways to do things.
* Expose an API to override these conventions for special cases. (TODO)
  Yeah sure it should be Fluent, for the cool kids! ;)
* Making it possible to generate an easy-to-use .NET client dll on-the-fly.
* Make it possible to specify what references to expand and not.

Additionally I also want it to be able to:

* Semi-automatic management of REST API versioning by inspection of changes in JSON schema. (TODO)

Oh, by the way, for all nitpickers out there. I use the REST definition freely. I believe
it will be possible to expose a somewhat RESTful API through Pōmōna someday, but oh course
it all depends on the domain model mapped.

## State of the project

Although usable for simple scenarios, Pōmōna should be considered early work-in-progress stuff.
It will change. A lot. It's for the adventurous and the rebels! ;)

My personal goal is to release a version 1.0 before christmas. But no promises, yet.

## On the shoulders of really cool people:

* [JSON.NET](ttp://james.newtonking.com/projects/json-net.aspx) for serialization stuff. h
* [Nancy](http://nancyfx.org/) for hosting the web service. 
  I really love Nancy! I can't overstate how good I think it is! <3 <3 <3
  One day I hope Pōmōna will offer a Super-Duper-Happy path just like it.
* [NUnit](http://www.nunit.org/) for testing.
* [Cecil](http://www.mono-project.com/Cecil) for generation of Client dll.
* [google-code-prettify](http://code.google.com/p/google-code-prettify/) for presenting JSON as HTML.

A huge thank you to all the authors of these projects.

## Getting started

So if you really want to check this stuff out, here's how you get started.

1. Implement your own `IPomonaDataSource`
2. Inherit from `TypeMappingFilterBase`, and at a minimum implement `GetSourceTypes()` and `GetIdFor()`.
   They're abstract, so you can't miss them.
   `GetSourceTypes()` must return the list of what Types to expose to web service.
3. Inherit `PomonaModule` (which is a Nancy module), and treat this as you normally would treat a Nancy module.
   Which could mean zero configuration. Just because Nancy is *that* awesome!
4. Inherit PomonaConfigurationBase and fill in the abstracts.

Look at the Critter example in the source code for details. If you fire up the `Pomona.Example.ServerApp.exe`, it expose the critters on port 2211.
When ServerApp is running go here with a web browser to see what Pōmōna is all about:

* `http://localhost:2211/critters`
* `http://localhost:2211/critters?$expand=critter.hat`
* `http://localhost:2211/Critters.Client.dll` - Generates a client library on-the-fly
* `http://localhost:2211/Critters.Client.1.0.0.0.nupkg` - Generates a [NuGet Package](http://www.nuget.org/) for the client library on-the-fly
* `http://localhost:2211/schemas` - Returns the JSON schema for the transformed data model
* 
You can also `POST` to `http://localhost:2211/critter` create a new critter entity, or `PUT` to `http//localhost:2211/critter/<id>` to update the values of a critter.

## Roadmap for first release

Features:
* Add tests for serialization and deserialization on client DONE
* Create IPomonaDataSource, for retrieval of data. DONE
* Create PomonaSession and PōmōnaSessionFactory that will bind everything together.
* Write correct metadata for generated client dll (AssemblyInfo etc..)
* Implement support for value types (which is always expanded, and don't have URI). 90% DONE.
* Implement simple query mechanism (Linq? relinq? something simpler?) 90% DONE.
  * Syntax inspired by OData, with some differences.
  * Most OData operators are supported, but functions are missing.
* Create a test helper that will compare an existing JSON schema with new one,
  then detect any breaking changes in API.
* Implement proper PATCH functionality.
* ETags and versioning

## Future tasks

* Implement JS client lib. Maybe two types, one based on KnockoutJs? That would be cool.
* Implement html media type for friendly browseing.
* Implement batch fetching on client side
* Batch query support, for example by encapsulating an array of http operations in a JSON array

# Brainstorm area
## Implementation of automatic API compatibility checking

For this to work we need to:
* Have a api schema folder specified.
* Have a method writing out existing schemas with version numbering: {api-name}.{version}.json
* Find a way to whitelist a breaking API change.
* Implement class ApiChangeVerifier.
* Encourage having an ignored test that outputs current schema.

## Random ideas

* Could make query mechanism pluggable, through some sort of `IHttpQueryTranslator`.
  Then we could provide a simple default implementation.

* An exotic side-project could be to implement an `IHttpQueryTranslator` that uses relinq
  to serialize and convert LINQ expressions, which then can be executed on Nhibernate or other ORM.
  This does however seem a bit dangerous with regards to security.

## Automated batching of queries to decrease N+1 performance problems

The classic N+1 problem will appear when looping through a list, where we at each step
access a reference to another object, which will then be loaded. Although Pōmōna supports
sending a list of expanded paths to deal with these problems, it would be sorta cool if
this could be detected runtime and fixed.

This is one way to do it (by example). We got two simple entity types:

Customer example:
```json
{
    "name" : "The first name",
    "order" : {
        "_ref" : "http://blah/order/1"
    }
}
```

Order example:
```json
{
    "_uri" : "http://blah/order/1",
    "description" : "This is a order",
}
```

This can be accomplished by keeping track of what properties have been accessed, and in what order.

Lets say we get a list of 30 customers by some criteria that is supposed to be presented
in a table along with the order description.. And we iterate through the customers in order.

Total query count: 1

`customers[0].Order.Description` is accessed first, which, as is normal, makes the first order loaded.
We take a note that Order of description has been loaded for first Customer, and double the prefetch
count for the path Customer.Order, from 1 (no prefetching) to 2.

Total query count: 2

`customers[1].Order.Description` is accessed next time, which loads Order for both customer #1 and customer #2.
Then prefetch count is doubled again from 2 to 4.

`customers[2].Order` is already prefetched, so nothing needs to be loaded for that.

Total query count: 3

The `customers[3].Order` is accessed, which loads Order for customer #3,#4,#5,#6. The prefetch count is
doubled again.

Total query count: 4

And so on:

Query 5: Order #7, #8, #9, #10, #11, #12, #13, #14 loaded
Query 6: Order #15, #16, #17, #18, #19, #20, #21, #22, #23, #24 loaded

This gives a total of 6 http requests instead of 26, which means instead of N+1 we have Log2(N)+1 operations.


[![Bitdeli Badge](https://d2weczhvl823v0.cloudfront.net/okb/pomona/trend.png)](https://bitdeli.com/free "Bitdeli Badge")

