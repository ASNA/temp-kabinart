
> table/file and column/field note here

Writing business applications requires writing tons of complex, specialized code. It also requires writing tons of repetitive, boring, mechanical code. To be effective coders, you need to maximize the time you spend on custom, unique code and minimize the time you spend on repetitive, repeatable code. Let's spend a little thinking about how to generate that repeatable code.

By "repeatable code" I mean things like 

* standard Create/Read/Update/Delete operations
* code to populate paged lists for displaying data
* file and data structure declarations 
* input panel creation for Web or Windows apps
* code to populate dropdown lists

By standardizing these common processes, not only do you make them available quickly, but you also make them available reliably. Code produced with a tested, solid template reduces testing and Q/A time and ensures correctness. 

To generate these repeatable chunks of code, we need:

1. An easy way to produce reliable meta data about files/tables (ie, what are the fields/columns and their data types). This data is pulled directly from existing files and tables.

2. A way to extend this existing meta data (ie, what kind of UI element should a field use in an input panel) to create a more complete data dictionary. You can do a lot without having a way to extend existing meta data, but and extended data dictionary adds substantial possibilities. For now, we'll consider a nice to have, but unnecessary thing--but we will get back to it later.

3. A flexible template engine that lets you create templates that produce AVR, C#, SQL, HTML, Markdown, ASPX, MVC or other Web markup, or any other output type that you need.  


#### Step 1. Producing reliable meta data

`GenFileSchema` is an AVR program that produces a Json (or Yaml) file that describes a data file in a given library. It was written with AVR 15.x but should work with versions down to about AVR 12.x and up.

`GenFileSchema` is a command line application. After installing it, type `GenFileSchema` on the command line and press enter. The screen of text below is shown. (You can also see this screen using the --help or -h flag.)



    Generate DataGate file schemas for a library

    Flag                  ShortHand  Required  Description
    --------------------  ---------  --------  ---------------------------------------------
    --databasename           -d        True    Database Name
    --library                -l        True    Library name (or *LIBL if using a library list)
    --outputpath             -op       False   output path (appended to the user's Documents folder)
    --yaml                   -y        False   Write schema in YAML instead of Json
    --pause                  -p        False   Pause the screen after processing
    --help                   -h                Show this help

    The default schema output path is:
    C:\Users\roger\Documents\Programming\_AVR\rpUtilities\librettox\template_work\schemas
    The default output path is the user's Document path plus what is defined in the 'app.config' file.
    To change the output path at runtime, the --outputpath arg is appended to the user's Documents path.
    The runtime output path must exist.

The default output path is defined in the program's `app.config` file, but it can be overridden with the `--outputpath/-op` flags.

    <appSettings>
        <add key="defaultOutputPath"
             value="Programming\_AVR\rpUtilities\librettox\template_work\schema"
        />
    </appSettings>   

asdfasdfasdf


    genfileschema -d "*Public/DG NET Local" -l devo


Produces     

    {
    "dbname": "*public/dg net local",
    "library": "devo",
    "file": "states",
    "format": "rstates",
    "description": "",
    "type": "physical",
    "recordlength": "50",
    "keylength": "48",
    "basefile": "",
    "duplicatekeys": "not allowed",
    "sqlserveruniqueindex": "unique",
    "alias": "states",
    "keyfieldslist": "state",
    "allfieldslist": "state, abbrev",
    "fields": [
        {
        "name": "state",
        "description": "",
        "alias": "state",
        "fulltype": "Type(*char) Len(48)",
        "type": "*char",
        "length": "48",
        "decimals": "",
        "systemtype": "System.String",
        "iskey": true,
        "keyposition": 0,
        "allownull": false,
        "sqlservertype": "char",
        "sqlservernull": "NOT NULL",
        "sqlserverprimarykey": "PRIMARY KEY"
        },
        {
        "name": "abbrev",
        "description": "",
        "alias": "abbrev",
        "fulltype": "Type(*char) Len(2)",
        "type": "*char",
        "length": "2",
        "decimals": "",
        "systemtype": "System.String",
        "iskey": false,
        "keyposition": -1,
        "allownull": false,
        "sqlservertype": "char",
        "sqlservernull": "NOT NULL",
        "sqlserverprimarykey": ""
        }
    ],
    "keyfields": [
        {
        "name": "state",
        "description": "",
        "alias": "state",
        "fulltype": "Type(*char) Len(48)",
        "type": "*char",
        "length": "48",
        "decimals": "",
        "systemtype": "System.String",
        "iskey": true,
        "keyposition": 0,
        "allownull": false,
        "sqlservertype": "char",
        "sqlservernull": "NOT NULL",
        "sqlserverprimarykey": "PRIMARY KEY"
        }
    ],
    "nonkeyfields": [
        {
        "name": "abbrev",
        "description": "",
        "alias": "abbrev",
        "fulltype": "Type(*char) Len(2)",
        "type": "*char",
        "length": "2",
        "decimals": "",
        "systemtype": "System.String",
        "iskey": false,
        "keyposition": -1,
        "allownull": false,
        "sqlservertype": "char",
        "sqlservernull": "NOT NULL",
        "sqlserverprimarykey": ""
        }
    ]
    }


























## File-level tokens

dbname................... Database name
library.................. Library name
file..................... File name
format................... Format name
description.............. File description
type..................... File type (physical or logical)
recordlength............. Record length
keylength................ Key length
basefile................. Base file (logical files only)
duplicatekeys............ Are duplicated keys allowed
sqlserveruniqueindex..... 'unique' if index is unique
alias.................... File alias (initially the same as 'file')
keyfieldslist............ Comma-separated list of key fields
allfieldslist............ Comma-separated list of all fields

### Field-level tokens

name..................... Field
description.............. Description 
alias.................... Field alias (initially the same as 'name')
fulltype................. Field full type
type..................... Field type
length................... Field length
decimals................. Field decimals
systemtype............... .NET system type
iskey.................... True if field is a key field
keyposition.............. Zero-based key position or -1 if field is not a key
allownull................ Allow nulls (True or False)
sqlservertype............ SQL Server type 
sqlservernull............ Nullable
sqlserverprimarykey...... 'PRIMARY KEY' if field is part of primary key

### Field lists

There are three groups of fields available:

* fields........ All of the fields in the file
* keyfields..... The key fields in the file
* nonkeyfields.. The non-key fields in the file

In a template, these groups can be iterated to generate code. For example, let's say you want to generate a simple class with each field in the file as a property. 

This template:

    Using System 

    DclNamespace MyNamespace 

    BegClass {{file}}_buffer Access(*Public)
        {% for field in fields %}
        DclProp {{field.name}} {{field.fulltype}} Access(*Public) 
        {% endfor %}
    EndClass

Generates this code:

    Using System 

    DclNamespace MyNamespace 

    BegClass cmastnewl2_buffer Access(*Public)
        DclProp cmcustno Type(*packed) Len(9,0) Access(*Public) 
        DclProp cmname Type(*char) Len(40) Access(*Public) 
        DclProp cmaddr1 Type(*char) Len(35) Access(*Public) 
        DclProp cmcity Type(*char) Len(30) Access(*Public) 
        DclProp cmstate Type(*char) Len(2) Access(*Public) 
        DclProp cmcntry Type(*char) Len(2) Access(*Public) 
        DclProp cmpostcode Type(*char) Len(10) Access(*Public) 
        DclProp cmactive Type(*char) Len(1) Access(*Public) 
        DclProp cmfax Type(*packed) Len(10,0) Access(*Public) 
        DclProp cmphone Type(*char) Len(20) Access(*Public) 
    EndClass

Let's say that you'd like to generate the class above, but you want to separate the key fields from the non-key fields (it's a contrived example, I know!). You could do that with this template: 

    Using System 

    DclNamespace MyNamespace 

    BegClass {{file}}_buffer Access(*Public)
        {% for field in keyfields %}
        DclProp {{field.name}} {{field.fulltype}} Access(*Public) 
        {% endfor %}

        {% for field in nonkeyfields %}
        DclProp {{field.name}} {{field.fulltype}} Access(*Public) 
        {% endfor %}
    EndClass

Which generates this code:

    Using System 

    DclNamespace MyNamespace 

    BegClass cmastnewl2_buffer Access(*Public)
        DclProp cmname Type(*char) Len(40) Access(*Public) 
        DclProp cmcustno Type(*packed) Len(9,0) Access(*Public) 

        DclProp cmaddr1 Type(*char) Len(35) Access(*Public) 
        DclProp cmcity Type(*char) Len(30) Access(*Public) 
        DclProp cmstate Type(*char) Len(2) Access(*Public) 
        DclProp cmcntry Type(*char) Len(2) Access(*Public) 
        DclProp cmpostcode Type(*char) Len(10) Access(*Public) 
        DclProp cmactive Type(*char) Len(1) Access(*Public) 
        DclProp cmfax Type(*packed) Len(10,0) Access(*Public) 
        DclProp cmphone Type(*char) Len(20) Access(*Public) 
    EndClass

Notice that blank lines are significant in templates. In the template above, there is a blank line between key fields and non-key fields. Also, notice the subtle difference in field order between these two examples. When you iterate the `fields` collection the fields are listed in the order in which they occur in the file definition. When you iterate the `keyfields` collection, the keys are listed in the key definition order. 
