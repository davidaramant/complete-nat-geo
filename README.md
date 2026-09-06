# complete-nat-geo
A local web app for browsing abandoned The Complete National Geographic DVD set.

The product shipped with a client app written in Adobe Air. It is not supported anymore and was pretty clunky even when new. 

The intended usecase is to host everything on an internal network and browse everything there from a more modern interface.

## Projects

### Local Tools

These are command line tools that are only run during the initial conversion process.

#### CngConverter

Used to convert the "encrypted" `.cng` pages to `.jpg`. Since the "encryption" (more like obfuscation really) is so trivial this ends up surprisingly fast.

#### PostgresBuilder

Peforms a few functions:

* Merges the legacy SQLite databases together to a single one. The yearly update disks contained additional SQLite databases that need to be combined with the larger one containing everything up to 2009.
* Converts the legacy SQLite database into a Postgres database. This is done through online population (IE Postgres is actually running).
  * The most complicated aspect is getting the page ordering & numbering correct. This was done at runtime in the legacy app, but in this version it's a one-time calculation and is stored in the database.

### Services

#### Postgres Database

Not a code project. Contains the converted schema into a format that is easier to query.

#### API

An ASP.NET REST API. Talks to the database and offers various endpoints for reading & querying data.

#### Client

A Blazor PWA. The primary use case is for browsing on an iPad. By using a PWA we can avoid the Safari chrome and make the viewer fullscreen. Browsing on a desktop web browser is also supported. Phones are ignored.

## Supported Deployements

Things can be run locally from the development machine, mostly intended for testing/development.

The services are intended to be deployed onto a Synology NAS in my case.